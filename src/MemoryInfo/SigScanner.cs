using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;

namespace MemoryInfo
{
    #region Delegates

    public delegate void ScanChanged(DateTime when);

    public delegate void ScanProgress(int foundOffsetsCount);

    public delegate void ScanException(Exception ex);

    public delegate void ScanMemoryError(string Message);

    public delegate void ScanResultFound(DateTime when);

    #endregion Delegates

    public class SigScanner
    {
        #region Events

        public event ScanChanged OnScanBegin = delegate { };

        public event ScanChanged OnScanEnd = delegate { };

        public event ScanException OnScanException = delegate { };

        public event ScanMemoryError OnScanError = delegate { };

        public event ScanResultFound OnScanFound = delegate { };

        public event ScanProgress OnScanProgress = delegate { };

        #endregion Events

        #region Fields

        private MemoryWalker _memoryTarget = null;

        private ulong _startPosition = 0;

        private long _readSize;

        private int _maxReadSize;

        private long _maxSearchSize;

        private byte[] _memdump;

        private bool _isMultipleOffsetsSearch;

        private WinAPI.Structs.MemoryBasicInformation _currentMemoryRegion;

        #endregion Fields

        #region Properties

        public Dictionary<ulong, WinAPI.Structs.MemoryBasicInformation> FoundAddresses { get; set; }

        public List<WinAPI.Structs.MemoryBasicInformation> MemoryRegions { get; private set; }

        #endregion Properties

        #region Constructors

        public SigScanner(Memory memory, int? maxReadSize = null, bool isMultipleOffsetsSearch = false)
        {
            _memoryTarget = new MemoryWalker(memory);
            _maxReadSize = maxReadSize ?? 16 * (1024 * 1024); // 1024KB
            _isMultipleOffsetsSearch = isMultipleOffsetsSearch;

            if (isMultipleOffsetsSearch)
                FoundAddresses = new Dictionary<ulong, WinAPI.Structs.MemoryBasicInformation>();
            MemoryRegions = memory.GetAccessableMemoryRegions();
        }

        ~SigScanner()
        {
            // Remove our listeners.
            OnScanBegin = null;
            OnScanEnd = null;
            OnScanException = null;
            OnScanError = null;
            OnScanProgress = null;
            OnScanFound = null;
        }

        #endregion Constructors

        #region Methods

        public void Reset()
        {
            _startPosition = 0;
            _readSize = 0;
            _memdump = null;
        }

        public uint ScanModule(string pattern, string mask = null)
        {
            var byteArray = pattern.Split(' ');
            byte[] patternBytes = new byte[byteArray.Length];
            for (var byteIndex = 0; byteIndex < byteArray.Length; byteIndex++)
            {
                patternBytes[byteIndex] = Byte.Parse(byteArray[byteIndex], NumberStyles.HexNumber);
            }

            return ScanModule(patternBytes, mask);
        }

        /// <summary>
        /// Dumping/checking memory chunks for the
        /// pattern requested.
        ///
        /// Returns zero on failure or a memory address somewhere in the
        /// module on success.
        /// </summary>
        public uint ScanModule(byte[] pattern, string mask = null)
        {
            bool isFullPatternSearch = false;

            // Standard checks.
            if (String.IsNullOrEmpty(mask) || mask.IndexOf('?') == -1)
            {
                isFullPatternSearch = true;
            }

            if (!isFullPatternSearch && pattern.Length != mask.Length)
            {
                OnScanError("The mask is bad.");
                return 0;
            }

            OnScanBegin(DateTime.Now);

            Reset();

            try
            {
                foreach (var memoryRegion in MemoryRegions)
                {
                    _currentMemoryRegion = memoryRegion;
                    _startPosition = (ulong)memoryRegion.BaseAddress;
                    long maxSearchSize = (uint)memoryRegion.RegionSize;
                    long leftoverSize = 0;
                    uint foundPatternIndex = 0;

                    if (maxSearchSize == 0)
                    {
                        OnScanError("Couldn't find the module or module size was 0.");
                        return 0;
                    }

                    // Careful with ReadSize: We want it to be a multiple of bPattern,
                    // but don't miss any bytes. Keep the leftover size in case we can't
                    // grab the whole memory region at once.
                    if (maxSearchSize > _maxReadSize)
                    {
                        _readSize = _maxReadSize;
                        leftoverSize = maxSearchSize - _maxReadSize;
                    }
                    else
                    {
                        _readSize = maxSearchSize;
                    } // Scan the whole thing.

                    // Scan chunks of this module's memory.
                    while (_readSize > 0)
                    {
                        OnScanProgress(FoundAddresses.Count);

                        // Grab the current chunk of memory.
                        if (DumpMemory() == false)
                        {
                            return 0;
                        }

                        if (!_memdump.All(item => item == 0))
                        {
                            // Scan it for our value.
                            foundPatternIndex = FindPattern(pattern, mask, isFullPatternSearch);
                            if (!_isMultipleOffsetsSearch && foundPatternIndex != 0)
                            {
                                // Got it.
                                OnScanFound(DateTime.Now);
                                return foundPatternIndex;
                            }
                        }

                        // Update ReadSize and StartPos. FindPattern stops at
                        // _memdump's length minus the signature size to avoid
                        // an overflow on the buffer. So the new StartPos is:
                        // StartPos + (read buffer size - pattern length)
                        if (leftoverSize == 0)
                        {
                            _readSize = 0;
                        } // Done.
                        else if (leftoverSize > _maxReadSize)
                        {
                            // Advance the starting position.
                            _startPosition = _startPosition + (ulong)_readSize - (ulong)pattern.Length;

                            // We end up grabbing the same regions of memory
                            // multiple times with this method. If we have a
                            // really large memory length to scan call this
                            // function from a separate thread.
                            _readSize = _maxReadSize;
                            leftoverSize -= _maxReadSize;
                        }
                        else
                        {
                            _startPosition = _startPosition + (ulong)_readSize - (ulong)pattern.Length;
                            _readSize = leftoverSize;
                            leftoverSize = 0;
                        }
                    }
                }

                // All done.
                OnScanEnd(DateTime.Now);
            }
            catch
            {
                // Grab the last interop error value.
                Win32Exception win32Exception = new Win32Exception();
                OnScanError(win32Exception.Message);
                return 0;
            }

            // Never found the value.
            OnScanError("No match for signature. Check signature, mask and correct module.");
            return 0;
        }

