// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.FactoryOrchestrator.Client;
using Microsoft.FactoryOrchestrator.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
            _resetSem = new SemaphoreSlim(1, 1);
            TaskListCollection = new ObservableCollection<TaskListSummary>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            mainPage = (Frame)e.Parameter;
            if (_taskListGuidPoller == null)
            {
                _taskListGuidPoller = new ServerPoller(null, typeof(TaskList), 2000);
                _taskListGuidPoller.OnUpdatedObject += OnUpdatedTaskListGuidsAsync;
                _taskListGuidPoller.OnException += ((App)Application.Current).OnServerPollerException;
            }

            _taskListGuidPoller.StartPolling(Client);
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
                        var lists = await Client.LoadTaskListsFromXmlFile(path);
                    }
                    else
                    {
                        var list = await Client.CreateTaskListFromDirectory(path, true);
                    }

                }
                catch (Exception ex)
                {
                    if (ex.GetType() != typeof(FactoryOrchestratorConnectionException))
                    {
                        ShowLoadFailure(_isFileLoad, path, ex.Message);
                    }
                }

                LoadProgressBar.Visibility = Visibility.Collapsed;
                LoadFlyout.Hide();
            }
        }

        private async void ShowLoadFailure(bool isFileLoad, string path, string error)
        {
            ContentDialog failedLoadDialog = new ContentDialog
            {
                Title = "Failed to load " + (isFileLoad ? "TaskLists XML file" : "folder"),
                Content = path + Environment.NewLine + Environment.NewLine,
                CloseButtonText = "Ok"
            };

            if (error != null)
            {
                failedLoadDialog.Content += error;
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
            await Client.DeleteTaskList(guid);
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
            try
            {
                var list = await Client.QueryTaskList(guid);
                mainPage.Navigate(typeof(EditPage), list);
                this.OnNavigatedFrom(null);
            }
            catch (FactoryOrchestratorUnkownGuidException ex)
            {
                ContentDialog failedQueryDialog = new ContentDialog
                {
                    Title = "Failed to query TaskList for edit",
                    Content = $"It may have been deleted." + Environment.NewLine + Environment.NewLine + ex.Message,
                    CloseButtonText = "Ok"
                };

                ContentDialogResult result = await failedQueryDialog.ShowAsync();
            }
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

                try
                {
                    if (SaveAllButton.Flyout.IsOpen)
                    {
                        await Client.SaveAllTaskListsToXmlFile(savePath);
                    }
                    else
                    {
                        await Client.SaveTaskListToXmlFile(_activeGuid, savePath);
                    }
                }
                catch (Exception ex)
                {
                    if (ex.GetType() != typeof(FactoryOrchestratorConnectionException))
                    {
                        ContentDialog failedSaveDialog = new ContentDialog
                        {
                            Title = "Failed to save TaskLists XML file",
                            Content = $"{ex.Message}",
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
                "Manually exported FactoryOrchestratorXML files will not be deleted, but will need to be manually imported via \"Load FactoryOrchestratorXML file\".\n\n" +
                "If \"Factory Reset\" is chosen, the service is restarted as if it is first boot. First boot and every boot tasks will re-run. Initial TaskLists will be loaded.\n" +
                "\"Factory Reset\" WILL interrupt communication with clients, including this app, until the boot tasks complete.",
                CloseButtonText = "Cancel",
                PrimaryButtonText = "Delete All",
                SecondaryButtonText = "Factory Reset"
            };

            ContentDialogResult result = await clearAllDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                await Task.Run(async () =>
                {
                    _resetSem.Wait();
                    try
                    {
                        await Client.ResetService(true);
                    }
                    finally
                    {
                        _resetSem.Release();
                    }
                });
            }
            else if (result == ContentDialogResult.Secondary)
            {
                await Task.Run(async () =>
                {
                    _resetSem.Wait();
                    try
                    {
                        await Client.ResetService(true, true);
                    }
                    finally
                    {
                        _resetSem.Release();
                    }
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
        /// Called when the TaskLists are reordered by drag and drop. Sends updated order to the Service.
        /// </summary>
        private async void TaskListsView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            try
            {
                if (!TaskListCollection.Any(x => x.IsRunningOrPending))
                {
                    var newOrder = new List<Guid>();
                    newOrder.AddRange(TaskListCollection.Select(x => x.Guid));
                    await Client.ReorderTaskLists(newOrder);
                }
                else
                {
                    throw new FactoryOrchestratorException("Cannot reorder TaskLists while a TaskList is running!");
                }
            }
            catch (FactoryOrchestratorException ex)
            {
                // If it fails, the poller will update the UI to the old order.
                ContentDialog failedSaveDialog = new ContentDialog
                {
                    Title = "Failed to reorder TaskLists",
                    Content = $"{ex.Message}",
                    CloseButtonText = "Ok"
                };

                ContentDialogResult result = await failedSaveDialog.ShowAsync();

            }
        }

        /// <summary>
        /// Keeps the Known TaskLists in sync with the server.
        /// </summary>
        private async void OnUpdatedTaskListGuidsAsync(object source, ServerPollerEventArgs e)
        {
            var taskListSummaries = e.Result as List<TaskListSummary>;

            if (taskListSummaries != null)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    // Add or update TaskLists
                    for (int i = 0; i < taskListSummaries.Count; i++)
                    {
                        try
                        {
                            if (i == TaskListCollection.Count)
                            {
                                TaskListCollection.Insert(i, taskListSummaries[i]);
                            }
                            else if (!TaskListCollection[i].Equals(taskListSummaries[i]))
                            {
                                TaskListCollection[i] = taskListSummaries[i];
                            }
                        }
                        catch (ArgumentOutOfRangeException ex)
                        {
                            Debug.WriteLine(ex.AllExceptionsToString());
                        }
                    }

                    // Prune existing list
                    int j = taskListSummaries.Count;
                    while (TaskListCollection.Count > taskListSummaries.Count)
                    {
                        TaskListCollection.RemoveAt(j);
                    }
                });
            }
        }

        /// <summary>
        /// Given a button associated with a tasklist, returns the tasklist GUID.
        /// </summary>
        private Guid GetTaskListGuidFromButton(Button button)
        {
            var stack = button.Parent as StackPanel;
            var grid = stack.Parent as Grid;
            return ((TaskListSummary)((ContentPresenter)(grid.Children.Where(x => x.GetType() == typeof(ContentPresenter)).First())).Content).Guid;
        }

        public ObservableCollection<TaskListSummary> TaskListCollection;
        private ServerPoller _taskListGuidPoller;
        private SemaphoreSlim _resetSem;
        private Guid _activeGuid;
        private bool _isFileLoad;
        private Frame mainPage;
        private FactoryOrchestratorUWPClient Client = ((App)Application.Current).Client;
    }
}
