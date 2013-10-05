using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using WinAPI;

namespace MemoryInfo
{
    /// <summary>
    ///   Contains methods to interact with an external applications, including reading- and writing memory.
    /// </summary>
    public class Memory : IDisposable
    {
        #region Protected

        /// <summary>
        ///   Contains the handle to the walker object for this memory.
        /// </summary>
        protected MemoryWalker _hWalker = null;

        /// <summary>
        ///   Process handle of the opened process.
        /// </summary>
        protected IntPtr _hProcess = IntPtr.Zero;

        /// <summary>
        ///   Contains the process identifier of the found process.
        /// </summary>
        protected int _iProcessID = 0;

        /// <summary>
        ///   Contains the process main window handle which can be used in command sending.
        /// </summary>
        protected IntPtr _iProcessWindow = IntPtr.Zero;

        /// <summary>
        ///   Process module used.
        /// </summary>
        protected ProcessModule _mModule = null;

        /// <summary>
        ///   The opened process.
        /// </summary>
        protected Process _pProcess = null;

        /// <summary>
        ///   Process name which was opened, used as reference for Base.
        /// </summary>
        protected string _zProcessName = null;

        #endregion Protected

        #region Properties

        /// <summary>
        ///   Retrieve the handle to the walker object.
        /// </summary>
        public MemoryWalker Walker
        {
            get { return _hWalker; }
        }

        /// <summary>
        ///   Retrieve the opened process.
        /// </summary>
        public Process Process
        {
            get { return _pProcess; }
        }

        /// <summary>
        ///   Retrieve the currently loaded module.
        /// </summary>
        public ProcessModule Module
        {
            get { return _mModule; }
        }

        /// <summary>
        ///   Retrieve the window handle for the opened process.
        /// </summary>
        public IntPtr Window
        {
            get { return _iProcessWindow; }
        }

        #endregion Properties

        #region Constructors

        /// <summary>
        ///   Constructs a new memory object.
        /// </summary>
        public Memory()
        {
            _hWalker = new MemoryWalker(this);
        }

        #endregion Constructors

        #region Open, Close

