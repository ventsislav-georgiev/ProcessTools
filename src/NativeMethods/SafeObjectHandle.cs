using System;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;

namespace WinAPI
{
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    public sealed class SafeObjectHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeObjectHandle(IntPtr preexistingHandle, bool ownsHandle)
            : base(ownsHandle)
        {
            SetHandle(preexistingHandle);
        }

        protected override bool ReleaseHandle()
        {
            return NativeMethods.CloseHandle(handle);
        }
    }
}