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
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
            //this.NavigationCacheMode = NavigationCacheMode.Disabled;

            // Put ipaddress in header
            Header.Text += IPCClientHelper.IsLocalHost ? " (Local Device)" : $" ({IPCClientHelper.IpAddress.ToString()})";

            // If localhost connection, hide file transfer page
            if (IPCClientHelper.IsLocalHost)
            {
                NavigationViewItem fileItem = (NavigationViewItem)NavView.MenuItems.Where(x => ((NavigationViewItem)x).Tag.ToString() == "files").First();
                fileItem.Visibility = Visibility.Collapsed;
                fileItem.IsEnabled = false;
                var pageMap = navViewPages.Where(x => x.Tag == "files").First();
                navViewPages.Remove(pageMap);
                pageMap.Enabled = false;
                navViewPages.Add(pageMap);
            }

            // If there was a previous tab loaded, navigate to it
            lastNavTag = ((App)Application.Current).MainPageLastNavTag;

            // Add handler for ContentFrame navigation.
            ContentFrame.Navigated += On_ContentFrameNavigated;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            this.Frame.CacheSize = 3;
            // Hide tabs disabled by OEM Customization
            List<string> disabledPages = await IPCClientHelper.IpcClient.InvokeAsync(x => x.GetDisabledPages());
            foreach (var disabledPage in disabledPages)
            {
                foreach (NavigationViewItem item in NavView.MenuItems)
                {
                    if (item.Tag.ToString() == disabledPage)
                    {
                        item.Visibility = Visibility.Collapsed;
                        item.IsEnabled = false;
                        var pageMap = navViewPages.Where(x => x.Tag == disabledPage).First();
                        navViewPages.Remove(pageMap);
                        pageMap.Enabled = false;
                        navViewPages.Add(pageMap);
                    }
                }
            }

            if (lastNavTag == null)
            {
                // NavView doesn't load any page by default, so load home page.
                NavView.SelectedItem = NavView.MenuItems[0];
                ((App)Application.Current).MainPageLastNavTag = lastNavTag = ((NavigationViewItem)NavView.SelectedItem).Tag.ToString();
            }
            else
            {
                NavView.SelectedItem = NavView.MenuItems.Where(x => ((NavigationViewItem)x).Tag.ToString() == lastNavTag).First();
            }

            NavView_Navigate(lastNavTag, null, e);

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

        private void NavView_Navigate(string navItemTag, NavigationTransitionInfo recommendedNavigationTransitionInfo, NavigationEventArgs e = null)
        {
            ((App)Application.Current).MainPageLastNavTag = lastNavTag = navItemTag;

            Type page = null;
            
            //if (navItemTag == "settings")
            //{
            //    page = typeof(SettingsPage);
            //}
            //else
            {
                var pageMap = navViewPages.FirstOrDefault(p => p.Tag.Equals(navItemTag));

                if (pageMap.Enabled)
                {
                    // Dont navigate unless the page is enabled
                    page = pageMap.Page;
                }
            }
             
            // Get the page type before navigation so you can prevent duplicate entries in the backstack.
            var preNavPageType = ContentFrame.CurrentSourcePageType;

            // Only navigate if the selected page isn't currently loaded or if we came from another full page view.
            if (!(page is null) && (!Type.Equals(preNavPageType, page) || e != null))
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
        private List<(string Tag, Type Page, bool Enabled)> navViewPages = new List<(string Tag, Type Page, bool Enabled)>
        {
            ("run", typeof(TaskListExecutionPage), true),
            ("console", typeof(ConsolePage), true),
            ("apps", typeof(AppsPage), true),
            ("save", typeof(SaveLoadEditPage), true),
            ("files", typeof(FileTransferPage), true),
            ("about", typeof(AboutPage), true)
        };
    }

}
