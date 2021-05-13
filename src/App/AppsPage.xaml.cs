// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.FactoryOrchestrator.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.Management.Deployment;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Microsoft.FactoryOrchestrator.UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AppsPage : Page
    {
        public AppsPage()
        {
            this.InitializeComponent();
            PackageStrings = new List<string>();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            // Get installed UWPs
            try
            {
                var packageInfos = (await Client.GetInstalledAppsDetailed()).OrderBy(x => x.Name);
                PackageStrings = new List<string>();
                foreach (var pkg in packageInfos)
                {
                    PackageStrings.Add($"{pkg.Name} ({pkg.AppId})");
                }

                LoadingRing.IsActive = false;
                PackageList.ItemsSource = PackageStrings;
            }
            catch (Exception ex)
            {
                LoadingRing.IsActive = false;
                ContentDialog failedAppsDialog = new ContentDialog
                {
                    Title = resourceLoader.GetString("FailedAppsQuery"),
                    Content = ex.Message,
                    CloseButtonText = resourceLoader.GetString("Ok")
                };

                ContentDialogResult result = await failedAppsDialog.ShowAsync();
            }

            base.OnNavigatedTo(e);
        }

        private async void PackageList_ItemClick(object sender, ItemClickEventArgs e)
        {
            string item = (string)e.ClickedItem;
            int start = item.LastIndexOf('(');
            string aumid = item.Substring(start + 1, item.Length - start - 2);
            await Client.RunApp(aumid);
        }

        public List<string> PackageStrings { get; private set; }
        private FactoryOrchestratorUWPClient Client = ((App)Application.Current).Client;
        private ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();
    }
}
