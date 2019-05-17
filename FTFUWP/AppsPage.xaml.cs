using Microsoft.FactoryTestFramework.Client;
using System.Collections.Generic;
using System.Linq;
using Windows.Management.Deployment;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Microsoft.FactoryTestFramework.UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AppsPage : Page
    {
        public AppsPage()
        {
            this.InitializeComponent();
            packages = new List<Windows.ApplicationModel.Package>();
            packageStrings = new List<string>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Get installed UWPs
            var pkgManager = new PackageManager();
            packages = pkgManager.FindPackagesForUserWithPackageTypes(string.Empty, PackageTypes.Main).ToList();
            packageStrings = packages.Select(x => x.Id.FamilyName).ToList();

            // todo: quality: bind properly with template
            PackageList.ItemsSource = packageStrings;
            PackageList.UpdateLayout();
            base.OnNavigatedTo(e);
        }

        private async void PackageList_ItemClick(object sender, ItemClickEventArgs e)
        {
            var run = await IPCClientHelper.IpcClient.InvokeAsync(x => x.RunUWPOutsideTestList((string)e.ClickedItem));
        }

        private List<Windows.ApplicationModel.Package> packages;
        private List<string> packageStrings;
    }
}
