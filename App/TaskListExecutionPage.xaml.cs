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
            this.TestViewModel = new TestViewModel();
            this.DataContext = TestViewModel;
            _listUpdateSem = new SemaphoreSlim(1, 1);
            _selectedTaskList = -1;
            mainPage = null;
#if DEBUG
            DisablePolling.Visibility = Visibility.Visible;
#endif
        }

        public TestViewModel TestViewModel { get; set; }

        private void TaskListsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TaskListsView.SelectedItem != null)
            {
                Guid taskListGuid = (Guid)TaskListsView.SelectedItem;
                _selectedTaskList = TaskListsView.SelectedIndex;
                TestViewModel.SetActiveTaskList(taskListGuid);
                if (_activeListPoller != null)
                {
                    _activeListPoller.StopPolling();
                }
                _activeListPoller = new ServerPoller(taskListGuid, typeof(TaskList), IPCClientHelper.IpcClient, 2000);
                _activeListPoller.OnUpdatedObject += OnUpdatedTaskListAsync;
#if DEBUG
                if ((DisablePolling.IsChecked != null) && (bool)(!DisablePolling.IsChecked))
#endif
                {
                    _activeListPoller.StartPolling();
                }
            }
        }

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            if (TaskListsView.SelectedItem != null)
            {
                if (RunButtonIcon.Symbol == Symbol.Play)
                {
                    RunButtonIcon.Symbol = Symbol.Stop;
                    Guid taskListGuid = (Guid)TaskListsView.SelectedItem;
                    await IPCClientHelper.IpcClient.InvokeAsync(x => x.RunTaskList(taskListGuid));
                }
                else if (RunButtonIcon.Symbol == Symbol.Stop)
                {
                    // call Stop TaskList API
                    await IPCClientHelper.IpcClient.InvokeAsync(x => x.AbortAllTaskLists());
                    RunButtonIcon.Symbol = Symbol.Play;
                }
            }
            else
            {
                ContentDialog select = new ContentDialog()
                {
                    Title = "No TaskList selected!",
                    Content = "Select (or create) a TaskList and try again.",
                    CloseButtonText = "Ok"
                };
                await select.ShowAsync();
            }
        }

        private void TestsView_SelectionChanged(object sender, RoutedEventArgs e)
        {
            ListView control = (ListView)sender;
            int index = control.SelectedIndex;
            if (index != -1)
            {
                var testGuid = TestViewModel.TestData.TestGuidsMap[index];
                TaskBase task = TestViewModel.TestData.TaskListMap[(Guid)TestViewModel.TestData.SelectedTaskListGuid].Tasks[testGuid];

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
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {

                    TestViewModel.AddOrUpdateTaskList(list);

                    if (list.TaskListStatus == TaskStatus.Running)
                    {
                        RunButtonIcon.Symbol = Symbol.Stop;
                    }
                    else
                    {
                        RunButtonIcon.Symbol = Symbol.Play;
                    }
                });
            }
        }

        private async void OnUpdatedTaskListGuidsAsync(object source, ServerPollerEventArgs e)
        {
            var taskListGuids = e.Result as List<Guid>;

            if (taskListGuids != null)
            {
                foreach (var guid in taskListGuids)
                {
                    if (!TestViewModel.TestData.TaskListGuids.Contains(guid))
                    {
                        _listUpdateSem.Wait();
                        if (!TestViewModel.TestData.TaskListGuids.Contains(guid))
                        {
                            var list = await IPCClientHelper.IpcClient.InvokeAsync(x => x.QueryTaskList(guid));

                            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                            {
                                TestViewModel.AddOrUpdateTaskList(list);
                                TaskListsView.ItemsSource = TestViewModel.TestData.TaskListGuids;
                                if (TaskListsView.SelectedItem == null)
                                {
                                    TestViewModel.TestData.SelectedTaskListGuid = list.Guid;
                                    TaskListsView.SelectedItem = list.Guid;
                                }
                            });
                        }

                        _listUpdateSem.Release();
                    }
                }
                
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    if (TestViewModel.PruneKnownTaskLists(taskListGuids))
                    {
                        TaskListsView.ItemsSource = TestViewModel.TestData.TaskListGuids;
                    }
                });
                
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            mainPage = (Frame)e.Parameter;

            // TODO: Quality: This is a hack so that if you click on the same task again after returning from the results page the selection is changed
            TestsView.SelectedIndex = -1;

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
                _taskListGuidPoller = new ServerPoller(null, typeof(TaskList), IPCClientHelper.IpcClient, 2000);
                _taskListGuidPoller.OnUpdatedObject += OnUpdatedTaskListGuidsAsync;
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

        private Frame mainPage;
        private ServerPoller _activeListPoller;
        private ServerPoller _taskListGuidPoller;
        private int _selectedTaskList;
        private SemaphoreSlim _listUpdateSem;
    }
}