        /// <summary>
        /// Given a signature pattern and a mask, attempt to find the
        /// pattern somewhere in the previously dumped memory.
        /// bPattern is the byte signature
        /// Mask is the string of 'x' and '?' to compare bytes. x = check, ? = skip byte.
        /// nOffset is the address added to the result if found.
        /// Returns a non-zero IntPtr if the pattern was found.
        /// </summary>
        private uint FindPattern(byte[] pattern, string mask, bool isFullPatternSearch)
        {
            try
            {
                // Make sure mask and the pattern length matches.
                if (!isFullPatternSearch && pattern.Length != mask.Length)
                {
                    OnScanError("Signature and signature mask must be same length.");
                    return 0;
                }

                // Loop the byte region looking for the pattern.
                if (isFullPatternSearch)
                {
                    long memoryDumpIndex = 0;
                    if (_isMultipleOffsetsSearch)
                    {
                        var indexes = _memdump.Locate(pattern);
                        foreach (var index in indexes)
                        {
                            FoundAddresses.Add((ulong)((ulong)_startPosition + (ulong)index), _currentMemoryRegion);
                        }
                    }
                    else
                    {
                        memoryDumpIndex = _memdump.IndexOf(pattern);

                        if (memoryDumpIndex != 0)
                        {
                            // FOUND
                            var offset = (uint)((int)_startPosition + (memoryDumpIndex));
                            return offset;
                        }
                    }
                }
                else
                {
                    for (int byteIndex = 0; byteIndex < _memdump.Length; byteIndex++)
                    {
                        if (MaskCheck(byteIndex, pattern, mask))
                        {
                            // FOUND
                            var offset = (uint)((int)_startPosition + byteIndex);
                            if (_isMultipleOffsetsSearch)
                                FoundAddresses.Add(offset, _currentMemoryRegion);
                            else
                                return offset;
                        }
                    }
                }
            }
            catch
            {
                Win32Exception wExcept = new Win32Exception();
                OnScanError(wExcept.Message);
                return 0;
            }

            // Nothing was found, return zero.
            return 0;
        }

        /// <summary>
        /// Used internally to grab chunks of memory for scanning.
        /// </summary>
        private bool DumpMemory()
        {
            try
            {
                if (_memoryTarget == null
                    || _readSize == 0)
                {
                    return false;
                }

                _memdump = _memoryTarget.GetBytes(_startPosition, (uint)_readSize);
            }
            catch (Exception e)
            {
                OnScanException(e);
                return false;
            }

            return true;
        }

        private bool MaskCheck(int nOffset, byte[] bPattern, string Mask)
        {
            for (int i = 0; i < bPattern.Length; i++)
            {
                // Does this put us past the end of the memory dump?
                if (i + nOffset >= _memdump.Length)
                {
                    return false;
                }

                // If the mask is a wildcard ignore this byte.
                if (Mask[i] == '?')
                {
                    continue;
                }

                // Check the pattern against the memory dump.
                if (bPattern[i] != _memdump[nOffset + i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Helpful function to turn a structure into a byte array for
        /// scanning.
        ///
        /// NOTE: Make sure your struct implements LayoutKind.Sequential
        /// otherwise C# will "pad" the struct to make it match the memory boundaries and your signature will never match.
        /// If you need a refresher on how to use structs in C# I highly recommend:
        /// http://www.developerfusion.com/article/84519/mastering-structs-in-c/
        /// </summary>
        public byte[] StructToBytes(object aStruct)
        {
            // Find the size of the struct in memory.
            int size = Marshal.SizeOf(aStruct);

            // Allocate memory that size on the heap, get a pointer to it.
            IntPtr buf = Marshal.AllocHGlobal(size);

            // Convert the struct to a pointer, with no Dispose() on struct beforehand.
            Marshal.StructureToPtr(aStruct, buf, false);

            // Create a byte array to receive the converted struct.
            byte[] structBytes = new byte[size];

            // Then copy from the heap to our array.
            Marshal.Copy(buf, structBytes, 0, size);

            // Free the heap memory, no memory leaks please.
            Marshal.FreeHGlobal(buf);

            // Return the converted struct as bytes.
            return structBytes;
        }

        #endregion Methods
    }
}