// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
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
            try
            {
                _ = await Client.GetInstalledApps();
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
                string url = "http://" + ipAddress + ":" + await Client.GetWdpHttpPort();
                Uri myUri = new Uri(url);
                wdp.Navigate(myUri);
            }
            else
            {
                ContentDialog failedAppsDialog = new ContentDialog
                {
                    Title = resourceLoader.GetString("WDPFailedTitle"),
                    Content = resourceLoader.GetString("WDPFailedContent"),
                    CloseButtonText = resourceLoader.GetString("Ok")
                };

                _ = await failedAppsDialog.ShowAsync();
                base.OnNavigatedTo(e);
            }
        }
        
        private readonly FactoryOrchestratorUWPClient Client = ((App)Application.Current).Client;
        private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();
    }
}
