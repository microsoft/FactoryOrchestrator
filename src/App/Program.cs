using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace Microsoft.FactoryOrchestrator.UWP
{
    public static class Program
    {
        static void Main(string[] args)
        {
            bool startNew = false;
            AppInstance current = null;

            var familystring = Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily.ToString();
            if (familystring.Contains("desktop", StringComparison.InvariantCultureIgnoreCase))
            {
                // Always start a new instance when invoked on desktop
                startNew = true;
            }
            else
            {
                // Only start a new instance on other OSes if none are running.
                var instances = AppInstance.GetInstances();
                if (instances.Count == 0)
                {
                    startNew = true;
                }
                else
                {
                    current = instances[0];
                }
            }

            if (startNew)
            {
                AppInstance.FindOrRegisterInstanceForKey("true");
#pragma warning disable CA1806 // Do not ignore method results
                global::Windows.UI.Xaml.Application.Start((p) => new App());
#pragma warning restore CA1806 // Do not ignore method results
            }
            else if (current != null)
            {
                current.RedirectActivationTo();
            }

        }
    }

}
