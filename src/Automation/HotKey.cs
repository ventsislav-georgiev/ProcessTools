using System;
using WinAPI;

namespace Automation
{
    public class HotKey : IDisposable
    {
        private bool _disposed = false;

        public Enums.VirtualKeyCode Key { get; private set; }

        public Enums.KeyModifier KeyModifiers { get; private set; }

        public int Id { get; set; }

        // ******************************************************************
        public HotKey(Enums.VirtualKeyCode vkKey, Enums.KeyModifier keyModifiers, bool register = true)
        {
            Key = vkKey;
            KeyModifiers = keyModifiers;
            if (register)
            {
                Register();
            }
        }

        // ******************************************************************
        public bool Register()
        {
            int virtualKeyCode = (int)Key;
            Id = virtualKeyCode + ((int)KeyModifiers * 0x10000);
            return NativeMethods.RegisterHotKey(IntPtr.Zero, Id, (UInt32)KeyModifiers, (UInt32)virtualKeyCode); ;
        }

        // ******************************************************************
        public bool Unregister()
        {
            return NativeMethods.UnregisterHotKey(IntPtr.Zero, Id);
        }

        // ******************************************************************
        // Implement IDisposable.
        // Do not make this method virtual.
        // A derived class should not be able to override this method.
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        // ******************************************************************
        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be _disposed.
        // If disposing equals false, the method has been called by the
        // runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be _disposed.
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this._disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    Unregister();
                }

                // Note disposing has been done.
                _disposed = true;
            }
        }
    }
}