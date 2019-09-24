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
using Windows.Security.Authentication.Web.Provider;
using Windows.Storage;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

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
            _listUpdateSem = new SemaphoreSlim(1, 1);
            _selectedTaskList = -1;
            mainPage = null;
            TaskListCollection = new ObservableCollection<TaskListSummary>();
            ActiveListCollection = new ObservableCollection<TaskBase>();
#if DEBUG
            DisablePolling.Visibility = Visibility.Visible;
#endif
        }

        private void TaskListsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TaskListsView.SelectedItem != null)
            {
                Guid taskListGuid = ((TaskListSummary)TaskListsView.SelectedItem).Guid;
                _selectedTaskList = TaskListsView.SelectedIndex;

                if (_activeListPoller != null)
                {
                    _activeListPoller.StopPolling();
                }
                _activeListPoller = new ServerPoller(taskListGuid, typeof(TaskList), IPCClientHelper.IpcClient, 2000);
                ActiveListCollection.Clear();
                _activeListPoller.OnUpdatedObject += OnUpdatedTaskListAsync;
#if DEBUG
                if ((DisablePolling.IsChecked != null) && (bool)(!DisablePolling.IsChecked))
#endif
                {
                    _activeListPoller.StartPolling();
                }
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
            if (e.Result != null)
            {
                TaskList list = (TaskList)e.Result;
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    var listArray = list.Tasks.Values.ToArray();
                    for (int i = 0; i < listArray.Length; i++)
                    {
                        try
                        {
                            if (ActiveListCollection[i] != listArray[i])
                            {
                                ActiveListCollection[i] = listArray[i];
                            }
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            ActiveListCollection.Insert(i, listArray[i]);
                        }

                        // Show retry button if tasklist is no longer running and the task failed
                        if (((list.TaskListStatus != TaskStatus.Running) && (list.TaskListStatus != TaskStatus.RunPending)) && (ActiveListCollection[i].LatestTaskRunPassed != null) && (ActiveListCollection[i].LatestTaskRunPassed == false))
                        {
                            ListViewItem item = ResultsView.ContainerFromItem(ActiveListCollection[i]) as ListViewItem;
                            while (item == null)
                            {
                                await Task.Delay(5);
                                item = ResultsView.ContainerFromItem(ActiveListCollection[i]) as ListViewItem;
                            }

                            item.ContentTemplate = this.Resources["RetryTaskBaseTemplate"] as DataTemplate;
                        }
                    }

                    for (int i = listArray.Length; i < ActiveListCollection.Count; i++)
                    {
                        ActiveListCollection.RemoveAt(i);
                    }
                });
            }
        }

        private async void OnUpdatedTaskListGuidAndStatusAsync(object source, ServerPollerEventArgs e)
        {
            _listUpdateSem.Wait();

            var taskListSummaries = e.Result as List<TaskListSummary>;
            if (taskListSummaries != null)
            {
                // Add or update TaskLists
                foreach (var summary in taskListSummaries)
                {
                    TaskListSummary existingSummary = null;
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        var updateNeeded = false;
                        var guidFound = false;
                        for (int i = 0; i < TaskListCollection.Count; i++)
                        {
                            existingSummary = TaskListCollection[i];
                            if (existingSummary.Guid == summary.Guid)
                            {
                                // Replace existing summary.
                                guidFound = true;

                                if (existingSummary.Status != summary.Status)
                                {
                                    existingSummary.Status = summary.Status;
                                    updateNeeded = true;
                                }

                                break;
                            }
                        }

                        if (!guidFound)
                        {
                            // Add new summary
                            TaskListCollection.Add(summary);
                            existingSummary = summary;
                            updateNeeded = true;
                        }

                        // If this list is new or changed, update button state for this TaskList
                        if (updateNeeded)
                        {
                            // Find the listview item for this tasklist. May need to wait for it to exist.
                            ListViewItem item;
                            item = TaskListsView.ContainerFromItem(existingSummary) as ListViewItem;
                            while (item == null)
                            {
                                await Task.Delay(5);
                                item = TaskListsView.ContainerFromItem(existingSummary) as ListViewItem;
                            }

                            SelectTemplate(existingSummary, item, true);
                        }
                    });
                }

                // Prune non-existant lists
                var guids = taskListSummaries.Select(x => x.Guid);
                for (int i = 0; i < TaskListCollection.Count; i++)
                {
                    var item = TaskListCollection[i];
                    if (!guids.Contains(item.Guid))
                    {
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            TaskListCollection.RemoveAt(i);
                        });
                        i--;
                    }
                }
            }

            _listUpdateSem.Release();
        }

        private void SelectTemplate(TaskListSummary list, ListViewItem item, bool updateOtherLists)
        {
            switch (list.Status)
            {
                case TaskStatus.Running:
                    // Another list should never be running, but just in case...
                    item.ContentTemplate = this.Resources["TaskListItemTemplate_Running"] as DataTemplate;
                    if (updateOtherLists && !list.AllowOtherTaskListsToRun)
                    {
                        DisableOtherTaskLists(list);
                    }
                    break;
                case TaskStatus.Aborted:
                    if (list.RunInParallel)
                    {
                        // "Resuming" doesnt make sense for a parallel list
                        item.ContentTemplate = this.Resources["TaskListItemTemplate_Completed"] as DataTemplate;
                    }
                    else
                    {
                        item.ContentTemplate = this.Resources["TaskListItemTemplate_Paused"] as DataTemplate;
                    }

                    if (updateOtherLists && !list.AllowOtherTaskListsToRun)
                    {
                        EnableOtherTaskLists(list);
                    }
                    break;
                case TaskStatus.Passed:
                case TaskStatus.Failed:
                    item.ContentTemplate = this.Resources["TaskListItemTemplate_Completed"] as DataTemplate;
                    if (updateOtherLists && !list.AllowOtherTaskListsToRun)
                    {
                        EnableOtherTaskLists(list);
                    }
                    break;
                default:
                    item.ContentTemplate = this.Resources["TaskListItemTemplate_NotRun"] as DataTemplate;
                    if (updateOtherLists && !list.AllowOtherTaskListsToRun)
                    {
                        EnableOtherTaskLists(list);
                    }
                    break;
            }
        }

        private async void EnableOtherTaskLists(TaskListSummary existingSummary)
        {
            var otherLists = TaskListCollection.Where(x => x.Guid != existingSummary.Guid);
            foreach (var list in otherLists)
            {
                // Find the listview item for this tasklist. May need to wait for it to exist.
                ListViewItem item;
                item = TaskListsView.ContainerFromItem(list) as ListViewItem;
                while (item == null)
                {
                    await Task.Delay(5);
                    item = TaskListsView.ContainerFromItem(list) as ListViewItem;
                }

                // Set to null to reset template
                item.ContentTemplate = null;

                SelectTemplate(list, item, false);
            }
        }

        private async void DisableOtherTaskLists(TaskListSummary existingSummary)
        {
            var otherLists = TaskListCollection.Where(x => x.Guid != existingSummary.Guid);
            foreach (var list in otherLists)
            {
                // Find the listview item for this tasklist. May need to wait for it to exist.
                ListViewItem item;
                item = TaskListsView.ContainerFromItem(list) as ListViewItem;
                while (item == null)
                {
                    await Task.Delay(5);
                    item = TaskListsView.ContainerFromItem(list) as ListViewItem;
                }

                while (item.ContentTemplateRoot == null)
                {
                    await Task.Delay(5);
                }

                // Get the backing grid, disable all buttons
                var contentGrid = item.ContentTemplateRoot as Grid;
                ToolTip t = new ToolTip();
                t.Content = "Disabled due to other running TaskList";
                ToolTipService.SetToolTip(contentGrid, t);
                var button = contentGrid.FindName("RestartListButton") as Button;
                if (button != null)
                {
                    button.IsEnabled = false;
                    ToolTipService.SetToolTip(button, t);
                }
                button = contentGrid.FindName("RunListButton") as Button;
                if (button != null)
                {
                    button.IsEnabled = false;
                    ToolTipService.SetToolTip(button, t);
                }
                button = contentGrid.FindName("ResumeListButton") as Button;
                if (button != null)
                {
                    button.IsEnabled = false;
                    ToolTipService.SetToolTip(button, t);
                }
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            mainPage = (Frame)e.Parameter;

            // TODO: Quality: This is a hack so that if you click on the same task again after returning from the results page the selection is changed
            //TestsView.SelectedIndex = -1;

            if (_activeListPoller != null)
            {
#if DEBUG
                if ((DisablePolling.IsChecked != null) && (bool)(!DisablePolling.IsChecked))
#endif
                {
                    _activeListPoller.StartPolling();
                }
            }

            if (_taskListGuidPoller == null)
            {
                _taskListGuidPoller = new ServerPoller(null, typeof(TaskList), IPCClientHelper.IpcClient, 1000);
                _taskListGuidPoller.OnUpdatedObject += OnUpdatedTaskListGuidAndStatusAsync;
            }

#if DEBUG
            if ((DisablePolling.IsChecked != null) && (bool)(!DisablePolling.IsChecked))
#endif
            {
                _taskListGuidPoller.StartPolling();
            }

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

        private void DisablePolling_Click(object sender, RoutedEventArgs e)
        {
            if ((DisablePolling.IsChecked != null) && (bool)(DisablePolling.IsChecked))
            {
                if (_activeListPoller != null)
                {
                    _activeListPoller.StopPolling();
                }

                _taskListGuidPoller.StopPolling();
            }
            else
            {
                if (_activeListPoller != null)
                {
                    _activeListPoller.StartPolling();
                }

                _taskListGuidPoller.StartPolling();
            }
        }

        // TaskList embedded buttons
        private void RunListButton_Click(object sender, RoutedEventArgs e)
        {
            var guid = GetTaskListGuidFromButton(sender as Button);
            _ = IPCClientHelper.IpcClient.InvokeAsync(x => x.RunTaskList(guid));
        }

        private async void ResumeListButton_Click(object sender, RoutedEventArgs e)
        {
            var guid = GetTaskListGuidFromButton(sender as Button);
            var newStatus = await IPCClientHelper.IpcClient.InvokeAsync(x => x.QueryTaskList(guid));
            var lastTask = newStatus.Tasks.Values.Where(x => x.LatestTaskRunStatus == TaskStatus.Aborted).DefaultIfEmpty(null).FirstOrDefault();
            var index = newStatus.Tasks.Keys.ToList().IndexOf(lastTask.Guid);
            _ = IPCClientHelper.IpcClient.InvokeAsync(x => x.RunTaskList(guid, index));
        }

        private void PauseListButton_Click(object sender, RoutedEventArgs e)
        {
            var guid = GetTaskListGuidFromButton(sender as Button);
            _ = IPCClientHelper.IpcClient.InvokeAsync(x => x.AbortTaskList(guid));
        }

        private void RestartListButton_Click(object sender, RoutedEventArgs e)
        {
            RunListButton_Click(sender, e);
        }
        private void RetryTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var guid = (button.DataContext as TaskBase).Guid;
            _ = IPCClientHelper.IpcClient.InvokeAsync(x => x.RunTask(guid));
        }

        /// <summary>
        /// Given a button associated with a tasklist, returns the tasklist.
        /// </summary>
        private Guid GetTaskListGuidFromButton(Button button)
        {
            return (button.DataContext as TaskListSummary).Guid;
        }

        private Frame mainPage;
        private ServerPoller _activeListPoller;
        private ServerPoller _taskListGuidPoller;
        private int _selectedTaskList;
        private SemaphoreSlim _listUpdateSem;
        public ObservableCollection<TaskListSummary> TaskListCollection;
        public ObservableCollection<TaskBase> ActiveListCollection;
    }
}
