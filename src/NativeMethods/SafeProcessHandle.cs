using System;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;

namespace WinAPI
{
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    public sealed class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeProcessHandle()
            : base(true)
        {
        }

        public SafeProcessHandle(IntPtr preexistingHandle, bool ownsHandle)
            : base(ownsHandle)
        {
            base.SetHandle(preexistingHandle);
        }

        protected override bool ReleaseHandle()
        {
            return NativeMethods.CloseHandle(base.handle);
        }
    }
}