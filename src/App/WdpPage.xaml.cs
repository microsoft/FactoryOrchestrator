using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Microsoft.FactoryOrchestrator.UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WdpPage : Page
    {
        public WdpPage()
        {
            this.InitializeComponent();
            
        }
        
        private async Task<bool> IsWindowsDevicePortalRunning()
        {
            List<string> InstalledApps = new List<string>();
            try
            {
                InstalledApps = await Client.GetInstalledApps();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            
            if (await IsWindowsDevicePortalRunning())
            {
                string ipAddress = Client.IsLocalHost ? "localhost" : $"{Client.IpAddress.ToString()}";
                string url = "http://" + ipAddress + ":80";
                Uri myUri = new Uri(url);
                wdp.Navigate(myUri);
            }
            else
            {
                ContentDialog failedAppsDialog = new ContentDialog
                {
                    Title = "Failed to launch Windows Device Portal",
                    Content = "Make sure Windows Device Portal is running and try again.",
                    CloseButtonText = "Ok"
                };

                ContentDialogResult result = await failedAppsDialog.ShowAsync();
                base.OnNavigatedTo(e);
            }
        }
        
        private FactoryOrchestratorUWPClient Client = ((App)Application.Current).Client;
 
    }
}
