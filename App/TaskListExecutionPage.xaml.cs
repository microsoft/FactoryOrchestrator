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
            _taskListUpdateSem = new SemaphoreSlim(1, 1);
            _activeListUpdateSem = new SemaphoreSlim(1, 1);
            _uiSem = new SemaphoreSlim(1, 1);
            _activListUiSem = new SemaphoreSlim(1, 1);
            _selectedTaskList = -1;
            _selectedTaskListGuid = null;
            _previousTaskLists = new List<TaskListSummary>();
            mainPage = null;
            TaskListCollection = new ObservableCollection<TaskListSummaryWithTemplate>();
            ActiveListCollection = new ObservableCollection<TaskBase>();
        }

        private void TaskListsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TaskListsView.SelectedItem != null)
            {
                _selectedTaskListGuid = ((TaskListSummaryWithTemplate)TaskListsView.SelectedItem).Summary.Guid;
                _selectedTaskList = TaskListsView.SelectedIndex;

                if (_activeListPoller != null)
                {
                    _activeListPoller.StopPolling();
                }
                _activeListPoller = new ServerPoller(_selectedTaskListGuid, typeof(TaskList), Client, 2000);
                ActiveListCollection.Clear();
                _activeListPoller.OnUpdatedObject += OnUpdatedTaskListAsync;
                _activeListPoller.StartPolling();
            }
            else
            {
                if (_activeListPoller != null)
                {
                    _activeListPoller.StopPolling();
                    _activeListPoller = null;
                }
                ActiveListCollection.Clear();
            }
        }

        private void TestsView_SelectionChanged(object sender, RoutedEventArgs e)
        {
            ListView control = (ListView)sender;
            int index = control.SelectedIndex;
            if (index != -1)
            {
                TaskBase task = ActiveListCollection[index];

                // Navigate from the MainPage frame so this is a "full screen" page
                mainPage.Navigate(typeof(ResultsPage), task);
                this.OnNavigatedFrom(null);
            }
        }

        private async void OnUpdatedTaskListAsync(object source, ServerPollerEventArgs e)
        {
            _activeListUpdateSem.Wait();
            Debug.WriteLine("OnUpdatedTaskListAsync");
            if (e.Result != null)
            {
                TaskList list = (TaskList)e.Result;
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    _activListUiSem.Wait();
                    try
                    {
                        Debug.WriteLine("OnUpdatedTaskListAsync UI thread");

                        var listArray = list.Tasks.ToArray();
                        for (int i = 0; i < listArray.Length; i++)
                        {
                            try
                            {
                                if (i == ActiveListCollection.Count)
                                {
                                    ActiveListCollection.Insert(i, listArray[i]);
                                }
                                else if (!ActiveListCollection[i].Equals(listArray[i]))
                                {
                                    ActiveListCollection[i] = listArray[i];
                                }
                            }
                            catch (ArgumentOutOfRangeException ex)
                            {
                                Debug.WriteLine(ex.AllExceptionsToString());
                            }

                            // Show retry button if tasklist is no longer running and the task failed
                            if (((list.TaskListStatus != TaskStatus.Running) && (list.TaskListStatus != TaskStatus.RunPending)) && (ActiveListCollection[i].LatestTaskRunPassed != null) && (ActiveListCollection[i].LatestTaskRunPassed == false))
                            {
                                // TODO: BUG:23733404 Move to a view selector like the TaskListsView
                                ListViewItem item = ResultsView.ContainerFromItem(ActiveListCollection[i]) as ListViewItem;
                                while (item == null)
                                {
                                    await Task.Delay(5);
                                    item = ResultsView.ContainerFromItem(ActiveListCollection[i]) as ListViewItem;
                                }

                                item.ContentTemplate = this.Resources["RetryTaskBaseTemplate"] as DataTemplate;
                            }
                        }

                        int j = listArray.Length;
                        while (ActiveListCollection.Count > listArray.Length)
                        {
                            ActiveListCollection.RemoveAt(j);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.AllExceptionsToString());
                    }
                    finally
                    {
                        Debug.WriteLine("OnUpdatedTaskListAsync UI thread done");
                        _activListUiSem.Release();
                    }
                });

                Debug.WriteLine("OnUpdatedTaskListAsync done");
                _activeListUpdateSem.Release();
            }
        }

        private async void OnUpdatedTaskListGuidAndStatusAsync(object source, ServerPollerEventArgs e)
        {
            _taskListUpdateSem.Wait();
            Debug.WriteLine("OnUpdatedTaskListGuidAndStatusAsync");

            // Get the new TaskLists
            var newTaskLists = e.Result as List<TaskListSummary>;

            // Do as much as possible before needing to use the UI thread.

            var changedLists = new List<TaskListSummary>();
            int previousCount = _previousTaskLists.Count;
            int newCount = newTaskLists.Count;

            // Find the Lists that no longer exist since we last polled
            var removedLists = _previousTaskLists.Select(x => x.Guid).Except(newTaskLists.Select(y => y.Guid)).ToList();
            int removedCount = removedLists.Count;
            var ListIndexRemoved = new List<bool>(previousCount);
            var selectedTemplate = new List<TaskListViewTemplate>(newCount);

            try
            {
                if ((newCount == previousCount) && (newCount == 0))
                {
                    return;
                }

                // Find the index of the removed lists
                if (removedCount > 0)
                {
                    for (int i = 0; i < previousCount; i++)
                    {
                        if (removedLists.Contains(_previousTaskLists[i].Guid))
                        {
                            ListIndexRemoved.Add(true);
                        }
                        else
                        {
                            ListIndexRemoved.Add(false);
                        }
                    }
                }

                // Compute the desired template
                foreach (var item in newTaskLists)
                {
                    switch (item.Status)
                    {
                        case TaskStatus.Running:
                            selectedTemplate.Add(TaskListViewTemplate.Running);
                            break;
                        case TaskStatus.Aborted:

                            if (item.RunInParallel)
                            {
                                selectedTemplate.Add(TaskListViewTemplate.Completed);
                            }
                            else
                            {
                                selectedTemplate.Add(TaskListViewTemplate.Paused);
                            }
                            break;
                        case TaskStatus.Passed:
                        case TaskStatus.Failed:
                            selectedTemplate.Add(TaskListViewTemplate.Completed);
                            break;
                        default:
                            selectedTemplate.Add(TaskListViewTemplate.NotRun);
                            break;
                    }
                }

                // Iterate through the listview items, removing old ones & updating the rest
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    _uiSem.Wait();
                    Debug.WriteLine("OnUpdatedTaskListGuidAndStatusAsync UI thread");
                    try
                    {
                        if (newTaskLists.Any(x => x.IsRunning))
                        {
                            RunAllButton.Content = "Abort all";
                        }
                        else
                        {
                            RunAllButton.Content = "Run all";
                        }

                        if (newCount == 0)
                        {
                            TaskListCollection.Clear();
                            return;
                        }

                    // Remove deleted lists
                    if (removedCount > 0)
                        {
                            for (int i = 0; i < TaskListCollection.Count; i++)
                            {
                                if (ListIndexRemoved[i] == true)
                                {
                                    TaskListCollection.RemoveAt(i);
                                }
                            }

                        // Wait for collection to be in good state
                        // it can get out of sync when removing a lot of items
                        while (TaskListCollection.Count != previousCount - removedCount)
                            {
                                await Task.Delay(7);
                            }
                        }
                        Debug.WriteLine("OnUpdatedTaskListGuidAndStatusAsync UI thread removals done");

                        for (int i = 0; i < newCount; i++)
                        {
                            if (i < previousCount)
                            {
                                if (!TaskListCollection[i].Summary.Equals(newTaskLists[i]))
                                {
                                    // If anything changed, create a new object so the Template Selector is run again.
                                    TaskListCollection[i] = new TaskListSummaryWithTemplate(new TaskListSummary(newTaskLists[i]), selectedTemplate[i]);

                                    if (_selectedTaskList == i)
                                    {
                                        // Set as selected item if it was previously
                                        TaskListsView.SelectedIndex = i;
                                    }
                                }
                            }
                            else
                            {
                            // add new lists
                            TaskListCollection.Add(new TaskListSummaryWithTemplate(newTaskLists[i], selectedTemplate[i]));
                            }
                        }
                        Debug.WriteLine("OnUpdatedTaskListGuidAndStatusAsync UI thread done");

                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.AllExceptionsToString());
                    }
                    finally
                    {
                        _uiSem.Release();
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.AllExceptionsToString());
            }
            finally
            {
                Debug.WriteLine("OnUpdatedTaskListGuidAndStatusAsync done");

                _previousTaskLists = newTaskLists;
                _taskListUpdateSem.Release();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            mainPage = (Frame)e.Parameter;

            // TODO: Quality: This is a hack so that if you click on the same task again after returning from the results page the selection is changed
            //TestsView.SelectedIndex = -1;

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
            var guid = (button.DataContext as TaskBase).Guid;
            _ = Client.RunTask(guid);
        }

        /// <summary>
        /// Given a button associated with a tasklist, returns the tasklist.
        /// </summary>
        private Guid GetTaskListGuidFromButton(Button button)
        {
            return (button.DataContext as TaskListSummaryWithTemplate).Summary.Guid;
        }

        private Frame mainPage;
        private ServerPoller _activeListPoller;
        private ServerPoller _taskListGuidPoller;
        private int _selectedTaskList;
        private Guid? _selectedTaskListGuid;
        private SemaphoreSlim _taskListUpdateSem;
        private SemaphoreSlim _uiSem;
        private SemaphoreSlim _activListUiSem;
        private SemaphoreSlim _activeListUpdateSem;
        private List<TaskListSummary> _previousTaskLists;
        private FactoryOrchestratorUWPClient Client = ((App)Application.Current).Client;
        public ObservableCollection<TaskListSummaryWithTemplate> TaskListCollection;
        public ObservableCollection<TaskBase> ActiveListCollection;
    }

    /// <summary>
    /// Enum for the possible TaskListView DataTemplates.
    /// </summary>
    public enum TaskListViewTemplate
    {
        Completed,
        Running,
        NotRun,
        Paused
    }

    /// <summary>
    /// A basic wrapper class to associate a TaskListSummary with the DataTemplate it should use in the UI.
    /// </summary>
    public class TaskListSummaryWithTemplate
    {
        public TaskListSummaryWithTemplate(TaskListSummary summary, TaskListViewTemplate template)
        {
            Summary = summary;
            Template = template;
        }

        public TaskListSummary Summary { get; set; }
        public TaskListViewTemplate Template { get; set; }
    }
}
