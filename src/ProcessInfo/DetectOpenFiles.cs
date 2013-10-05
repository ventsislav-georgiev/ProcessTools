using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using WinAPI;

namespace ProcessInfo
{
    public class DetectOpenFiles
    {
        private const string NetworkDevicePrefix = "\\Device\\LanmanRedirector\\";

        private const int MaxPath = 260;

        private const int HandleTypeTokenCount = 27;
        private static Dictionary<string, string> _deviceMap;

        private static readonly string[] HandleTypeTokens = new[]
            {
                "", "", "Directory", "SymbolicLink", "Token",
                "Process", "Thread", "Unknown7", "Event", "EventPair", "Mutant",
                "Unknown11", "Semaphore", "Timer", "Profile", "WindowStation",
                "Desktop", "Section", "Key", "Port", "WaitablePort",
                "Unknown21", "Unknown22", "Unknown23", "Unknown24",
                "IoCompletion", "File"
            };

        /// <summary>
        ///   Gets the open files enumerator.
        /// </summary>
        /// <param name="processId">The process id.</param>
        /// <returns></returns>
        private static IEnumerable<String> GetOpenFilesEnumerator(int processId)
        {
            return new OpenFiles(processId);
        }

        public static List<Process> GetProcessesUsingFile(string filePath)
        {
            return (from p in Process.GetProcesses()
                    let openedFiles = GetOpenFilesEnumerator(p.Id).ToArray()
                    where openedFiles.Contains(filePath)
                    select p).ToList();
        }

        private sealed class OpenFiles : IEnumerable<String>
        {
            private readonly int _processId;

            internal OpenFiles(int processId)
            {
                _processId = processId;
            }

