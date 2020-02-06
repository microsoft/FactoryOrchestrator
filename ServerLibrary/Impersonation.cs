using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace Microsoft.FactoryOrchestrator.Server
{
    public static class Impersonation
    {
        public static readonly HttpClient WdpHttpClient = new HttpClient();

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int LogonUser(String lpszUsername, String lpszDomain, String lpszPassword,
            int dwLogonType, int dwLogonProvider, out SafeAccessTokenHandle phToken);

        public static SafeAccessTokenHandle Logon()
        {
            SafeAccessTokenHandle safeTokenHandle;
            const int LOGON32_PROVIDER_DEFAULT = 0;
            //This parameter causes LogonUser to create a primary token.
            const int LOGON32_LOGON_INTERACTIVE = 2;

            // Call LogonUser to obtain a handle to an access token.
            int returnValue = LogonUser("DefaultUser", ".", "WindowsCore",
                LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT,
                out safeTokenHandle);

            if (0 == returnValue)
            {
                int err = Marshal.GetLastWin32Error();
                throw new System.ComponentModel.Win32Exception(err);
            }

            return safeTokenHandle;
        }

        public static T RunFuncImpersonated<T>(Func<T> action)
        {
            using (var safeTokenHandle = Logon())
            {
                T ret;

                // Use the token handle returned by LogonUser.
                ret = WindowsIdentity.RunImpersonated<T>(safeTokenHandle, action);
                return ret;
            }
        }

        public static void RunActionImpersonated(Action action)
        {
            using (var safeTokenHandle = Logon())
            {

                // Use the token handle returned by LogonUser.
                WindowsIdentity.RunImpersonated(safeTokenHandle, action);
            }
        }
    }
}