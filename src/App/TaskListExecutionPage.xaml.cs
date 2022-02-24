// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using Microsoft.FactoryOrchestrator.Client;
using Microsoft.FactoryOrchestrator.Core;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaskStatus = Microsoft.FactoryOrchestrator.Core.TaskStatus;
using Windows.UI.Core;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Automation;

namespace Microsoft.FactoryOrchestrator.UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TaskListExecutionPage : Page, IDisposable
    {
        public TaskListExecutionPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
            _selectedTaskList = -1;
            _selectedTaskListGuid = Guid.Empty;
            _selectedTask = -1;
            _selectedTaskGuid = Guid.Empty;
            _headerUpdateLock = new object();
            mainPage = null;
            TaskListCollection = new ObservableCollection<TaskListSummary>();
            ActiveListCollection = new ObservableCollection<TaskBaseWithTemplate>();
            ResultsPageEmbedded.IsEmbedded = true;
            ((App)Application.Current).OnServiceDoneExecutingBootTasks += TaskListExecutionPage_OnServiceDoneExecutingBootTasks;
            ((App)Application.Current).OnServiceStart += TaskListExecutionPage_OnServiceStart;
            Settings.PropertyChanged += Settings_PropertyChanged;
        }

        private void TaskListsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TaskListsView.SelectedItem != null)
            {
                var selectedTaskListGuid = ((TaskListSummary)TaskListsView.SelectedItem).Guid;

                // Selection changed might have been due to updating a template, compare to _selectedTaskList
                if (_selectedTaskList != TaskListsView.SelectedIndex)
                {
                    // New list selected, start over
                    ActiveListCollection.Clear();
                    _selectedTaskList = TaskListsView.SelectedIndex;
                    _selectedTaskListGuid = TaskListCollection[_selectedTaskList].Guid;
                    // Show loading ring
                    LoadingTasksRing.IsActive = true;

                    if (Settings.TrackExecution)
                    {
                        EnsureSelectedIndexVisible(TaskListsView, TaskListsScrollView);
                    }
                }

                // Keep indicies in sync
                TaskListsResultsAndButtonsView.SelectedIndex = _selectedTaskList;

                var item = TaskListsResultsAndButtonsView.ContainerFromIndex(TaskListsResultsAndButtonsView.SelectedIndex) as FrameworkElement;
                string status = ((TaskListSummary)TaskListsResultsAndButtonsView.SelectedItem).Status.ToString();
                AutomationProperties.SetName(item, status);

                // Create new poller
                if (_activeListPoller != null)
                {
                    _activeListPoller.StopPolling();
                }
                _activeListPoller = new ServerPoller(selectedTaskListGuid, typeof(TaskList), 1000);
                _activeListPoller.OnUpdatedObject += OnUpdatedTaskListAsync;
                _activeListPoller.OnException += ((App)Application.Current).OnServerPollerException;
                _activeListPoller.StartPolling(Client);

                // Show Tests
                ActiveTestsView.Visibility = Visibility.Visible;
            }
            else
            {
                // Stop polling, hide tasks
                if (_activeListPoller != null)
                {
                    _activeListPoller.StopPolling();
                    _activeListPoller = null;
                }
                ActiveTestsView.Visibility = Visibility.Collapsed;
            }
        }

        //Setting default Tasklist item on startup to trigger accessiblity name update
        private void TaskListResultButtonView_Loaded(object sender, RoutedEventArgs e)
        {
            if(_selectedTaskList == -1)
            {
                _selectedTaskList = 0;
                TaskListsResultsAndButtonsView.SelectedIndex = _selectedTaskList;
            }
        }

        private void TaskListsResultsAndButtonsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((TaskListsResultsAndButtonsView.SelectedIndex != -1) && (_selectedTaskList != TaskListsResultsAndButtonsView.SelectedIndex))
            {
                // Select the tasklist to trigger TaskListsView_SelectionChanged
                TaskListsView.SelectedIndex = TaskListsResultsAndButtonsView.SelectedIndex;
                var item = TaskListsResultsAndButtonsView.ContainerFromIndex(TaskListsResultsAndButtonsView.SelectedIndex) as FrameworkElement;
                string status = ((TaskListSummary)TaskListsResultsAndButtonsView.SelectedItem).Status.ToString();
                AutomationProperties.SetName(item, status);
            }
        }

        private void ActiveTestsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((ActiveTestsView.SelectedIndex != -1) && (_selectedTask != ActiveTestsView.SelectedIndex))
            {
                // Select the tasklist to trigger ActiveTestsResultsView_SelectionChanged
                _selectedTask = ActiveTestsView.SelectedIndex;
                ActiveTestsResultsView.SelectedIndex = ActiveTestsView.SelectedIndex;
            }
        }

        private void ActiveTestsResultsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((ActiveTestsResultsView.SelectedIndex != -1) && (_selectedTask != ActiveTestsResultsView.SelectedIndex))
            {
                // Select the tasklist to trigger ACtiveTestView_SelectionChanged
                _selectedTask = ActiveTestsResultsView.SelectedIndex;
                ActiveTestsView.SelectedIndex = ActiveTestsResultsView.SelectedIndex;
            }
            var item = ActiveTestsResultsView.ContainerFromIndex(ActiveTestsResultsView.SelectedIndex) as FrameworkElement;
            string status = ((TaskBaseWithTemplate)ActiveTestsResultsView.SelectedItem).ToString();
            AutomationProperties.SetName(item, status);
        }

        private void ActiveTestsResultsView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem != null)
            {
                var taskWithTemplate = (TaskBaseWithTemplate)(e.ClickedItem);
                // Navigate from the MainPage frame so this is a "full screen" page
                mainPage?.Navigate(typeof(ResultsPage), taskWithTemplate.Task);
                this.OnNavigatedFrom(null);
            }
        }

        private async void OnUpdatedTaskListAsync(object source, ServerPollerEventArgs e)
        {
            if (e.Result != null)
            {
                TaskList list = (TaskList)e.Result;
                var taskArray = list.Tasks.ToArray();

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    try
                    {
                        LoadingTasksRing.IsActive = false;

                        if (taskArray.Length == 0)
                        {
                            // No Tasks exist, clear everything
                            ActiveListCollection.Clear();
                            return;
                        }

                        for (int i = 0; i < taskArray.Length; i++)
                        {
                            // Determine the template to use. Do here since it depends on the status of the list, not only the Task
                            var newTask = taskArray[i];
                            var newTemplate = (((list.TaskListStatus != TaskStatus.Running) &&
                                                    (list.TaskListStatus != TaskStatus.RunPending)) &&
                                                    (newTask.LatestTaskRunPassed != null) &&
                                                    (newTask.LatestTaskRunPassed == false)) ? TaskViewTemplate.WithRetryButton : TaskViewTemplate.Normal;

                            // Update the ActiveListCollection
                            try
                            {
                                if (i == ActiveListCollection.Count)
                                {
                                    ActiveListCollection.Insert(i, new TaskBaseWithTemplate(newTask, newTemplate));
                                }
                                else if (!ActiveListCollection[i].Task.Equals(newTask))
                                {
                                    // force template reselection
                                    ActiveListCollection[i] = new TaskBaseWithTemplate(newTask, newTemplate);
                                }
                            }
                            catch (ArgumentOutOfRangeException ex)
                            {
                                Debug.WriteLine(ex.AllExceptionsToString());
                            }
                        }

                        if (Settings.TrackExecution)
                        {
                            // Show mini window with latest output
                            var latestTask = taskArray.Where(x => x.LatestTaskRunStatus == TaskStatus.Running).DefaultIfEmpty(null).LastOrDefault();
                            if (latestTask == null)
                            {
                                latestTask = taskArray.Where(x => x.LatestTaskRunStatus == TaskStatus.RunPending).DefaultIfEmpty(null).FirstOrDefault();
                            }

                            if (latestTask != null)
                            {
                                // Select the running task
                                await TrackTaskExecution(latestTask);
                            }
                            else if (!TaskListCollection.Any(x => (x.Guid != _selectedTaskListGuid) && (x.IsRunningOrPending)))
                            {
                                // No more tasks are queued to run. Hide preview.
                                _selectedTaskGuid = Guid.Empty;
                                EndTrackExecution();
                            }
                        }

                        // Prune non-existent Tasks
                        int j = taskArray.Length;
                        while (ActiveListCollection.Count > taskArray.Length)
                        {
                            ActiveListCollection.RemoveAt(j);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.AllExceptionsToString());
                    }
                });
            }
            else
            {
                // No Tasks exist, clear everything
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    LoadingTasksRing.IsActive = false;
                    ActiveListCollection.Clear();
                });
            }
        }

        private async Task TrackTaskExecution(TaskBase latestTask, bool settingChanged = false)
        {
            var item = ActiveListCollection.Where(x => x.Task.Guid == latestTask.Guid).First();
            ActiveTestsResultsView.SelectedItem = item;
            ActiveTestsView.SelectedItem = item;
            FollowOutput = true;

            // Ensure the running task has changed before updating UI
            if (settingChanged || (_selectedTaskGuid != latestTask.Guid))
            {
                // Prepare result preview
                _selectedTaskGuid = latestTask.Guid;
                await ResultsPageEmbedded.SetupForTask(latestTask).ConfigureAwait(true);
                // Make result preview visible
                ResultsPreviewScrollView.Visibility = Visibility.Visible;
                ResultsPreviewTaskName.Visibility = Visibility.Visible;
                ResultsPreviewTaskName.Text = latestTask.Name;
                LayoutRoot.RowDefinitions.Last().Height = new GridLength(1, GridUnitType.Star);
                LayoutRoot.RowDefinitions[2].Height = GridLength.Auto;
                EnsureSelectedIndexVisible(ActiveTestsView, TestsScrollView);
            }
        }

        private async void OnUpdatedTaskListGuidAndStatusAsync(object source, ServerPollerEventArgs e)
        {
            var newSummaries = (List<TaskListSummary>)e.Result;

            // Get the new TaskLists
            if (newSummaries != null)
            {
                int newCount = newSummaries.Count;

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    try
                    {
                        if (newSummaries.Any(x => x.IsRunningOrPending))
                        {
                            RunAllButton.Content = resourceLoader.GetString("AbortAllButton/Content");
                            ToolTipService.SetToolTip(RunAllButton, resourceLoader.GetString("AbortAllButton/ToolTipService/ToolTip"));
                            AutomationProperties.SetName(RunAllButton, resourceLoader.GetString("AbortAllButton/ToolTipService/ToolTip"));
                        }
                        else
                        {
                            
                            RunAllButton.Content = resourceLoader.GetString("RunAllButton/Content");
                            ToolTipService.SetToolTip(RunAllButton, resourceLoader.GetString("RunAllButton/ToolTipService/ToolTip"));
                            AutomationProperties.SetName(RunAllButton, resourceLoader.GetString("RunAllButton/ToolTipService/ToolTip"));
                        }

                        if (newCount == 0)
                        {
                            // No TaskLists exist, clear everything
                            LoadingTasksRing.IsActive = false;
                            TaskListCollection.Clear();
                            ActiveListCollection.Clear();
                            return;
                        }

                        if (Settings.TrackExecution)
                        {
                            // Find the latest running list. If none are running find the first run pending list.
                            var latestList = newSummaries.Where(x => x.Status == TaskStatus.Running).DefaultIfEmpty(new TaskListSummary()).LastOrDefault();
                            if (latestList.Guid == Guid.Empty)
                            {
                                latestList = newSummaries.Where(x => x.Status == TaskStatus.RunPending).DefaultIfEmpty(new TaskListSummary()).FirstOrDefault();
                            }

                            if (latestList.Guid != Guid.Empty)
                            {
                                _selectedTaskListGuid = latestList.Guid;
                            }
                        }

                        bool selectedListFound = false;
                        for (int i = 0; i < newSummaries.Count; i++)
                        {
                            var newSummary = newSummaries[i];

                            if (newSummary.Guid == _selectedTaskListGuid) 
                            {
                                selectedListFound = true;
                            }

                            // Update the TaskListCollection
                            if (i == TaskListCollection.Count)
                            {
                                TaskListCollection.Insert(i, newSummary);
                            }
                            else if (!TaskListCollection[i].Equals(newSummary))
                            {
                                // Template reselected automatically
                                TaskListCollection[i] = newSummary;
                            }

                            // Ensure correct list is selected
                            if ((_selectedTaskListGuid == newSummary.Guid) && (TaskListsView.SelectedIndex != i))
                            {
                                TaskListsView.SelectedIndex = i;
                            }
                        }
                        
                        // Prune non-existent Lists
                        int j = newSummaries.Count;
                        while (TaskListCollection.Count > newSummaries.Count)
                        {
                            TaskListCollection.RemoveAt(j);
                        }

                        if (!selectedListFound && _selectedTaskList != -1)
                        {
                            // The selected list was deleted
                            _selectedTaskList = -1;
                            _selectedTaskListGuid = Guid.Empty;

                            TaskListsView.SelectedIndex = -1;
                            ActiveListCollection.Clear();
                            LoadingTasksRing.IsActive = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.AllExceptionsToString());
                    }
                });
            }
            else
            {
                // No TaskLists exist, clear everything
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    LoadingTasksRing.IsActive = false;
                    TaskListCollection.Clear();
                    ActiveListCollection.Clear();
                    EndTrackExecution();
                });
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Client = ((App)Application.Current).Client;
            if (e != null)
            {
                mainPage = (Frame)e.Parameter;
            }
            EndTrackExecution();

            if (((App)Application.Current).IsServiceExecutingBootTasks)
            {
                UpdateHeaders(false);
            }
            else
            {
                UpdateHeaders(true);
            }

            if (_activeListPoller != null)
            {
                _activeListPoller.StartPolling(Client);
            }

            if (_taskListGuidPoller == null)
            {
                _taskListGuidPoller = new ServerPoller(null, typeof(TaskList), 1000, true, 2);
                _taskListGuidPoller.OnUpdatedObject += OnUpdatedTaskListGuidAndStatusAsync;
                _taskListGuidPoller.OnException += ((App)Application.Current).OnServerPollerException;
            }

            _taskListGuidPoller.StartPolling(Client);

            if (_selectedTaskList != -1)
            {
                TaskListsView.SelectedIndex = _selectedTaskList;
                if (Settings.TrackExecution)
                {
                    EnsureSelectedIndexVisible(TaskListsView, TaskListsScrollView);
                }
            }
        }

        /// <summary>
        /// Event handler for boot tasks completing.
        /// </summary>
        private async void TaskListExecutionPage_OnServiceDoneExecutingBootTasks()
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                UpdateHeaders(true);
            });
        }

        private async void TaskListExecutionPage_OnServiceStart()
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                UpdateHeaders(false);
            });
        }

        /// <summary>
        /// Updates headers depending on if boot tasks are executing.
        /// </summary>
        /// <param name="bootTasksComplete"></param>
        private void UpdateHeaders(bool bootTasksComplete)
        {
            lock (_headerUpdateLock)
            {
                if (bootTasksComplete)
                {
                    TaskListsText.Text = resourceLoader.GetString("TaskListsText/Text");
                    TasksText.Text = resourceLoader.GetString("TasksText/Text");
                }
                else
                {
                    TaskListsText.Text = $"{resourceLoader.GetString("Boot")} {resourceLoader.GetString("TaskListsText/Text")}";
                    TasksText.Text = $"{resourceLoader.GetString("Boot")} {resourceLoader.GetString("TasksText/Text")}";
                }
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (_activeListPoller != null)
            {
                _activeListPoller.StopPolling();
            }

            _taskListGuidPoller.StopPolling();
        }

        private void RunAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (TaskListCollection.All(x => x.IsRunningOrPending == false))
            {
                _ = Client.RunAllTaskLists();
            }
            else
            {
                _ = Client.AbortAll();
            }
        }

        // TaskList embedded buttons
        private void RunListButton_Click(object sender, RoutedEventArgs e)
        {
            var guid = GetTaskListGuidFromButton(sender as Button);
            _ = Client.RunTaskList(guid);
        }

        private async void ResumeListButton_Click(object sender, RoutedEventArgs e)
        {
            var guid = GetTaskListGuidFromButton(sender as Button);
            var newList = await Client.QueryTaskList(guid);
            if (newList != null)
            {
                var lastTask = newList.Tasks.Where(x => x.LatestTaskRunStatus == TaskStatus.Aborted).DefaultIfEmpty(null).FirstOrDefault();
                var index = newList.Tasks.FindIndex(x => x.Guid.Equals(lastTask.Guid));
                _ = Client.RunTaskList(guid, index);
            }
        }

        private void PauseListButton_Click(object sender, RoutedEventArgs e)
        {
            var guid = GetTaskListGuidFromButton(sender as Button);
            _ = Client.AbortTaskList(guid);
        }

        private void RestartListButton_Click(object sender, RoutedEventArgs e)
        {
            RunListButton_Click(sender, e);
        }

        private async void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Settings.TrackExecutionKey)
            {
                if (!Settings.TrackExecution)
                {
                    EndTrackExecution();
                }
                else
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        // Show mini window with latest output
                        var latestTask = ActiveListCollection.Select(x => x.Task).Where(t => t.LatestTaskRunStatus == TaskStatus.Running).DefaultIfEmpty(null).LastOrDefault();
                        if (latestTask == null)
                        {
                            latestTask = ActiveListCollection.Select(x => x.Task).Where(t => t.LatestTaskRunStatus == TaskStatus.RunPending).DefaultIfEmpty(null).FirstOrDefault();
                        }

                        if (latestTask != null)
                        {
                            await TrackTaskExecution(latestTask, true).ConfigureAwait(true);
                        }
                    });
                }
            }
        }

        /// <summary>
        /// Ensures the selected index is visible to the user.
        /// </summary>
        /// <param name="list">ListView to check.</param>
        /// <param name="scroller">Scroll view the ListView is a child of.</param>
        private static void EnsureSelectedIndexVisible(ListView list, ScrollViewer scroller)
        {
            // Get ListItem
            var element = list.ContainerFromIndex(list.SelectedIndex) as FrameworkElement;
            if (element != null)
            {
                // Calculate Y distance between list item and top of scroll view
                var transform = element.TransformToVisual(scroller);
                var pos = transform.TransformPoint(new Point(0, 0));
                // Add (or subtract) Y distance. Y can be negative.
                scroller.ChangeView(null, scroller.VerticalOffset + pos.Y, null);
            }
        }

        /// <summary>
        /// Stops tracking execution, hides results preview.
        /// </summary>
        private void EndTrackExecution()
        {
            ResultsPreviewScrollView.Visibility = Visibility.Collapsed;
            ResultsPreviewTaskName.Visibility = Visibility.Collapsed;
            ResultsPageEmbedded.StopPolling();
            LayoutRoot.RowDefinitions.Last().Height = new GridLength(0);
            LayoutRoot.RowDefinitions[2].Height = new GridLength(0);
        }

        /// <summary>
        /// Given a button associated with a tasklist, returns the tasklist guid.
        /// </summary>
        private static Guid GetTaskListGuidFromButton(Button button)
        {
            return ((TaskListSummary)button.DataContext).Guid;
        }

        /// <summary>
        /// Called when the user interacts with the scroll view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResultsPreviewScrollView_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            // Stop following output
            FollowOutput = false;

            if (e.IsIntermediate)
            {
                return;
            }

            if (ResultsPreviewScrollView.ScrollableHeight == ResultsPreviewScrollView.VerticalOffset)
            {
                // User scrolled to end, resume following output
                FollowOutput = true;
            }
        }

        /// <summary>
        /// Temporary handler for scroll event, only used for automated scroll events generated by ResultsPageEmbedded_SizeChanged.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Temporary_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (e.IsIntermediate)
            {
                return;
            }

            // Scroll is finished. Allow User to interact with scroll bar again.
            ResultsPreviewScrollView.ViewChanged -= Temporary_ViewChanged;
            ResultsPreviewScrollView.ViewChanged -= ResultsPreviewScrollView_ViewChanged;
            ResultsPreviewScrollView.ViewChanged += ResultsPreviewScrollView_ViewChanged;

            ResultsPreviewScrollView.HorizontalScrollMode = ScrollMode.Enabled;
            ResultsPreviewScrollView.VerticalScrollMode = ScrollMode.Enabled;
            ResultsPreviewScrollView.ZoomMode = ZoomMode.Enabled;
        }

        /// <summary>
        /// Called when results preview has new output. Scrolls to end of output if FollowOutput is enabled.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResultsPageEmbedded_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (FollowOutput)
            {
                // Prevent user from interacting with scroll bar.
                ResultsPreviewScrollView.HorizontalScrollMode = ScrollMode.Disabled;
                ResultsPreviewScrollView.VerticalScrollMode = ScrollMode.Disabled;
                ResultsPreviewScrollView.ZoomMode = ZoomMode.Disabled;

                ResultsPreviewScrollView.ViewChanged -= Temporary_ViewChanged;
                ResultsPreviewScrollView.ViewChanged -= ResultsPreviewScrollView_ViewChanged;
                ResultsPreviewScrollView.ViewChanged += Temporary_ViewChanged;

                // Auto scroll to end of output
                ResultsPreviewScrollView.ChangeView(null, ResultsPreviewScrollView.ScrollableHeight, null);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _activeListPoller?.Dispose();
                    _taskListGuidPoller?.Dispose();
                    ResultsPageEmbedded?.Dispose();
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

        private Frame mainPage;
        private ServerPoller _activeListPoller;
        private ServerPoller _taskListGuidPoller;
        private int _selectedTaskList;
        private Guid _selectedTaskListGuid;
        private int _selectedTask;
        private Guid _selectedTaskGuid;
        private readonly object _headerUpdateLock;
        private FactoryOrchestratorUWPClient Client = ((App)Application.Current).Client;
        public ObservableCollection<TaskListSummary> TaskListCollection { get; private set; }
        public ObservableCollection<TaskBaseWithTemplate> ActiveListCollection { get; private set; }
        public bool FollowOutput { get; set; }
        private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();
    }

    /// <summary>
    /// Enum for the possible Task DataTemplates.
    /// </summary>
    public enum TaskViewTemplate
    {
        Normal,
        WithRetryButton
    }

    /// <summary>
    /// A basic wrapper struct to associate a Task with the DataTemplate it should use in the UI.
    /// This is needed since the UI is dependent on the TaskList state, not just the Task.
    /// </summary>
    public struct TaskBaseWithTemplate : IEquatable<TaskBaseWithTemplate>
    {
        public TaskBaseWithTemplate(TaskBase task, TaskViewTemplate template)
        {
            Task = task;
            Template = template;
        }

        public TaskBase Task { get; set; }

        public TaskViewTemplate Template { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is TaskBaseWithTemplate))
            {
                return false;
            }

            return Equals((TaskBaseWithTemplate)obj);
        }

        public bool Equals(TaskBaseWithTemplate other)
        {
            if (other.Task != this.Task)
            {
                return false;
            }

            if (other.Template != this.Template)
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(TaskBaseWithTemplate left, TaskBaseWithTemplate right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TaskBaseWithTemplate left, TaskBaseWithTemplate right)
        {
            return !(left == right);
        }
    }
}
