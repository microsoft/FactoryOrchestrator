using Microsoft.FactoryOrchestrator.Client;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel;
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
            PackageStrings = await Client.GetInstalledApps();
            PackageList.ItemsSource = PackageStrings;
            base.OnNavigatedTo(e);
        }

        private async void PackageList_ItemClick(object sender, ItemClickEventArgs e)
        {
            await Client.RunApp((string)e.ClickedItem);
        }

        public List<string> PackageStrings { get; private set; }
        private FactoryOrchestratorUWPClient Client = ((App)Application.Current).Client;
    }
}
