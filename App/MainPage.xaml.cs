﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Windows.UI.Core;
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
            Client = ((App)Application.Current).Client;

            // Put Client ipaddress in header
            Header.Text += Client.IsLocalHost ? " (Local Device)" : $" ({Client.IpAddress.ToString()})";

            // If localhost connection, hide file transfer page
            if (Client.IsLocalHost)
            {
                NavigationViewItem fileItem = (NavigationViewItem)NavView.MenuItems.Where(x => ((NavigationViewItem)x).Tag.ToString() == "files").First();
                fileItem.Visibility = Visibility.Collapsed;
                fileItem.IsEnabled = false;
                var pageMap = navViewPages.Where(x => x.Tag == "files").First();
                navViewPages.Remove(pageMap);
                pageMap.Enabled = false;
                navViewPages.Add(pageMap);
            }

            // Update visible network information every 7 seconds
            networkTimer = new System.Timers.Timer(7000);
            networkTimerIndex = 0;
            ipAddressSem = new SemaphoreSlim(1, 1);

            // If there was a previous tab loaded, navigate to it
            lastNavTag = ((App)Application.Current).MainPageLastNavTag;

            // Add handler for ContentFrame navigation.
            ContentFrame.Navigated += On_ContentFrameNavigated;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            Client = ((App)Application.Current).Client;
            this.Frame.CacheSize = 3;
            // Hide tabs disabled by OEM Customization
            List<string> disabledPages = await Client.GetDisabledPages();
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

            // Put OS & OEM versions in the footer
            OEMVersionHeader.Text = "OEM Version: ";
            OSVersionHeader.Text = "OS Version: ";
            try
            {
                OSVersionHeader.Text += await Client.GetOSVersionString();
            }
            catch (Exception)
            {
                OSVersionHeader.Text += $"Could not query OS version!";
            }
            try
            {
                OEMVersionHeader.Text += await Client.GetOEMVersionString();
            }
            catch (Exception)
            {
                OEMVersionHeader.Text += $"Could not query OEM version!";
            }

            // Configure network information update timer
            await UpdateIpAddresses();
            networkTimer.Elapsed += NetworkTimer_Elapsed;
            // Call elapsed to set initial values
            NetworkTimer_Elapsed(networkTimer, null);
            networkTimer.Start();

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
            if (ContentFrame.SourcePageType != null)
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
            if (args.InvokedItemContainer != null)
            {
                var navItemTag = args.InvokedItemContainer.Tag.ToString();
                NavView_Navigate(navItemTag, args.RecommendedNavigationTransitionInfo);
            }
        }

        private void NavView_Navigate(string navItemTag, NavigationTransitionInfo recommendedNavigationTransitionInfo, NavigationEventArgs e = null)
        {
            ((App)Application.Current).MainPageLastNavTag = lastNavTag = navItemTag;
            Type page = null;
            var pageMap = navViewPages.FirstOrDefault(p => p.Tag.Equals(navItemTag));

            if (pageMap.Enabled)
            {
                // Dont navigate unless the page is enabled
                page = pageMap.Page;
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
            Client.RebootDevice(5);
            ShutdownProgessBar.Visibility = Visibility.Visible;
            ConfirmReboot.IsEnabled = false;
            ConfirmShutdown.IsEnabled = false;
            ConfirmExit.IsEnabled = false;
        }

        private void ConfirmShutdown_Click(object sender, RoutedEventArgs e)
        {
            Client.ShutdownDevice(5);
            ShutdownProgessBar.Visibility = Visibility.Visible;
            ConfirmReboot.IsEnabled = false;
            ConfirmShutdown.IsEnabled = false;
            ConfirmExit.IsEnabled = false;
        }
        private async void NetworkFlyout_Opening(object sender, object e)
        {
            await UpdateIpAddresses();
            await ipAddressSem.WaitAsync();
            NetworkStackPanel.Children.Clear();

            foreach (var ipAndNic in ipAddresses)
            {
                NetworkStackPanel.Children.Add(new TextBlock()
                {
                    Text = $"{ipAndNic.Item2} : {ipAndNic.Item1}",
                    IsTextSelectionEnabled = true
                });
            }

            ipAddressSem.Release();
        }
        private void ExitFlyout_Closed(object sender, object e)
        {
            ConfirmExit.IsEnabled = true;
            ConfirmReboot.IsEnabled = true;
            ConfirmShutdown.IsEnabled = true;
            ShutdownProgessBar.Visibility = Visibility.Collapsed;
        }

        private async void NetworkTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            await UpdateIpAddresses();
            await ipAddressSem.WaitAsync();
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (ipAddresses.Count == 0)
                {
                    NetworkIp.Text = "";
                    NetworkName.Text = "";
                    return;
                }

                if (ipAddresses.Count <= networkTimerIndex)
                {
                    networkTimerIndex = 0;
                }

                var ipAndName = ipAddresses[networkTimerIndex++];
                NetworkIp.Text = ipAndName.Item1;
                NetworkName.Text = ipAndName.Item2;
            });
            ipAddressSem.Release();
        }

        private async Task UpdateIpAddresses()
        {
            await ipAddressSem.WaitAsync();
            ipAddresses = await Client.GetIpAddressesAndNicNames();
            ipAddressSem.Release();
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

        private FactoryOrchestratorUWPClient Client;
        private System.Timers.Timer networkTimer;
        private int networkTimerIndex;
        private List<Tuple<string, string>> ipAddresses;
        private SemaphoreSlim ipAddressSem;
    }

}
