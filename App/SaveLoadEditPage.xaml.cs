using Microsoft.FactoryOrchestrator.Client;
using Microsoft.FactoryOrchestrator.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using TaskStatus = Microsoft.FactoryOrchestrator.Core.TaskStatus;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Microsoft.FactoryOrchestrator.UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SaveLoadEditPage : Page
    {
        public SaveLoadEditPage()
        {
            this.InitializeComponent();
            _listUpdateSem = new SemaphoreSlim(1, 1);
            TaskListCollection = new ObservableCollection<TaskList>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            mainPage = (Frame)e.Parameter;
            if (_taskListGuidPoller == null)
            {
                _taskListGuidPoller = new ServerPoller(null, typeof(TaskList), IPCClientHelper.IpcClient, 2000);
                _taskListGuidPoller.OnUpdatedObject += OnUpdatedTaskListGuidsAsync;
            }

            _taskListGuidPoller.StartPolling();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _taskListGuidPoller.StopPolling();
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
            LoadFlyoutTextHeader.Text = "Folder to load as TaskList:";
            Flyout.ShowAttachedFlyout(button);
        }   

        private void LoadFileButton_Click(object sender, RoutedEventArgs e)
        {
            _isFileLoad = true;
            LoadFlyoutTextHeader.Text = "TaskLists XML file to load from:";
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
                        var lists = await IPCClientHelper.IpcClient.InvokeAsync(x => x.LoadTaskListsFromXmlFile(path));
                        if (lists == null)
                        {
                            var error = await IPCClientHelper.IpcClient.InvokeAsync(x => x.GetLastServiceError());
                            ShowLoadFailure(_isFileLoad, path, error);
                        }
                    }
                    else
                    {
                        var list = await IPCClientHelper.IpcClient.InvokeAsync(x => x.CreateTaskListFromDirectory(path, false));
                        if (list == null)
                        {
                            var error = await IPCClientHelper.IpcClient.InvokeAsync(x => x.GetLastServiceError());
                            ShowLoadFailure(_isFileLoad, path, error);
                        }
                    }

                }
                catch (Exception ex)
                {
                    ShowLoadFailure(_isFileLoad, path, new ServiceEvent(ServiceEventType.ServiceError, null, "Check that FactoryOrchestratorService is running on DUT" + Environment.NewLine + ex.Message));
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
                Title = "Failed to load " + (isFileLoad ? "TaskLists XML file" : "folder"),
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
        /// Delete the selected tasklist
        /// </summary>
        private async void DeleteListButton_Click(object sender, RoutedEventArgs e)
        {
            var guid = GetTaskListGuidFromButton(sender as Button);
            await IPCClientHelper.IpcClient.InvokeAsync(x => x.DeleteTaskList(guid));
        }

        /// <summary>
        /// Save the selected tasklist
        /// </summary>
        private void SaveListButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            _activeGuid = GetTaskListGuidFromButton(button);
            SaveFlyoutUserPath.Text = _activeGuid.ToString();
            Flyout.ShowAttachedFlyout(button);
        }

        /// <summary>
        /// Edit the selected tasklist
        /// </summary>
        private async void EditListButton_Click(object sender, RoutedEventArgs e)
        {
            var guid = GetTaskListGuidFromButton(sender as Button);
            var list = await IPCClientHelper.IpcClient.InvokeAsync(x => x.QueryTaskList(guid));
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
                var savePath = SaveFlyoutUserPath.Text + ".xml";
                SaveProgressBar.Width = SaveFlyoutUserPath.ActualWidth - CancelSave.ActualWidth - ConfirmSave.ActualWidth - 30; // 30 == combined margin size
                SaveProgressBar.Visibility = Visibility.Visible;
                bool saved = false;

                try
                {
                    if (SaveAllButton.Flyout.IsOpen)
                    {
                        saved = await IPCClientHelper.IpcClient.InvokeAsync(x => x.SaveAllTaskListsToXmlFile(savePath));
                    }
                    else
                    {
                        saved = await IPCClientHelper.IpcClient.InvokeAsync(x => x.SaveTaskListToXmlFile(_activeGuid, savePath));
                    }
                }
                catch (Exception)
                {}
                finally
                {
                    if (!saved)
                    {
                        ContentDialog failedSaveDialog = new ContentDialog
                        {
                            Title = "Failed to save TaskLists XML file",
                            Content = saved ? (savePath + Environment.NewLine + Environment.NewLine + "Check the path and try again.") : "No task list(s) found!",
                            CloseButtonText = "Ok"
                        };

                        ContentDialogResult result = await failedSaveDialog.ShowAsync();
                    }
                }

                SaveProgressBar.Visibility = Visibility.Collapsed;
                SaveFlyout.Hide();
            }
        }

        /// <summary>
        /// Clear all known tasklists
        /// </summary>
        private async void DeleteAllButton_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog clearAllDialog = new ContentDialog
            {
                Title = "Delete all TaskLists?",
                Content = "All running TaskLists will be stopped. All TaskLists will be removed from the server permanently.\n" +
                "Manually exported FactoryOrchestratorXML files will not be deleted, but will need to be manually imported via \"Load FactoryOrchestratorXML file\".",
                CloseButtonText = "Cancel",
                PrimaryButtonText = "Clear All"
            };

            ContentDialogResult result = await clearAllDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                await Task.Run(async () =>
                {
                    _listUpdateSem.Wait();
                    await IPCClientHelper.IpcClient.InvokeAsync(x => x.ResetService(true));
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
        /// Keeps the Known TaskLists in sync with the server.
        /// </summary>
        private async void OnUpdatedTaskListGuidsAsync(object source, ServerPollerEventArgs e)
        {
            var taskListGuids = (e.Result as List<(Guid guid, TaskStatus status)>).Select(x => x.guid);

            if (taskListGuids != null)
            {
                // Add or update TaskLists
                foreach (var guid in taskListGuids)
                {
                    var list = TaskListCollection.Where(x => x.Guid == guid).DefaultIfEmpty(null).FirstOrDefault();

                    if (list == null)
                    {
                        var newList = await IPCClientHelper.IpcClient.InvokeAsync(x => x.QueryTaskList(guid));
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            TaskListCollection.Add(newList);
                        });
                    }
                }

                // Prune non-existant lists
                for (int i = 0; i < TaskListCollection.Count; i++)
                {
                    var item = TaskListCollection[i];
                    if (!taskListGuids.Contains(item.Guid))
                    {
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            TaskListCollection.RemoveAt(i);
                        });
                        i--;
                    }
                }
            }

        }

        /// <summary>
        /// Given a button associated with a tasklist, returns the tasklist GUID.
        /// </summary>
        private Guid GetTaskListGuidFromButton(Button button)
        {
            var stack = button.Parent as StackPanel;
            var grid = stack.Parent as Grid;
            return ((TaskList)((ContentPresenter)(grid.Children[0])).Content).Guid;
        }

        public ObservableCollection<TaskList> TaskListCollection;
        private ServerPoller _taskListGuidPoller;
        private SemaphoreSlim _listUpdateSem;
        private Guid _activeGuid;
        private bool _isFileLoad;
        private Frame mainPage;
    }
}
