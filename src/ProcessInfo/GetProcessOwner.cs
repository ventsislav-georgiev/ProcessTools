using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using WinAPI;

namespace ProcessInfo
{
    public class GetProcessOwner
    {
        /// <summary>
        /// Collect User Info
        /// </summary>
        /// <param name="pToken">Process Handle</param>
        /// <param name="sid"></param>
        private static bool DumpUserInfo(IntPtr pToken, out IntPtr sid)
        {
            const int access = 0X00000008; // TokenQuery
            IntPtr procToken = IntPtr.Zero;
            bool ret = false;
            sid = IntPtr.Zero;
            try
            {
                if (NativeMethods.OpenProcessToken(pToken, access, ref procToken))
                {
                    ret = ProcessTokenToSid(procToken, out sid);
                    NativeMethods.CloseHandle(procToken);
                }
                return ret;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool ProcessTokenToSid(IntPtr token, out IntPtr sid)
        {
            const int bufLength = 256;
            IntPtr tu = Marshal.AllocHGlobal(bufLength);
            sid = IntPtr.Zero;
            try
            {
                int cb = bufLength;
                bool ret = NativeMethods.GetTokenInformation(token, Enums.TokenInformationClass.TokenUser, tu, cb, ref cb);
                if (ret)
                {
                    var tokUser = (Structs.TokenUser)Marshal.PtrToStructure(tu, typeof(Structs.TokenUser));
                    sid = tokUser.User.Sid;
                }
                return ret;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                Marshal.FreeHGlobal(tu);
            }
        }

        public static bool ExGetProcessInfoByPid(int pid, out string sid) //, out string OwnerSID)
        {
            sid = String.Empty;
            try
            {
                Process process = Process.GetProcessById(pid);
                IntPtr sidPtr;
                if (DumpUserInfo(process.Handle, out sidPtr))
                {
                    NativeMethods.ConvertSidToStringSid(sidPtr, ref sid);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}