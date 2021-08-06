// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.FactoryOrchestrator.Core;
using Windows.ApplicationModel.Resources;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using MUXC = Microsoft.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Microsoft.FactoryOrchestrator.UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, IDisposable
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
            Client = ((App)Application.Current).Client;
            disabledPages = new List<string>();
            navUpdateSem = new SemaphoreSlim(1, 1);

            // Put Client ipaddress in header
            Header.Text += Client.IsLocalHost ? $" ({resourceLoader.GetString("LocalDevice")})" : $" ({Client.IpAddress.ToString()})";

            // Update visible network information every 7 seconds
            networkTimer = new System.Timers.Timer(7000);
            networkTimerIndex = 0;
            ipAddressSem = new SemaphoreSlim(1, 1);

            // If there was a previous tab loaded, navigate to it
            lastNavTag = ((App)Application.Current).MainPageLastNavTag;

            // Add handler for ContentFrame navigation.
            ContentFrame.Navigated += On_ContentFrameNavigated;

            ((App)Application.Current).OnServiceDoneExecutingBootTasks += MainPage_OnServiceDoneExecutingBootTasks;
            ((App)Application.Current).OnServiceStart += MainPage_OnServiceStart;
            ((App)Application.Current).PropertyChanged += MainPage_AppPropertyChanged;
        }

        private async void MainPage_AppPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("IsContainerRunning", StringComparison.Ordinal) && !disabledPages.Any(x => x.Equals("files", StringComparison.OrdinalIgnoreCase)))
            {
                await navUpdateSem.WaitAsync();
                try
                {
                    if (((App)Application.Current).IsContainerRunning && !navViewPages.Any(x => x.Tag == "files"))
                    {
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            ShowPage("files");
                        });
                    }
                    else if (!((App)Application.Current).IsContainerRunning && navViewPages.Any(x => x.Tag == "files"))
                    {
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            HidePage("files");
                        });
                    }
                }
                finally
                {
                    navUpdateSem.Release();
                }
            }
        }

        private async void MainPage_OnServiceStart()
        {
            await navUpdateSem.WaitAsync();
            try
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    BootTasksStart();
                });
            }
            finally
            {
                navUpdateSem.Release();
            }
        }

        private async void MainPage_OnServiceDoneExecutingBootTasks()
        {
            await navUpdateSem.WaitAsync();
            try
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    BootTasksDone();
                });
            }
            finally
            {
                navUpdateSem.Release();
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e == null)
            {
                lastNavTag = null;
            }
            else
            {
                lastNavTag = e.Parameter as string;
            }

            Client = ((App)Application.Current).Client;
            this.Frame.CacheSize = 3;

            await navUpdateSem.WaitAsync();
            try
            {
                // Hide tabs disabled by OEM Customization
                disabledPages = await Client.GetDisabledPages();

                if (Client.IsLocalHost)
                {
                    if (((App)Application.Current).IsContainerDisabled)
                    {
                        // If localhost connection and no container, disable file transfer page
                        disabledPages.Add("files");
                    }
                }

                foreach (var disabledPage in disabledPages)
                {
                    HidePage(disabledPage);
                }

                if (((App)Application.Current).IsServiceExecutingBootTasks)
                {
                    // Disable pages that are disallowed during Boot Tasks
                    BootTasksStart();
                }
                else
                {
                    // Enable all pages that are not disabled
                    BootTasksDone();
                }
            }
            finally
            {
                navUpdateSem.Release();
            }

            // Put OS & OEM versions in the header
            OEMVersionHeader.Text = $"{resourceLoader.GetString("OEMVersion")}: ";
            OSVersionHeader.Text = $"{resourceLoader.GetString("OSVersion")}: ";
            ServiceVersionHeader.Text = $"{resourceLoader.GetString("ServiceVersion")}: ";
            AppVersionHeader.Text = $"{resourceLoader.GetString("AppVersion")}: ";

            var assembly = Assembly.GetExecutingAssembly();
            string assemblyVersion = assembly.GetName().Version.ToString();

            AppVersionHeader.Text += assemblyVersion;

            try
            {
                OSVersionHeader.Text += await Client.GetOSVersionString();
            }
            catch (Exception)
            {
                OSVersionHeader.Text += $"{resourceLoader.GetString("CouldNotQuery")} {resourceLoader.GetString("OSVersion")}!";
            }
            try
            {
                OEMVersionHeader.Text += await Client.GetOEMVersionString();
            }
            catch (Exception)
            {
                OEMVersionHeader.Text += $"{resourceLoader.GetString("CouldNotQuery")} {resourceLoader.GetString("OEMVersion")}!";
            }
            try
            {
                ServiceVersionHeader.Text += await Client.GetServiceVersionString();
            }
            catch (Exception)
            {
                ServiceVersionHeader.Text += $"{resourceLoader.GetString("CouldNotQuery")} {resourceLoader.GetString("ServiceVersion")}!";
            }

            // Configure network information update timer
            await UpdateIpAddresses();
            networkTimer.Elapsed += NetworkTimer_Elapsed;
            // Call elapsed to set initial values
            NetworkTimer_Elapsed(networkTimer, null);
            networkTimer.Start();

            if (string.IsNullOrEmpty(lastNavTag))
            {
                // NavView doesn't load any page by default, so load home page.
                NavView.SelectedItem = NavView.MenuItems[0];
                ((App)Application.Current).MainPageLastNavTag = lastNavTag = ((MUXC.NavigationViewItem)NavView.SelectedItem).Tag.ToString();
            }
            else 
            {
                NavView.SelectedItem = NavView.MenuItems.Where(x => ((MUXC.NavigationViewItem)x).Tag.ToString() == lastNavTag).First();
            }

            if (!disabledPages.Any(str => str.Equals(lastNavTag, StringComparison.OrdinalIgnoreCase)))
            {
                NavView_Navigate(lastNavTag, null, e);
            }
            else
            {
                ContentDialog failedAppsDialog = new ContentDialog
                {

                    Title = $"{resourceLoader.GetString("FailedToLaunch")} {GetHeader()}",
                    Content = string.Format(CultureInfo.CurrentCulture, resourceLoader.GetString("EnableAndRetry"), GetHeader()),
                    CloseButtonText = resourceLoader.GetString("Ok")
                };

                NavView.SelectedItem = NavView.MenuItems[0];
                ((App)Application.Current).MainPageLastNavTag = lastNavTag = ((MUXC.NavigationViewItem)NavView.SelectedItem).Tag.ToString();
                NavView_Navigate(lastNavTag, null, e);


                ContentDialogResult result = await failedAppsDialog.ShowAsync();
            }

            base.OnNavigatedTo(e);
        }

        /// <summary>
        /// Call when boot tasks start. Disables pages not allowed during boot.
        /// </summary>
        private void BootTasksStart()
        {
            BootTaskWarning.Visibility = Visibility.Visible;
            var pagesToDisable = navViewPages.Where(x => (x.AllowedDuringBoot == false) && (x.Enabled == true)).ToArray();
            for (int i = 0; i < pagesToDisable.Length; i++)
            {
                HidePage(pagesToDisable[i]);
            }
        }

        /// <summary>
        /// Call when boot tasks complete. Enables all non-disabled pages.
        /// </summary>
        private void BootTasksDone()
        {
            BootTaskWarning.Visibility = Visibility.Collapsed;

            var pagesToEnable = navViewPages.Where(x => (x.AllowedDuringBoot == false) && (x.Enabled == false) && (!disabledPages.Contains(x.Tag))).ToArray();
            for (int i = 0; i < pagesToEnable.Length; i++)
            {
                ShowPage(pagesToEnable[i]);
            }
        }

        private void HidePage(string tag)
        {
            HidePage(navViewPages.Where(x => x.Tag == tag).First());
        }

        private void HidePage((string Tag, Type Page, bool Enabled, bool AllowedDuringBoot) pageMap)
        {
            var item = (MUXC.NavigationViewItem)NavView.MenuItems.First(x => ((MUXC.NavigationViewItem)x).Tag.ToString() == pageMap.Tag);
            item.Visibility = Visibility.Collapsed;
            item.IsEnabled = false;
            navViewPages.Remove(pageMap);
            pageMap.Enabled = false;
            navViewPages.Add(pageMap);
        }

        private void ShowPage(string tag)
        {
            ShowPage(navViewPages.Where(x => x.Tag == tag).First());
        }

        private void ShowPage((string Tag, Type Page, bool Enabled, bool AllowedDuringBoot) pageMap)
        {
            if (disabledPages.Contains(pageMap.Tag))
            {
                // Page is disabled, don't allow it to be shown
                return;
            }

            if ((pageMap.Tag == "files") && (Client.IsLocalHost) && (!((App)Application.Current).IsContainerRunning))
            {
                // Special case, only show files page if connected remotely or if a container is running.
                return;
            }

            MUXC.NavigationViewItem item = (MUXC.NavigationViewItem)NavView.MenuItems.Where(x => ((MUXC.NavigationViewItem)x).Tag.ToString() == pageMap.Tag).First();
            item.Visibility = Visibility.Visible;
            item.IsEnabled = true;
            navViewPages.Remove(pageMap);
            pageMap.Enabled = true;
            navViewPages.Add(pageMap);
        }

        private void On_ContentFrameNavigated(object sender, NavigationEventArgs e)
        {
            if (ContentFrame.SourcePageType != null)
            {
                var item = navViewPages.FirstOrDefault(p => (p.Page == e.SourcePageType));

                NavView.SelectedItem = NavView.MenuItems.OfType<MUXC.NavigationViewItem>().
                                                         First(n => n.Tag.Equals(item.Tag));

                NavView.Header = GetHeader();
            }
        }

        private string GetHeader()
        {
            if ("console".Equals((string)((MUXC.NavigationViewItem)NavView.SelectedItem).Tag, StringComparison.OrdinalIgnoreCase))
            {
                return resourceLoader.GetString("CommandPromptText/Text");
            }
            else
            {
               return ((MUXC.NavigationViewItem)NavView.SelectedItem)?.Content?.ToString();
            }
        }

        private void NavView_ItemInvoked(MUXC.NavigationView sender, MUXC.NavigationViewItemInvokedEventArgs args)
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
            var pageMap = navViewPages.FirstOrDefault(p => p.Tag.Equals(navItemTag, StringComparison.OrdinalIgnoreCase));

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
        private async void SystemFlyout_Opening(object sender, object e)
        {
            // Network info:
            await UpdateIpAddresses();
            await ipAddressSem.WaitAsync();
            try
            {
                NetworkStackPanel.Children.Clear();

                foreach (var ipAndNic in ipAddresses)
                {
                    NetworkStackPanel.Children.Add(new TextBlock()
                    {
                        Text = $"{ipAndNic.Item2} : {ipAndNic.Item1}",
                        IsTextSelectionEnabled = true
                    });
                }
            }
            finally
            {
                ipAddressSem.Release();
            }
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
            try
            {
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
            }
            finally
            {
                ipAddressSem.Release();
            }
        }

        private async Task UpdateIpAddresses()
        {
            await ipAddressSem.WaitAsync();
            try
            {
                ipAddresses = await Client.GetIpAddressesAndNicNames();
                if (((App)Application.Current).IsContainerRunning)
                {
                    try
                    {
                        var temp = await Client.GetContainerIpAddresses();
                        foreach (var ip in temp)
                        {
                            ipAddresses.Add(new Tuple<string, string>(ip, resourceLoader.GetString("ContainerIP")));
                        }
                    }
                    catch (FactoryOrchestratorContainerException)
                    {}
                }
            }
            finally
            {
                ipAddressSem.Release();
            }
        }

        private void SettingsFlyout_Opening(object sender, object e)
        {
            SettingsShowExternalTasks.IsChecked = Settings.ShowExternalTasks;
            SettingsTrackExecution.IsChecked = Settings.TrackExecution;
        }

        private void SettingsShowExternalTasks_Checked(object sender, RoutedEventArgs e)
        {
            Settings.ShowExternalTasks = (bool)SettingsShowExternalTasks.IsChecked;
        }

        private void SettingsTrackExecution_Checked(object sender, RoutedEventArgs e)
        {
            Settings.TrackExecution = (bool)SettingsTrackExecution.IsChecked;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    navUpdateSem?.Dispose();
                    ipAddressSem?.Dispose();
                    networkTimer?.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        private string lastNavTag;
        private readonly List<(string Tag, Type Page, bool Enabled, bool AllowedDuringBoot)> navViewPages = new List<(string Tag, Type Page, bool Enabled, bool AllowedDuringBoot)>
        {
            ("run", typeof(TaskListExecutionPage), true, true),
            ("console", typeof(ConsolePage), true, false),
            ("apps", typeof(AppsPage), true, false),
            ("save", typeof(SaveLoadEditPage), true, false),
            ("files", typeof(FileTransferPage), true, false),
            ("wdp", typeof(WdpPage), true, true),
            ("about", typeof(AboutPage), true, true)
        };
        private List<string> disabledPages;
        private readonly SemaphoreSlim navUpdateSem;

        private FactoryOrchestratorUWPClient Client;
        private readonly System.Timers.Timer networkTimer;
        private int networkTimerIndex;
        private List<Tuple<string, string>> ipAddresses;
        private readonly SemaphoreSlim ipAddressSem;
        private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();

    }

}
