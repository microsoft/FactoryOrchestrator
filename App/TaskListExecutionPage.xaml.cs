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

namespace Microsoft.FactoryOrchestrator.UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TaskListExecutionPage : Page
    {
        public TaskListExecutionPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
            _selectedTaskList = -1;
            _selectedTaskListGuid = Guid.Empty;
            mainPage = null;
            TaskListCollection = new ObservableCollection<TaskListSummary>();
            ActiveListCollection = new ObservableCollection<TaskBaseWithTemplate>();
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
                }

                // Keep indicies in sync
                TaskListsResultsAndButtonsView.SelectedIndex = _selectedTaskList;

                // Create new poller
                if (_activeListPoller != null)
                {
                    _activeListPoller.StopPolling();
                }
                _activeListPoller = new ServerPoller(selectedTaskListGuid, typeof(TaskList), Client, 2000);
                _activeListPoller.OnUpdatedObject += OnUpdatedTaskListAsync;
                _activeListPoller.StartPolling();

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

        private void TaskListsResultsAndButtonsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((TaskListsResultsAndButtonsView.SelectedIndex != -1) && (_selectedTaskList != TaskListsResultsAndButtonsView.SelectedIndex))
            {
                // Select the tasklist to trigger TaskListsView_SelectionChanged
                TaskListsView.SelectedIndex = TaskListsResultsAndButtonsView.SelectedIndex;
            }
        }


        private void ActiveTestsResultsView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem != null)
            {
                var taskWithTemplate = (TaskBaseWithTemplate)(e.ClickedItem);
                // Navigate from the MainPage frame so this is a "full screen" page
                mainPage.Navigate(typeof(ResultsPage), taskWithTemplate.Task);
                this.OnNavigatedFrom(null);
            }
        }

        private async void OnUpdatedTaskListAsync(object source, ServerPollerEventArgs e)
        {
            if (e.Result != null)
            {
                TaskList list = (TaskList)e.Result;
                var taskArray = list.Tasks.ToArray();

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
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

        private async void OnUpdatedTaskListGuidAndStatusAsync(object source, ServerPollerEventArgs e)
        {
            var newSummaries = e.Result as List<TaskListSummary>;

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
                            RunAllButton.Content = "Abort all";
                        }
                        else
                        {
                            RunAllButton.Content = "Run all";
                        }

                        if (newCount == 0)
                        {
                            // No TaskLists exist, clear everything
                            LoadingTasksRing.IsActive = false;
                            TaskListCollection.Clear();
                            ActiveListCollection.Clear();
                            return;
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

                                if (_selectedTaskListGuid == newSummary.Guid)
                                {
                                    // Ensure this list is still selected
                                    TaskListsView.SelectedIndex = i;
                                }
                            }
                        }
                        
                        // Prune non-existent Lists
                        int j = newSummaries.Count;
                        while (TaskListCollection.Count > newSummaries.Count)
                        {
                            TaskListCollection.RemoveAt(j);
                        }

                        if (!selectedListFound)
                        {
                            // The selected list was deleted
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
                });
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            mainPage = (Frame)e.Parameter;

            if (_activeListPoller != null)
            {
                _activeListPoller.StartPolling();
            }

            if (_taskListGuidPoller == null)
            {
                _taskListGuidPoller = new ServerPoller(null, typeof(TaskList), Client, 1000, true, 2);
                _taskListGuidPoller.OnUpdatedObject += OnUpdatedTaskListGuidAndStatusAsync;
            }

            _taskListGuidPoller.StartPolling();

            if (_selectedTaskList != -1)
            {
                TaskListsView.SelectedIndex = _selectedTaskList;
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
            if (RunAllButton.Content.ToString().Contains("Run", StringComparison.CurrentCultureIgnoreCase))
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
            var lastTask = newList.Tasks.Where(x => x.LatestTaskRunStatus == TaskStatus.Aborted).DefaultIfEmpty(null).FirstOrDefault();
            var index = newList.Tasks.FindIndex(x => x.Guid.Equals(lastTask.Guid));
            _ = Client.RunTaskList(guid, index);
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
        private void RetryTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var guid = ((TaskBaseWithTemplate)button.DataContext).Task.Guid;
            _ = Client.RunTask(guid);
        }

        /// <summary>
        /// Given a button associated with a tasklist, returns the tasklist guid.
        /// </summary>
        private Guid GetTaskListGuidFromButton(Button button)
        {
            return ((TaskListSummary)button.DataContext).Guid;
        }

        private Frame mainPage;
        private ServerPoller _activeListPoller;
        private ServerPoller _taskListGuidPoller;
        private int _selectedTaskList;
        private Guid _selectedTaskListGuid;
        private FactoryOrchestratorUWPClient Client = ((App)Application.Current).Client;
        public ObservableCollection<TaskListSummary> TaskListCollection;
        public ObservableCollection<TaskBaseWithTemplate> ActiveListCollection;
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
    public struct TaskBaseWithTemplate
    {
        public TaskBaseWithTemplate(TaskBase task, TaskViewTemplate template)
        {
            Task = task;
            Template = template;
        }

        public TaskBase Task { get; set; }

        public TaskViewTemplate Template { get; set; }
    }
}
