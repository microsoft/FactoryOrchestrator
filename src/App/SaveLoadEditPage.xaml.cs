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
using Windows.ApplicationModel.Resources;
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
    public sealed partial class SaveLoadEditPage : Page, IDisposable
    {
        public SaveLoadEditPage()
        {
            this.InitializeComponent();
            _resetSem = new SemaphoreSlim(1, 1);
            TaskListCollection = new ObservableCollection<TaskListSummary>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e != null)
            {
                mainPage = (Frame)e.Parameter;
            }
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
            mainPage?.Navigate(typeof(EditPage), null);
            this.OnNavigatedFrom(null);
        }

        private void LoadFolderButton_Click(object sender, RoutedEventArgs e)
        {
            _isFileLoad = false;
            var button = sender as Button;
            LoadFlyoutTextHeader.Text = resourceLoader.GetString("LoadFolderFlyoutText");
            Flyout.ShowAttachedFlyout(button);
        }   

        private void LoadFileButton_Click(object sender, RoutedEventArgs e)
        {
            _isFileLoad = true;
            LoadFlyoutTextHeader.Text = resourceLoader.GetString("LoadXMLFlyoutText");
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

        // cancel delete flyout action
        private void CancelDelete_Click(object sender, RoutedEventArgs e)
        {
            DeleteFlyout.Hide();
        }

        /// <summary>
        /// Confirm delete flyout
        /// </summary>
        private async void ConfirmDelete_Click(object sender, RoutedEventArgs e)
        {
                try
                {
                    await Client.DeleteTaskList(_activeGuid);
                }
                catch (Exception ex)
                {
                    if (ex.GetType() != typeof(FactoryOrchestratorConnectionException))
                    {
                        ContentDialog failedDeleteDialog = new ContentDialog
                        {
                            Title = resourceLoader.GetString("FOXMLSaveFailed"),
                            Content = $"{ex.Message}",
                            CloseButtonText = resourceLoader.GetString("Ok")
                        };

                        _ = await failedDeleteDialog.ShowAsync();
                    }
                }
                DeleteFlyout.Hide();
        }

        //Function monitors text box changes to enable/disable the cancel and save buttons for save feature.
        private void SaveFlyout_TextChanged(Object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(SaveFlyoutUserPath.Text))
            {
                ConfirmSave.IsEnabled = true;
                CancelSave.IsEnabled = true;
            }
            else
            {
                ConfirmSave.IsEnabled = false;
                CancelSave.IsEnabled = false;
            }
        }

        //Function monitors text box changes to enable/disable the cancel and save buttons for load feature.
        private void LoadFlyout_TextChanged(Object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(LoadFlyoutUserPath.Text))
            {
                ConfirmLoad.IsEnabled = true;
                CancelLoad.IsEnabled = true;
            }
            else
            {
                ConfirmLoad.IsEnabled = false;
                CancelLoad.IsEnabled = false;
            }
        }

        private async void ShowLoadFailure(bool isFileLoad, string path, string error)
        {
            var type = isFileLoad ? resourceLoader.GetString("FOXML") : resourceLoader.GetString("Folder");
            ContentDialog failedLoadDialog = new ContentDialog
            {
                Title = $"{resourceLoader.GetString("LoadFailed")} {type}",
                Content = path + Environment.NewLine + Environment.NewLine,
                CloseButtonText = resourceLoader.GetString("Ok")
            };

            if (error != null)
            {
                failedLoadDialog.Content += error;
            }
            else
            {
                failedLoadDialog.Content += resourceLoader.GetString("CheckPath");
            }

            _ = await failedLoadDialog.ShowAsync();
        }

        /// <summary>
        /// Delete the selected tasklist
        /// </summary>
        private void DeleteListButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            _activeGuid = GetTaskListGuidFromButton(button);
            Flyout.ShowAttachedFlyout(button);
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
            var list = await Client.QueryTaskList(guid);
            if (list != null)
            {
                mainPage?.Navigate(typeof(EditPage), list);
                this.OnNavigatedFrom(null);
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
                var savePath = SaveFlyoutUserPath.Text;
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
                            Title = resourceLoader.GetString("FOXMLSaveFailed"),
                            Content = $"{ex.Message}",
                            CloseButtonText = resourceLoader.GetString("Ok")
                        };

                        _ = await failedSaveDialog.ShowAsync();
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
                Title = $"{resourceLoader.GetString("DeleteAll/Text")}?",
                Content = resourceLoader.GetString("DeleteAllContent"),
                CloseButtonText = resourceLoader.GetString("Cancel"),
                PrimaryButtonText = resourceLoader.GetString("DeleteAll/Text"),
                SecondaryButtonText = resourceLoader.GetString("FactoryReset")
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
                var newOrder = new List<Guid>();
                newOrder.AddRange(TaskListCollection.Select(x => x.Guid));
                await Client.ReorderTaskLists(newOrder);
            }
            catch (FactoryOrchestratorException ex)
            {
                // If it fails, the poller will update the UI to the old order.
                ContentDialog failedSaveDialog = new ContentDialog
                {
                    Title = resourceLoader.GetString("ReorderFailed"),
                    Content = ex.Message,
                    CloseButtonText = resourceLoader.GetString("Ok")
                };

                ContentDialogResult result = await failedSaveDialog.ShowAsync();

            }
        }

        /// <summary>
        /// Keeps the Known TaskLists in sync with the server.
        /// </summary>
        private async void OnUpdatedTaskListGuidsAsync(object source, ServerPollerEventArgs e)
        {
            if (e.Result is List<TaskListSummary> taskListSummaries)
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
        private static Guid GetTaskListGuidFromButton(Button button)
        {
            var stack = button.Parent as StackPanel;
            var grid = stack.Parent as Grid;
            return ((TaskListSummary)((ContentPresenter)(grid.Children.Where(x => x.GetType() == typeof(ContentPresenter)).First())).Content).Guid;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _resetSem?.Dispose();
                    _taskListGuidPoller?.Dispose();
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

        public ObservableCollection<TaskListSummary> TaskListCollection { get; private set; }
        private ServerPoller _taskListGuidPoller;
        private readonly SemaphoreSlim _resetSem;
        private Guid _activeGuid;
        private bool _isFileLoad;
        private Frame mainPage;
        private readonly FactoryOrchestratorUWPClient Client = ((App)Application.Current).Client;
        private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();
    }
}
