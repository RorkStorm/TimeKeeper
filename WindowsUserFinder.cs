using murrayju.ProcessExtensions;
using NLog;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Topshelf.Logging;

namespace TimeKeeper
{
    internal class WindowsUserFinder
    {
        [DllImport("Wtsapi32.dll")]
        private static extern bool WTSQuerySessionInformation(IntPtr hServer, int sessionId, WtsInfoClass wtsInfoClass, out IntPtr ppBuffer, out int pBytesReturned);

        [DllImport("Wtsapi32.dll")]
        private static extern void WTSFreeMemory(IntPtr pointer);

        [DllImport("Wtsapi32.dll", SetLastError = true)]
        private static extern bool WTSLogoffSession(IntPtr hServer, int sessionId, bool bWait);

        [DllImport("user32.dll")]
        public static extern void LockWorkStation();

        [DllImport("Wtsapi32.dll")]
        private static extern bool WTSQueryUserToken(int SessionId, out IntPtr pToken);

        public enum WtsInfoClass
        {
            WTSInitialProgram,
            WTSApplicationName,
            WTSWorkingDirectory,
            WTSOEMId,
            WTSSessionId,
            WTSUserName,
            WTSWinStationName,
            WTSDomainName,
            WTSConnectState,
            WTSClientBuildNumber,
            WTSClientName,
            WTSClientDirectory,
            WTSClientProductId,
            WTSClientHardwareId,
            WTSClientAddress,
            WTSClientDisplay,
            WTSClientProtocolType,
            WTSIdleTime,
            WTSLogonTime,
            WTSIncomingBytes,
            WTSOutgoingBytes,
            WTSIncomingFrames,
            WTSOutgoingFrames,
            WTSClientInfo,
            WTSSessionInfo,
        }

        private static readonly LogWriter logger = HostLogger.Get<WindowsUserFinder>();

        public static string GetUsernameBySessionId(int sessionId, bool prependDomain)
        {
            if (sessionId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sessionId), "Session ID must be a non-negative integer.");
            }

            string username = GetSessionInfo(sessionId, WtsInfoClass.WTSUserName);
            if (string.IsNullOrEmpty(username))
            {
                return "SYSTEM";
            }

            if (prependDomain)
            {
                string domainName = GetSessionInfo(sessionId, WtsInfoClass.WTSDomainName);
                if (!string.IsNullOrEmpty(domainName))
                {
                    username = $"{domainName}\\{username}";
                }
            }

            return username;
        }

        private static string GetSessionInfo(int sessionId, WtsInfoClass infoClass)
        {
            IntPtr buffer = IntPtr.Zero;
            int strLen = 0;
            try
            {
                if (WTSQuerySessionInformation(IntPtr.Zero, sessionId, infoClass, out buffer, out strLen) && strLen > 1)
                {
                    return Marshal.PtrToStringAnsi(buffer);
                }
            }
            catch (Exception ex)
            {
                logger.Debug($"Failed to query session information for session {sessionId}: {ex.Message}");
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                {
                    WTSFreeMemory(buffer);
                }
            }
            return null;
        }

        public static bool ForceLogout(int sessionId)
        {
            if (sessionId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sessionId), "Session ID must be a non-negative integer.");
            }

            try
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo("logoff", sessionId.ToString())
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                using (Process process = Process.Start(processStartInfo))
                {
                    process.WaitForExit();
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.Debug($"Failed to log off session {sessionId}: {ex.Message}");
                return false;
            }
        }

        public static bool ForceLock()
        {
            try
            {
                LockWorkStation();

                return true;
            }
            catch (Exception ex)
            {
                logger.Debug($"Failed to lock session : {ex.Message}");
                return false;
            }
        }

        public static bool ForceLockFromSessionId(int sessionId)
        {
            try
            {
                Directory.SetCurrentDirectory(Environment.SystemDirectory);
                return ProcessExtensions.StartProcessAsCurrentUser("Lock.bat");

                //return ProcessExtensions.StartProcessAsCurrentUser(@"C:\WINDOWS\system32\rundll32.exe", cmdLine:"user32.dll,LockWorkStation");
            }
            catch (Exception ex)
            {
                logger.Debug($"Failed to lock session: {ex.Message}");
            }
            return false;
        }
    }
}