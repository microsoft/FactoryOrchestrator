using Microsoft.FactoryTestFramework.Client;
using Microsoft.FactoryTestFramework.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
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

namespace Microsoft.FactoryTestFramework.UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SaveLoadEditPage : Page
    {
        public SaveLoadEditPage()
        {
            this.InitializeComponent();
            TestViewModel = new TestViewModel();
            _listUpdateSem = new SemaphoreSlim(1, 1);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            mainPage = (Frame)e.Parameter;

            if (_testListGuidPoller == null)
            {
                _testListGuidPoller = new FTFPoller(null, typeof(TestList), IPCClientHelper.IpcClient, 2000);
                _testListGuidPoller.OnUpdatedObject += OnUpdatedTestListGuidsAsync;
            }

            _testListGuidPoller.StartPolling();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _testListGuidPoller.StopPolling();
        }

        private void NewListButton_Click(object sender, RoutedEventArgs e)
        {
            mainPage.Navigate(typeof(EditPage), null);
            this.OnNavigatedFrom(null);
        }

        private void LoadFolderButton_Click(object sender, RoutedEventArgs e)
        {
            _isFileLoad = false;
            var button = sender as Button;
            LoadFlyoutTextHeader.Text = "Folder to load as TestList:";
            Flyout.ShowAttachedFlyout(button);
        }   

        private void LoadFileButton_Click(object sender, RoutedEventArgs e)
        {
            _isFileLoad = true;
            LoadFlyoutTextHeader.Text = "TestLists XML file to load from:";
            var button = sender as Button;
            Flyout.ShowAttachedFlyout(button);
        }

        /// <summary>
        /// Cancel load flyout
        /// </summary>
        private void CancelLoad_Click(object sender, RoutedEventArgs e)
        {
            LoadFlyout.Hide();
        }


        private void ConfirmLoad_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                ConfirmLoad_Click(sender, null);
            }
        }

        /// <summary>
        /// Confirm load flyout
        /// </summary>
        private async void ConfirmLoad_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(LoadFlyoutUserPath.Text))
            {
                var path = String.Copy(LoadFlyoutUserPath.Text);
                LoadProgressBar.Width = LoadFlyoutUserPath.ActualWidth - CancelLoad.ActualWidth - ConfirmLoad.ActualWidth - 30; // 30 == combined margin size
                LoadProgressBar.Visibility = Visibility.Visible;
                try
                {
                    if (_isFileLoad)
                    {
                        var lists = await IPCClientHelper.IpcClient.InvokeAsync(x => x.LoadTestListsFromXmlFile(path));
                        if (lists == null)
                        {
                            var error = await IPCClientHelper.IpcClient.InvokeAsync(x => x.GetLastServiceError());
                            ShowLoadFailure(_isFileLoad, path, error);
                        }
                    }
                    else
                    {
                        var list = await IPCClientHelper.IpcClient.InvokeAsync(x => x.CreateTestListFromDirectory(path, false));
                        if (list == null)
                        {
                            var error = await IPCClientHelper.IpcClient.InvokeAsync(x => x.GetLastServiceError());
                            ShowLoadFailure(_isFileLoad, path, error);
                        }
                    }

                }
                catch (Exception ex)
                {
                    ShowLoadFailure(_isFileLoad, path, new ServiceEvent(ServiceEventType.ServiceError, null, "Check that FTFService is running on DUT" + Environment.NewLine + ex.Message));
                }

                LoadProgressBar.Visibility = Visibility.Collapsed;
                LoadFlyout.Hide();
            }
        }

        ///
        private async void ShowLoadFailure(bool isFileLoad, string path, ServiceEvent lastError = null)
        {
            ContentDialog failedLoadDialog = new ContentDialog
            {
                Title = "Failed to load " + (isFileLoad ? "TestLists XML file" : "folder"),
                Content = path + Environment.NewLine + Environment.NewLine,
                CloseButtonText = "Ok"
            };

            if (lastError != null)
            {
                failedLoadDialog.Content += lastError.Message;
            }
            else
            {
                failedLoadDialog.Content += "Check the path and try again.";
            }

            ContentDialogResult result = await failedLoadDialog.ShowAsync();
        }

        /// <summary>
        /// Delete the selected testlist
        /// </summary>
        private async void DeleteListButton_Click(object sender, RoutedEventArgs e)
        {
            var guid = GetTestListGuidFromButton(sender as Button);
            await IPCClientHelper.IpcClient.InvokeAsync(x => x.DeleteTestList(guid));
        }

        /// <summary>
        /// Save the selected testlist
        /// </summary>
        private void SaveListButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            _activeGuid = GetTestListGuidFromButton(button);
            SaveFlyoutUserPath.Text = _activeGuid.ToString();
            Flyout.ShowAttachedFlyout(button);
        }

        /// <summary>
        /// Edit the selected testlist
        /// </summary>
        private async void EditListButton_Click(object sender, RoutedEventArgs e)
        {
            var guid = GetTestListGuidFromButton(sender as Button);
            var list = await IPCClientHelper.IpcClient.InvokeAsync(x => x.QueryTestList(guid));
            mainPage.Navigate(typeof(EditPage), list);
            this.OnNavigatedFrom(null);
        }

        /// <summary>
        /// Cancel save flyout
        /// </summary>
        private void CancelSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFlyout.Hide();
        }

        private void ConfirmSave_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                ConfirmSave_Click(sender, null);
            }
        }

        /// <summary>
        /// Confirm save flyout
        /// </summary>
        private async void ConfirmSave_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(SaveFlyoutUserPath.Text))
            {
                var savePath = SaveFlyoutUserPath.Text + ".testlists";
                SaveProgressBar.Width = SaveFlyoutUserPath.ActualWidth - CancelSave.ActualWidth - ConfirmSave.ActualWidth - 30; // 30 == combined margin size
                SaveProgressBar.Visibility = Visibility.Visible;
                bool saved = true;

                try
                {
                    if (SaveAllButton.Flyout.IsOpen)
                    {
                        saved = await IPCClientHelper.IpcClient.InvokeAsync(x => x.SaveAllTestListsToXmlFile(savePath));
                    }
                    else
                    {
                        saved = await IPCClientHelper.IpcClient.InvokeAsync(x => x.SaveTestListToXmlFile(_activeGuid, savePath));
                    }
                }
                catch (Exception)
                {}
                finally
                {
                    ContentDialog failedSaveDialog = new ContentDialog
                    {
                        Title = "Failed to save TestLists XML file",
                        Content = saved ? (savePath + Environment.NewLine + Environment.NewLine + "Check the path and try again.") : "No test list(s) found!",
                        CloseButtonText = "Ok"
                    };

                    ContentDialogResult result = await failedSaveDialog.ShowAsync();
                }

                SaveProgressBar.Visibility = Visibility.Collapsed;
                SaveFlyout.Hide();
            }
        }

        /// <summary>
        /// Clear all known testlists
        /// </summary>
        private async void DeleteAllButton_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog clearAllDialog = new ContentDialog
            {
                Title = "Clear all TestLists?",
                Content = "All running TestLists will be stopped. All TestLists will be removed from the server.\n" +
                "NOTE: If TestLists are configured to load at service start, they will be reloaded on reboot.",
                CloseButtonText = "Cancel",
                PrimaryButtonText = "Clear All"
            };

            ContentDialogResult result = await clearAllDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                await Task.Run(async () =>
                {
                    _listUpdateSem.Wait();
                    foreach (var guid in TestViewModel.TestData.TestListGuids)
                    {
                        await IPCClientHelper.IpcClient.InvokeAsync(x => x.AbortTestList(guid));
                        await IPCClientHelper.IpcClient.InvokeAsync(x => x.DeleteTestList(guid));
                    }
                    _listUpdateSem.Release();
                });
            }
        }

        private void SaveFlyout_Closing(FlyoutBase sender, FlyoutBaseClosingEventArgs args)
        {
            SaveFlyoutUserPath.Text = "";
        }

        private void LoadFlyout_Closing(FlyoutBase sender, FlyoutBaseClosingEventArgs args)
        {
            LoadFlyoutUserPath.Text = "";
        }

        /// <summary>
        /// Keeps the Known TestLists in sync with the server.
        /// </summary>
        private async void OnUpdatedTestListGuidsAsync(object source, FTFPollEventArgs e)
        {
            var testListGuids = e.Result as List<Guid>;

            if (testListGuids != null)
            {
                foreach (var guid in testListGuids)
                {
                    if (!TestViewModel.TestData.TestListGuids.Contains(guid))
                    {
                        _listUpdateSem.Wait();
                        if (!TestViewModel.TestData.TestListGuids.Contains(guid))
                        {
                            var list = await IPCClientHelper.IpcClient.InvokeAsync(x => x.QueryTestList(guid));

                            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                            {
                                TestViewModel.AddOrUpdateTestList(list);
                                TestListsView.ItemsSource = TestViewModel.TestData.TestListGuids;
                                if (TestListsView.SelectedItem == null)
                                {
                                    TestViewModel.TestData.SelectedTestListGuid = list.Guid;
                                    TestListsView.SelectedItem = list.Guid;
                                }
                            });
                        }

                        _listUpdateSem.Release();
                    }
                }

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    if (TestViewModel.PruneKnownTestLists(testListGuids))
                    {
                        TestListsView.ItemsSource = TestViewModel.TestData.TestListGuids;
                    }
                });

            }
        }

        /// <summary>
        /// Given a button associated with a testlist, returns the testlist GUID.
        /// </summary>
        private Guid GetTestListGuidFromButton(Button button)
        {
            var stack = button.Parent as StackPanel;
            var grid = stack.Parent as Grid;
            return (Guid)(((ContentPresenter)(grid.Children[0])).Content);
        }

        public TestViewModel TestViewModel { get; set; }

        private FTFPoller _testListGuidPoller;
        private SemaphoreSlim _listUpdateSem;
        private Guid _activeGuid;
        private bool _isFileLoad;
        private Frame mainPage;
    }
}