            public IEnumerator<String> GetEnumerator()
            {
                Enums.NtStatus ret;
                int sysInfoLength = 0x10000;

                // Loop, probing for required memory.

                do
                {
                    IntPtr sysInfoPtr = IntPtr.Zero;
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try
                    {
                        RuntimeHelpers.PrepareConstrainedRegions();
                        try
                        {
                        }
                        finally
                        {
                            // CER guarantees that the address of the allocated
                            // memory is actually assigned to ptr if an
                            // asynchronous exception occurs.
                            sysInfoPtr = Marshal.AllocHGlobal(sysInfoLength);
                        }
                        int returnLength;
                        ret =
                            NativeMethods.NtQuerySystemInformation(
                                Enums.SystemInformationClass.SystemHandleInformation, sysInfoPtr, sysInfoLength,
                                out returnLength);
                        if (ret == Enums.NtStatus.StatusInfoLengthMismatch)
                        {
                            // Round required memory up to the nearest 64KB boundary.
                            sysInfoLength = ((returnLength + 0xffff) & ~0xffff);
                        }
                        else if (ret == Enums.NtStatus.StatusSuccess)
                        {
                            int handleCount = Marshal.ReadInt32(sysInfoPtr);
                            int offset = 0;
                            int size = Marshal.SizeOf(typeof(SYSTEM_HANDLE_ENTRY));
                            for (int i = 0; i < handleCount; i++)
                            {
                                var sysInfoHandle = (IntPtr)((ulong)sysInfoPtr + (ulong)offset);
                                var handleEntry =
                                    (SYSTEM_HANDLE_ENTRY)
                                    Marshal.PtrToStructure(sysInfoHandle, typeof(SYSTEM_HANDLE_ENTRY));
                                if (handleEntry.OwnerPid == _processId)
                                {
                                    var handle = (IntPtr)handleEntry.HandleValue;
                                    SystemHandleType handleType;

                                    if (GetHandleType(handle, handleEntry.OwnerPid, out handleType) &&
                                        handleType == SystemHandleType.ObTypeFile)
                                    {
                                        string devicePath;
                                        if (GetFileNameFromHandle(handle, handleEntry.OwnerPid, out devicePath))
                                        {
                                            string dosPath;
                                            if (ConvertDevicePathToDosPath(devicePath, out dosPath))
                                            {
                                                if (File.Exists(dosPath))
                                                {
                                                    yield return dosPath; // return new FileInfo(dosPath);
                                                }
                                                else if (Directory.Exists(dosPath))
                                                {
                                                    yield return dosPath; // new DirectoryInfo(dosPath);
                                                }
                                            }
                                        }
                                    }
                                }
                                offset += size;
                            }
                        }
                    }
                    finally
                    {
                        // CER guarantees that the allocated memory is freed,
                        // if an asynchronous exception occurs.
                        Marshal.FreeHGlobal(sysInfoPtr);

                        //sw.Flush();
                        //sw.Close();
                    }
                } while (ret == Enums.NtStatus.StatusInfoLengthMismatch);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_HANDLE_ENTRY
        {
            public readonly int OwnerPid;
            public readonly byte ObjectType;
            public readonly byte HandleFlags;
            public readonly short HandleValue;
            public readonly int ObjectPointer;
            public readonly int AccessMask;
        }

        private enum SystemHandleType
        {
            ObTypeUnknown = 0,
            //ObTypeType = 1,
            //ObTypeDirectory,
            //ObTypeSymbolicLink,
            //ObTypeToken,
            //ObTypeProcess,
            //ObTypeThread,
            //ObTypeUnknown7,
            //ObTypeEvent,
            //ObTypeEventPair,
            //ObTypeMutant,
            //ObTypeUnknown11,
            //ObTypeSemaphore,
            //ObTypeTimer,
            //ObTypeProfile,
            //ObTypeWindowStation,
            //ObTypeDesktop,
            //ObTypeSection,
            //ObTypeKey,
            //ObTypePort,
            //ObTypeWaitablePort,
            //ObTypeUnknown21,
            //ObTypeUnknown22,
            //ObTypeUnknown23,
            //ObTypeUnknown24,

            //ObTypeController,
            //ObTypeDevice,
            //ObTypeDriver,

            ObTypeFile
        };

        #region Private Members

        private static bool GetFileNameFromHandle(IntPtr handle, int processId, out string fileName)
        {
            Process currentProcess = Process.GetCurrentProcess();
            IntPtr currentProcessHandle = currentProcess.Handle;
            int currentProcessId = currentProcess.Id;
            bool remote = (processId != currentProcessId);
            SafeProcessHandle processHandle = null;
            SafeObjectHandle objectHandle = null;
            try
            {
                if (remote)
                {
                    processHandle = NativeMethods.OpenProcess(Enums.ProcessAccessFlags.DupHandle, true, processId);
                    if (NativeMethods.DuplicateHandle(processHandle.DangerousGetHandle(), handle, currentProcessHandle,
                                                      out objectHandle, 0, false,
                                                      Enums.DuplicateHandleOptions.DuplicateSameAccess))
                    {
                        handle = objectHandle.DangerousGetHandle();
                    }
                }
                return GetFileNameFromHandle(handle, out fileName, 200);
            }
            finally
            {
                if (remote)
                {
                    if (processHandle != null)
                    {
                        processHandle.Close();
                    }
                    if (objectHandle != null)
                    {
                        objectHandle.Close();
                    }
                }
            }
        }

        private static bool GetFileNameFromHandle(IntPtr handle, out string fileName, int wait)
        {
            using (var f = new FileNameFromHandleState(handle))
            {
                ThreadPool.QueueUserWorkItem(GetFileNameFromHandle, f);
                if (f.WaitOne(wait))
                {
                    fileName = f.FileName;
                    return f.RetValue;
                }
                else
                {
                    fileName = string.Empty;
                    return false;
                }
            }
        }

        private static void GetFileNameFromHandle(object state)
        {
            var s = (FileNameFromHandleState)state;
            string fileName;
            s.RetValue = GetFileNameFromHandle(s.Handle, out fileName);
            s.FileName = fileName;
            s.Set();
        }

        private static bool GetFileNameFromHandle(IntPtr handle, out string fileName)
        {
            IntPtr ptr = IntPtr.Zero;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                int length = 0x200; // 512 bytes
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                }
                finally
                {
                    // CER guarantees the assignment of the allocated
                    // memory address to ptr, if an ansynchronous exception
                    // occurs.
                    ptr = Marshal.AllocHGlobal(length);
                }
                Enums.NtStatus ret = NativeMethods.NtQueryObject(handle,
                                                                      Enums.ObjectInformationClass
                                                                                .ObjectNameInformation, ptr, length,
                                                                      out length);
                if (ret == Enums.NtStatus.StatusBufferOverflow)
                {
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try
                    {
                    }
                    finally
                    {
                        // CER guarantees that the previous allocation is freed,
                        // and that the newly allocated memory address is
                        // assigned to ptr if an asynchronous exception occurs.
                        Marshal.FreeHGlobal(ptr);
                        ptr = Marshal.AllocHGlobal(length);
                    }
                    ret = NativeMethods.NtQueryObject(handle, Enums.ObjectInformationClass.ObjectNameInformation,
                                                      ptr, length, out length);
                }
                if (ret == Enums.NtStatus.StatusSuccess)
                {
                    ulong offset = 0x10;
                    fileName = Marshal.PtrToStringUni((IntPtr)((ulong)ptr + offset));
                    return fileName.Length != 0;
                }
            }
            finally
            {
                // CER guarantees that the allocated memory is freed,
                // if an asynchronous exception occurs.
                Marshal.FreeHGlobal(ptr);
            }

            fileName = string.Empty;
            return false;
        }

        private static bool GetHandleType(IntPtr handle, int processId, out SystemHandleType handleType)
        {
            string token = GetHandleTypeToken(handle, processId);
            return GetHandleTypeFromToken(token, out handleType);
        }

        private static bool GetHandleType(IntPtr handle, out SystemHandleType handleType)
        {
            string token = GetHandleTypeToken(handle);
            return GetHandleTypeFromToken(token, out handleType);
        }

