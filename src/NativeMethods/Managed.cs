using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace WinAPI
{
    public static class Managed
    {
        public static Structs.WindowInfo GetWindowInfo(IntPtr windowHandle)
        {
            var windowInfo = new Structs.WindowInfo();
            windowInfo.cbSize = (uint)Marshal.SizeOf(windowInfo);
            NativeMethods.GetWindowInfo(windowHandle, ref windowInfo);
            return windowInfo;
        }

        public static string GetClassName(IntPtr handle)
        {
            StringBuilder stringBuilder = new StringBuilder(256);
            NativeMethods.GetClassName(handle, stringBuilder, 256);
            return stringBuilder.ToString();
        }

        public static Point GetCursorPosition()
        {
            Structs.POINT point;
            NativeMethods.GetCursorPos(out point);
            return new Point(point.X, point.Y);
        }

        /// <summary>
        /// Returns a list of all windows or all windows of the specified process
        /// </summary>
        public static ArrayList GetAllWindows(Process process = null)
        {
            var windowHandles = new ArrayList();
            NativeMethods.EnumedWindow callBackPtr = GotWindowHandleCallBack;

            if (process == null)
                NativeMethods.EnumWindows(callBackPtr, windowHandles);
            else
            {
                foreach (ProcessThread pThread in process.Threads)
                {
                    NativeMethods.EnumThreadWindows((uint)pThread.Id, GotWindowHandleCallBack, windowHandles);
                }
            }

            foreach (IntPtr windowHandle in windowHandles.ToArray())
            {
                NativeMethods.EnumChildWindows(windowHandle, callBackPtr, windowHandles);
            }

            return windowHandles;
        }

        public static bool GotWindowHandleCallBack(IntPtr windowHandle, ArrayList windowHandlesArray)
        {
            windowHandlesArray.Add(windowHandle);
            return true;
        }

        public static string GetWindowText(IntPtr windowHandle)
        {
            // Allocate correct string length first
            int length = NativeMethods.GetWindowTextLength(windowHandle);
            StringBuilder sb = new StringBuilder(length + 1);
            NativeMethods.GetWindowText(windowHandle, sb, sb.Capacity);
            return sb.ToString();
        }

        public static string GetWindowTextRaw(IntPtr windowHandle)
        {
            int textLength = (int)NativeMethods.SendMessage(windowHandle, (uint)Enums.WMessages.GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero);
            var stringBuilder = new StringBuilder(textLength + 1);
            NativeMethods.SendMessage(windowHandle, (uint)Enums.WMessages.GETTEXT, (IntPtr)stringBuilder.Capacity, stringBuilder);
            return stringBuilder.ToString();
        }

        public static void ShowWindow(IntPtr handle, bool hide = false)
        {
            const int SW_HIDE = 0;
            const int SW_RESTORE = 9;
            NativeMethods.ShowWindow(handle, hide ? SW_HIDE : SW_RESTORE);
        }

        public static uint GetWindowThreadProcessId(IntPtr handle)
        {
            return NativeMethods.GetWindowThreadProcessId(handle, IntPtr.Zero);
        }

        /// <summary>
        ///   Retrieve the active window handle.
        /// </summary>
        public static IntPtr GetActiveWindowHandle()
        {
            return NativeMethods.GetForegroundWindow();
        }

        #region Resume, Suspend

        /// <summary>
        ///   Resume or Suspend a processes list
        /// </summary>
        public static void ResumeOrSuspend(Process hProcess, bool isSuspend = true)
        {
            foreach (ProcessThread hThread in hProcess.Threads)
            {
                IntPtr ThreadHandle = NativeMethods.OpenThread(Enums.ThreadAccess.SuspendResume, false,
                                                               (uint)hThread.Id);
                if (isSuspend)
                    NativeMethods.SuspendThread(ThreadHandle);
                else
                    NativeMethods.ResumeThread(ThreadHandle);
            }
        }

        /// <summary>
        ///   Resumes all threads in the target process.
        /// </summary>
        public static void Resume(Process process)
        {
            ResumeOrSuspend(process, isSuspend: false);
        }

        /// <summary>
        ///   Suspends all threads in the target process.
        /// </summary>
        public static void Suspend(Process process)
        {
            ResumeOrSuspend(process, isSuspend: true);
        }

        #endregion Resume, Suspend

        #region IconExtraction

        /// <summary>
        ///   GetClassLongPtr (64 bit version loses significant specific 64-bit information)
        /// </summary>
        private static IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 4)
                return new IntPtr(NativeMethods.GetClassLong32(hWnd, nIndex));
            else
                return NativeMethods.GetClassLong64(hWnd, nIndex);
        }

        /// <summary>
        ///   Get process icon by handle
        /// </summary>
        public static Bitmap GetSmallWindowIcon(IntPtr hWnd)
        {
            try
            {
                var IDI_APPLICATION = new IntPtr(0x7F00);
                int GCL_HICON = -14;
                IntPtr hIcon = default(IntPtr);

                hIcon = NativeMethods.SendMessage(hWnd, (uint)Enums.WMessages.GETICON, new IntPtr(2), IntPtr.Zero);

                if (hIcon == IntPtr.Zero)
                    hIcon = GetClassLongPtr(hWnd, GCL_HICON);

                if (hIcon == IntPtr.Zero)
                    hIcon = NativeMethods.LoadIcon(IntPtr.Zero, IDI_APPLICATION);

                if (hIcon != IntPtr.Zero)
                    return new Bitmap(Icon.FromHandle(hIcon).ToBitmap(), 16, 16);
                else
                    return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static Bitmap ExtractIconFromExe(string file, bool large)
        {
            int readIconCount = 0;
            var hDummy = new IntPtr[1] { IntPtr.Zero };
            var hIconEx = new IntPtr[1] { IntPtr.Zero };

            try
            {
                if (large)
                    readIconCount = NativeMethods.ExtractIconEx(file, 0, hIconEx, hDummy, 1);
                else
                    readIconCount = NativeMethods.ExtractIconEx(file, 0, hDummy, hIconEx, 1);

                if (readIconCount > 0 && hIconEx[0] != IntPtr.Zero)
                    return new Bitmap(Icon.FromHandle(hIconEx[0]).ToBitmap(), 16, 16);
                else
                    return null;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Could not extract icon", ex);
            }
            finally
            {
                // RELEASE RESOURCES
                foreach (IntPtr ptr in hIconEx)
                    if (ptr != IntPtr.Zero)
                        NativeMethods.DestroyIcon(ptr);

                foreach (IntPtr ptr in hDummy)
                    if (ptr != IntPtr.Zero)
                        NativeMethods.DestroyIcon(ptr);
            }
        }

        private const uint SHGFI_ICON = 0x100;
        private const uint SHGFI_LARGEICON = 0x0;    // 'Large icon
        private const uint SHGFI_SMALLICON = 0x1;    // 'Small icon

        /// <summary>
        /// Get the icon used for a given, existing file.
        /// </summary>
        /// <param name="fileName">Name of the file</param>
        /// <param name="small">Whether to get the small icon instead of the large one</param>
        public static Icon GetIconForFilename(string fileName, bool small)
        {
            Structs.SHFILEINFO shinfo = new Structs.SHFILEINFO();

            if (small)
            {
                IntPtr hImgSmall = NativeMethods.SHGetFileInfo(fileName, 0, ref shinfo,
                                   (uint)Marshal.SizeOf(shinfo),
                                    SHGFI_ICON |
                                    SHGFI_SMALLICON);
            }
            else
            {
                IntPtr hImgLarge = NativeMethods.SHGetFileInfo(fileName, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo),
                    SHGFI_ICON | SHGFI_LARGEICON);
            }

            if (shinfo.hIcon == IntPtr.Zero) return null;

            System.Drawing.Icon myIcon = System.Drawing.Icon.FromHandle(shinfo.hIcon);
            return myIcon;
        }

        #endregion IconExtraction
    }
}