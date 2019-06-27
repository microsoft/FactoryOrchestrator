using Microsoft.FactoryOrchestrator.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Microsoft.FactoryOrchestrator.UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Disabled;
            Header.Text += IPCClientHelper.IsLocalHost ? " (Local Device)" : $" ({IPCClientHelper.IpAddress.ToString()})";
            lastNavTag = ((App)Application.Current).MainPageLastNavTag;
            // Add handler for ContentFrame navigation.
            ContentFrame.Navigated += On_ContentFrameNavigated;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // NavView doesn't load any page by default, so load home page.
            if (lastNavTag == null)
            {
                NavView.SelectedItem = NavView.MenuItems[0];
                ((App)Application.Current).MainPageLastNavTag = lastNavTag = ((NavigationViewItem)NavView.SelectedItem).Tag.ToString();
            }
            else
            {
                NavView.SelectedItem = NavView.MenuItems.Where(x => ((NavigationViewItem)x).Tag.ToString() == lastNavTag).First();
            }
            NavView_Navigate(lastNavTag, null);

            base.OnNavigatedTo(e);
        }

        private void On_ContentFrameNavigated(object sender, NavigationEventArgs e)
        {
            //if (ContentFrame.SourcePageType == typeof(SettingsPage))
            //{
            //    // SettingsItem is not part of NavView.MenuItems, and doesn't have a Tag.
            //    NavView.SelectedItem = (NavigationViewItem)NavView.SettingsItem;
            //    NavView.Header = "Settings";
            //}
            //else
            if(ContentFrame.SourcePageType != null)
            {
                var item = navViewPages.FirstOrDefault(p => (p.Page == e.SourcePageType));

                NavView.SelectedItem = NavView.MenuItems.OfType<NavigationViewItem>().
                                                         First(n => n.Tag.Equals(item.Tag));

                if (item.Tag == "console")
                {
                    NavView.Header = "Command Prompt";
                }
                else
                {
                    NavView.Header = ((NavigationViewItem)NavView.SelectedItem)?.Content?.ToString();
                }
            }
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                NavView_Navigate("settings", args.RecommendedNavigationTransitionInfo);
            }
            else if (args.InvokedItemContainer != null)
            {
                var navItemTag = args.InvokedItemContainer.Tag.ToString();
                NavView_Navigate(navItemTag, args.RecommendedNavigationTransitionInfo);
            }
        }

        private void NavView_Navigate(string navItemTag, NavigationTransitionInfo recommendedNavigationTransitionInfo)
        {
            ((App)Application.Current).MainPageLastNavTag = lastNavTag = navItemTag;

            Type page;
            //if (navItemTag == "settings")
            //{
            //    page = typeof(SettingsPage);
            //}
            //else
            {
                page = navViewPages.FirstOrDefault(p => p.Tag.Equals(navItemTag)).Page;
            }
             
            // Get the page type before navigation so you can prevent duplicate entries in the backstack.
            var preNavPageType = ContentFrame.CurrentSourcePageType;

            // Only navigate if the selected page isn't currently loaded.
            if (!(page is null) && !Type.Equals(preNavPageType, page))
            {
                ContentFrame.Navigate(page, this.Frame, recommendedNavigationTransitionInfo);
            }
        }

        private void ConfirmExit_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).Exit();
        }
        private void ConfirmReboot_Click(object sender, RoutedEventArgs e)
        {
            IPCClientHelper.RebootServerDevice();
        }

        private void ConfirmShutdown_Click(object sender, RoutedEventArgs e)
        {
            IPCClientHelper.ShutdownServerDevice();
        }
        private async void NetworkFlyout_Opening(object sender, object e)
        {
            var tuples = await GetIpAddresses();
            NetworkStackPanel.Children.Clear();
            foreach (var ipAndNic in tuples)
            {
                NetworkStackPanel.Children.Add(new TextBlock()
                {
                    Text = $"{ipAndNic.Item2} : {ipAndNic.Item1}",
                    IsTextSelectionEnabled = true
                });
            }
        }

        private async Task<List<Tuple<string, string>>> GetIpAddresses()
        {
            return await IPCClientHelper.IpcClient.InvokeAsync(x => x.GetIpAddressesAndNicNames());
        }

        private string lastNavTag;
        private readonly List<(string Tag, Type Page)> navViewPages = new List<(string Tag, Type Page)>
        {
            ("run", typeof(TaskListExecutionPage)),
            ("console", typeof(ConsolePage)),
            ("apps", typeof(AppsPage)),
            ("save", typeof(SaveLoadEditPage)),
            ("files", typeof(FileTransferPage)),
            ("about", typeof(AboutPage))
        };
    }

}
