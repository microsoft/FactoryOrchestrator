using Microsoft.FactoryOrchestrator.Client;
using System.Collections.Generic;
using System.Linq;
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
            packageStrings = new List<string>();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            // Get installed UWPs
            var pkgManager = new PackageManager();
            packageStrings = await IPCClientHelper.IpcClient.InvokeAsync(x => x.GetInstalledApps());

            // todo: quality: bind properly with template
            PackageList.ItemsSource = packageStrings;
            PackageList.UpdateLayout();
            base.OnNavigatedTo(e);
        }

        private async void PackageList_ItemClick(object sender, ItemClickEventArgs e)
        {
            ((App)Application.Current).uwpRunGuidFromAppsPage = (await IPCClientHelper.IpcClient.InvokeAsync(x => x.RunApp((string)e.ClickedItem))).Guid;
        }

        private List<string> packageStrings;
    }
}
