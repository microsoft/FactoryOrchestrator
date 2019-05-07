using Microsoft.FactoryTestFramework.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Management.Deployment;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Microsoft.FactoryTestFramework.UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class UtilitiesPage : Page
    {
        public UtilitiesPage()
        {
            this.InitializeComponent();
            packages = new List<Windows.ApplicationModel.Package>();
            packageStrings = new List<string>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            BackButton.IsEnabled = this.Frame.CanGoBack;

            // Get installed UWPs
            var pkgManager = new PackageManager();
            packages = pkgManager.FindPackagesForUserWithPackageTypes(string.Empty, PackageTypes.Main).ToList();
            packageStrings = packages.Select(x => x.Id.FamilyName).ToList();

            // todo: quality: bind properly with template
            PackageList.ItemsSource = packageStrings;
            base.OnNavigatedTo(e);
        }

        private List<Windows.ApplicationModel.Package> packages;
        private List<string> packageStrings;

        private async void PackageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PackageList.SelectedIndex != -1)
            {
                var run = await IPCClientHelper.IpcClient.InvokeAsync(x => x.RunUWPOutsideTestList((string)PackageList.SelectedItem));
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            On_BackRequested();
        }

        private bool On_BackRequested()
        {
            if (this.Frame.CanGoBack)
            {
                this.Frame.GoBack();
                return true;
            }
            return false;
        }

        private void BackInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            On_BackRequested();
            args.Handled = true;
        }
    }

}