        private static bool GetHandleTypeFromToken(string token, out SystemHandleType handleType)
        {
            for (int i = 1; i < HandleTypeTokenCount; i++)
            {
                if (HandleTypeTokens[i] == token)
                {
                    handleType = (SystemHandleType)i;
                    return true;
                }
            }
            handleType = SystemHandleType.ObTypeUnknown;
            return false;
        }

        private static string GetHandleTypeToken(IntPtr handle, int processId)
        {
            Process currentProcess = Process.GetCurrentProcess();
            IntPtr currentProcessHandle = currentProcess.Handle;
            int currentProcessID = currentProcess.Id;
            bool remote = (processId != currentProcessID);
            SafeProcessHandle processHandle = null;
            SafeObjectHandle objectHandle = null;
            try
            {
                if (remote)
                {
                    processHandle = NativeMethods.OpenProcess(Enums.ProcessAccessFlags.DupHandle, true, processId);
                    if (NativeMethods.DuplicateHandle(processHandle.DangerousGetHandle(), handle, currentProcessHandle,
                                                      out objectHandle, 0, false,
                                                      Enums.DuplicateHandleOptions.DuplicateSameAccess))
                    {
                        handle = objectHandle.DangerousGetHandle();
                    }
                }
                return GetHandleTypeToken(handle);
            }
            finally
            {
                if (remote)
                {
                    if (processHandle != null)
                    {
                        processHandle.Close();
                    }
                    if (objectHandle != null)
                    {
                        objectHandle.Close();
                    }
                }
            }
        }

        private static string GetHandleTypeToken(IntPtr handle)
        {
            int length;
            NativeMethods.NtQueryObject(handle, Enums.ObjectInformationClass.ObjectTypeInformation, IntPtr.Zero,
                                        0, out length);
            IntPtr ptr = IntPtr.Zero;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                }
                finally
                {
                    ptr = Marshal.AllocHGlobal(length);
                }
                if (
                    NativeMethods.NtQueryObject(handle, Enums.ObjectInformationClass.ObjectTypeInformation, ptr,
                                                length, out length) == Enums.NtStatus.StatusSuccess)
                {
                    ulong offset = 0x68;
                    string token = Marshal.PtrToStringUni((IntPtr)((ulong)ptr + offset));
                    return token;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return string.Empty;
        }

        private static bool ConvertDevicePathToDosPath(string devicePath, out string dosPath)
        {
            EnsureDeviceMap();
            int i = devicePath.Length;
            while (i > 0 && (i = devicePath.LastIndexOf('\\', i - 1)) != -1)
            {
                string drive;
                if (_deviceMap.TryGetValue(devicePath.Substring(0, i), out drive))
                {
                    dosPath = string.Concat(drive, devicePath.Substring(i));
                    return dosPath.Length != 0;
                }
            }
            dosPath = string.Empty;
            return false;
        }

        private static void EnsureDeviceMap()
        {
            if (_deviceMap == null)
            {
                Dictionary<string, string> localDeviceMap = BuildDeviceMap();
                Interlocked.CompareExchange(ref _deviceMap, localDeviceMap, null);
            }
        }

        private static Dictionary<string, string> BuildDeviceMap()
        {
            string[] logicalDrives = Environment.GetLogicalDrives();
            var localDeviceMap = new Dictionary<string, string>(logicalDrives.Length);
            var lpTargetPath = new StringBuilder(MaxPath);
            foreach (string drive in logicalDrives)
            {
                string lpDeviceName = drive.Substring(0, 2);
                NativeMethods.QueryDosDevice(lpDeviceName, lpTargetPath, MaxPath);
                localDeviceMap.Add(NormalizeDeviceName(lpTargetPath.ToString()), lpDeviceName);
            }
            localDeviceMap.Add(NetworkDevicePrefix.Substring(0, NetworkDevicePrefix.Length - 1), "\\");
            return localDeviceMap;
        }

        private static string NormalizeDeviceName(string deviceName)
        {
            if (
                string.Compare(deviceName, 0, NetworkDevicePrefix, 0, NetworkDevicePrefix.Length,
                               StringComparison.InvariantCulture) == 0)
            {
                string shareName = deviceName.Substring(deviceName.IndexOf('\\', NetworkDevicePrefix.Length) + 1);
                return string.Concat(NetworkDevicePrefix, shareName);
            }
            return deviceName;
        }

        private class FileNameFromHandleState : IDisposable
        {
            private readonly IntPtr _handle;
            private readonly ManualResetEvent _mr;

            public FileNameFromHandleState(IntPtr handle)
            {
                _mr = new ManualResetEvent(false);
                _handle = handle;
            }

            public IntPtr Handle
            {
                get { return _handle; }
            }

            public string FileName { get; set; }

            public bool RetValue { get; set; }

            public bool WaitOne(int wait)
            {
                return _mr.WaitOne(wait, false);
            }

            public void Set()
            {
                try
                {
                    _mr.Set();
                }
                catch
                {
                }
            }

            #region IDisposable Members

            public void Dispose()
            {
                if (_mr != null)
                    _mr.Close();
            }

            #endregion IDisposable Members
        }

        #endregion Private Members
    }
}