        /// <summary>
        ///   Open a process by id.
        /// </summary>
        /// <param name="iProcessID">Contains the process identifier to open.</param>
        public bool Open(int iProcessID)
        {
            try
            {
                // Retrieve the process with the provided identifier.
                Process hProcess = Process.GetProcessById(iProcessID);

                // See if the process is found and abort when it wasn't.
                if (hProcess == null) return false;

                // Remember the details and open the process for reading.
                _pProcess = hProcess;
                _zProcessName = hProcess.ProcessName;
                _hProcess = _pProcess.Handle; //(uint)NativeMethods.OpenProcess(dwDesiredAccess, false, iProcessID).DangerousGetHandle();
                _iProcessID = iProcessID;
                _iProcessWindow = hProcess.MainWindowHandle;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        ///   Open a process by name.
        /// </summary>
        /// <param name="zProcessName">Contains the process name.</param>
        public ArrayList Open(string zProcessName)
        {
            Process[] hProcessList = Process.GetProcessesByName(zProcessName);
            var hResultList = new ArrayList();

            if (hProcessList.Length == 0)
            {
                return null;
            }
            else if (hProcessList.Length > 1)
            {
                foreach (Process hProcess in hProcessList) hResultList.Add(hProcess.Id);
                return hResultList;
            }
            else
            {
                _pProcess = hProcessList[0];
                _zProcessName = zProcessName;
                _hProcess = _pProcess.Handle; //(uint)NativeMethods.OpenProcess(dwDesiredAccess, false, hProcessList[0].Id).DangerousGetHandle();
                _iProcessID = hProcessList[0].Id;
                _iProcessWindow = hProcessList[0].MainWindowHandle;
                hResultList.Add(hProcessList[0].Id);
                return hResultList;
            }
        }

        /// <summary>
        ///   Close the process handle.
        /// </summary>
        public bool Close()
        {
            if (_hProcess != IntPtr.Zero)
            {
                NativeMethods.CloseHandle(_hProcess);
                _hProcess = IntPtr.Zero;
                _iProcessID = 0;
                _iProcessWindow = IntPtr.Zero;
                return true;
            }

            return false;
        }

        #endregion Open, Close

        #region Read, Write

        /// <summary>
        ///   Read the value at the provided memory location with the size.
        /// </summary>
        /// <param name="iAddress">Address in target process to read.</param>
        /// <param name="iSize">Number of bytes to read.</param>
        public byte[] Read(ulong iAddress, uint iSize = 4)
        {
            if (_hProcess != IntPtr.Zero && !_pProcess.HasExited)
            {
                var zBuffer = new byte[iSize];
                int iNumberOfBytesRead = 0;
                NativeMethods.ReadProcessMemory(_pProcess.Handle, (UIntPtr)iAddress, zBuffer, iSize, out iNumberOfBytesRead);
                if (iNumberOfBytesRead == 0)
                {
                    var error = Marshal.GetLastWin32Error();
                    Debug.WriteLine("Read > Marshal.GetLastWin32Error(): " + error);
                }
                return zBuffer;
            }

            return null;
        }

        /// <summary>
        /// Write the provided value at the memory location with the size.
        /// Override memory region protection.
        /// </summary>
        /// <param name="memoryRegion">The memory region where the address is located</param>
        /// <param name="iAddress">Address in target process to write.</param>
        /// <param name="zValue">Value which is to be written.</param>
        /// <param name="iSize">Size of the value to write.</param>
        /// <returns></returns>
        public uint WriteToProtectedMemory(Structs.MemoryBasicInformation memoryRegion, ulong iAddress, byte[] zValue, uint iSize = 4)
        {
            uint iNumberOfBytesWritten = 0;
            var pageProtection = Enums.Protection.PAGE_READWRITE;
            Enums.Protection previousPageProtection;
            NativeMethods.VirtualProtectEx(_hProcess, memoryRegion.BaseAddress, memoryRegion.RegionSize, pageProtection, out previousPageProtection);
            iNumberOfBytesWritten = Write(iAddress, zValue, iSize);
            NativeMethods.VirtualProtectEx(_hProcess, memoryRegion.BaseAddress, memoryRegion.RegionSize, previousPageProtection, out pageProtection);
            return iNumberOfBytesWritten;
        }

        /// <summary>
        ///   Write the provided value at the memory location with the size.
        /// </summary>
        /// <param name="iAddress">Address in target process to write.</param>
        /// <param name="zValue">Value which is to be written.</param>
        /// <param name="iSize">Size of the value to write.</param>
        public uint Write(ulong iAddress, byte[] zValue, uint iSize = 4)
        {
            if (_hProcess != IntPtr.Zero)
            {
                int iNumberOfBytesWritten = 0;
                NativeMethods.WriteProcessMemory(_pProcess.Handle, (UIntPtr)iAddress, zValue, iSize, out iNumberOfBytesWritten);
                return (uint)iNumberOfBytesWritten;
            }

            return 0;
        }

        #endregion Read, Write

        #region Resume, Suspend

        /// <summary>
        ///   Resumes all threads in the target process.
        /// </summary>
        public void Resume()
        {
            Managed.ResumeOrSuspend(_pProcess, isSuspend: false);
        }

        /// <summary>
        ///   Suspends all threads in the target process.
        /// </summary>
        public void Suspend()
        {
            Managed.ResumeOrSuspend(_pProcess, isSuspend: true);
        }

        #endregion Resume, Suspend

        #region Methods

        /// <summary>
        /// Gets all memory regions for the currently opened process
        /// </summary>
        public List<Structs.MemoryBasicInformation> GetAccessableMemoryRegions(bool getAll = false)
        {
            if (_hProcess == IntPtr.Zero)
                return null;

            var memoryRegions = new List<Structs.MemoryBasicInformation>();
            var nativeModules = _pProcess.Modules.OfType<ProcessModule>().Where(module => module.FileName.IndexOf("Windows") != -1).ToArray();

            long MaxAddress = 0x7fffffff;
            long address = 0;
            do
            {
                Structs.MemoryBasicInformation memoryRegionInfo;
                int bytesRead = NativeMethods.VirtualQueryEx(_hProcess, (IntPtr)address, out memoryRegionInfo, Marshal.SizeOf(typeof(Structs.MemoryBasicInformation)));
                if (bytesRead == 0)
                {
                    var error = Marshal.GetLastWin32Error();
                    Debug.WriteLine("GetAccessableMemoryRegions > Marshal.GetLastWin32Error(): " + error);
                    break;
                }

                var isMemStateCommit = memoryRegionInfo.State == Enums.MemoryState.MEM_COMMIT;
                var isMemProtectGruad = memoryRegionInfo.Protect == Enums.Protection.PAGE_GUARD;

                if (getAll || (isMemStateCommit && !isMemProtectGruad))
                {
                    if (getAll || !nativeModules.Any(module => (ulong)module.BaseAddress <= (ulong)memoryRegionInfo.BaseAddress && (ulong)(module.BaseAddress + module.ModuleMemorySize) >= (ulong)memoryRegionInfo.BaseAddress))
                        memoryRegions.Add(memoryRegionInfo);
                }

                var nextAddress = (long)memoryRegionInfo.BaseAddress + (long)memoryRegionInfo.RegionSize;
                if (address == nextAddress)
                    break;
                address = nextAddress;
            } while (address <= MaxAddress);
            return memoryRegions;
        }

        /// <summary>
        ///   Allocate memory in the open process.
        /// </summary>
        /// <param name="iSize">Size of memory to allocate.</param>
        public uint Allocate(uint iSize)
        {
            if (_hProcess == IntPtr.Zero)
            {
                return 0;
            }

            return (uint)NativeMethods.VirtualAllocEx(_hProcess, IntPtr.Zero, iSize, Enums.AllocationType.Commit, Enums.Protection.PAGE_READWRITE);
        }

        /// <summary>
        ///   Indicates whether or not the memory is available for reading.
        /// </summary>
        public bool Available()
        {
            try
            {
                // When no process identifier is available, it is false.
                if (_iProcessID == 0) return false;

                // When the process can no longer be found, it is false.
                if (Process.GetProcessById(_iProcessID) == null) return false;

                // Otherwise return true!
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///   Find the base address of the provided module.
        /// </summary>
        /// <param name="zModuleName">Contains the module name to retrieve.</param>
        public uint GetModuleBaseAddress(string zModuleName)
        {
            try
            {
                Process[] hProcessList = Process.GetProcessesByName(_zProcessName);

                foreach (Process hProcess in hProcessList)
                {
                    if (_iProcessID != 0 && hProcess.Id != _iProcessID)
                    {
                        continue;
                    }

                    foreach (ProcessModule Module in hProcess.Modules)
                    {
                        if (zModuleName.Equals(Module.ModuleName, StringComparison.CurrentCultureIgnoreCase))
                        {
                            _mModule = Module;
                            return (uint)Module.BaseAddress;
                        }
                    }
                }
            }
            catch
            {
                return 0;
            }

            return 0;
        }

        /// <summary>
        ///   Sets the active window to the opened process window handle.
        /// </summary>
        public bool SetActiveWindowHandle()
        {
            return NativeMethods.SetForegroundWindow(_iProcessWindow);
        }

        #endregion Methods

        #region IDisposable

        public void Dispose()
        {
            Close();
        }

        #endregion IDisposable
    }
}