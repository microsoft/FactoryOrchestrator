// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.FactoryOrchestrator.Client;
using Microsoft.FactoryOrchestrator.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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
    public sealed partial class ResultsPage : Page
    {
        public ResultsPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Disabled;
            _isEmbedded = false;
            _updateSem = new SemaphoreSlim(1, 1);
            _taskRunPollLock = new object();
            _isBootTask = false;
        }

        public async Task SetupForTask(TaskBase task)
        {
            if (((App)Application.Current).IsServiceExecutingBootTasks)
            {
                _isBootTask = true;
            }
            else
            {
                _isBootTask = false;
            }

            Client = ((App)Application.Current).Client;
            StopPolling();
            await ClearOutput();
            _taskRunPoller = null;
            _test = task;
            FollowOutput = true;

            if (task != null)
            {
                _taskPoller = new ServerPoller(_test.Guid, typeof(TaskBase), 5000);
                _taskPoller.OnUpdatedObject += OnUpdatedTestAsync;
                _taskPoller.OnException += OnPollingException;
                _taskPoller.StartPolling(Client);
            }

            UpdateTaskRunNav(null);
        }

        public void StopPolling()
        {
            if (_taskPoller != null)
            {
                _taskPoller.StopPolling();
            }
            if (_taskRunPoller != null)
            {
                _taskRunPoller.StopPolling();
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await SetupForTask(e.Parameter as TaskBase);
            BackButton.IsEnabled = this.Frame.CanGoBack;
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            StopPolling();
        }

        private async void OnUpdatedTestAsync(object source, ServerPollerEventArgs e)
        {
            _test = (TaskBase)e.Result;
            if ((_test != null) && (_taskRunPoller == null))
            {
                if (_test.LatestTaskRunTimeFinished != null)
                {
                    // Test is already finished, don't scroll to end of output
                    FollowOutput = false;
                }

                if (!TryCreateTaskRunPoller(_test.LatestTaskRunGuid))
                {
                    // Set task status to not run
                    OverallTaskResult.Text = "❔ Not Run";
                }
            }
            else if (_test.TaskRunGuids.Count == 0)
            {
                // TaskRuns were deleted while we were looking at the results
                _taskRunPoller.StopPolling();
                _selectedRun = null;
                _taskRunPoller = null;
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    await ClearOutput();
                    UpdateTaskRunNav(_selectedRun);
                    TaskRunGuid.Visibility = Visibility.Collapsed;
                    TaskRunGuidConst.Visibility = Visibility.Collapsed;
                });
            }

            if (_test != null)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    UpdateTaskRunNav(_selectedRun);
                    CreateHeader();
                });
            }
        }

        private async void OnUpdatedTaskRunAsync(object source, ServerPollerEventArgs e)
        {
            var newRun = (TaskRun)e.Result;

            if (newRun != null)
            {
                if (_selectedRun == null || newRun.Guid != _selectedRun.Guid)
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        await ClearOutput();
                        UpdateTaskRunNav(newRun);
                    });
                }

                _selectedRun = newRun;

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    UpdateResultsSummary();
                });


                await _updateSem.WaitAsync();
                try
                {
                    while (lastOutput != _selectedRun.TaskOutput.Count)
                    {
                        var blocks = PrepareOutput();
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            UpdateOutput(blocks);
                        });
                    }

                    if (FollowOutput)
                    {
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            // Scroll to end
                            ScrollView.ViewChanged -= ScrollView_ViewChanged;
                            ScrollView.ViewChanged -= Temporary_ViewChanged;
                            ScrollView.ViewChanged += Temporary_ViewChanged;
                            ScrollView.ChangeView(null, ScrollView.ScrollableHeight, null);
                        });
                    }
                }
                finally
                {
                    _updateSem.Release();
                }
            }
        }

        private async Task ClearOutput()
        {
            await _updateSem.WaitAsync();
            try
            {
                lastOutput = 0;
                OutputStack.Children.Clear();
            }
            finally
            {
                _updateSem.Release();
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            On_BackRequested();
        }

        private bool On_BackRequested()
        {
            if (this.Frame.CanGoBack)
            {
                this.Frame.GoBack();
                return true;
            }
            return false;
        }

        private void BackInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            On_BackRequested();
            args.Handled = true;
        }

        private void CreateHeader()
        {
            TaskHeader.Text = _test.Name;
            TaskGuid.Text = _test.Guid.ToString();
            
            if (_test.Path != null)
            {
                TaskPath.Text = _test.Path;
            }
            else
            {
                TaskPath.Text = "";
            }

            if (_test.Arguments != null)
            {
                Args.Text = _test.Arguments.ToString();
            }
            else
            {
                Args.Text = "";
            }
        }

        private void UpdateResultsSummary()
        {
            var children = TestResultSummaryStack.Children;
            switch (_selectedRun.TaskStatus)
            {
                case TaskStatus.Passed:
                    OverallTaskResult.Text = resourceLoader.GetString("Passed");
                    break;
                case TaskStatus.Failed:
                    OverallTaskResult.Text = resourceLoader.GetString("Failed");
                    break;
                case TaskStatus.Running:
                    OverallTaskResult.Text = resourceLoader.GetString("Running");
                    break;
                case TaskStatus.NotRun:
                    OverallTaskResult.Text = resourceLoader.GetString("NotRun");
                    break;
                case TaskStatus.Aborted:
                    OverallTaskResult.Text = resourceLoader.GetString("Aborted");
                    break;
                case TaskStatus.Timeout:
                    OverallTaskResult.Text = resourceLoader.GetString("TimedOut");
                    break;
                case TaskStatus.RunPending:
                    OverallTaskResult.Text = resourceLoader.GetString("RunPending");
                    break;
                default:
                    OverallTaskResult.Text = resourceLoader.GetString("Unknown");
                    break;
            }


            switch (_selectedRun.TaskStatus)
            {
                case TaskStatus.Passed:
                case TaskStatus.Failed:
                    ExitCode.Text = _selectedRun.ExitCode.ToString();
                    ExitCodeConst.Visibility = Visibility.Visible;
                    ExitCode.Visibility = Visibility.Visible;
                    break;
                default:
                    break;
            }

            if (_selectedRun.TimeStarted != null)
            {
                LastTimeRun.Text = _selectedRun.TimeStarted.ToString();
                LastTimeRunConst.Visibility = Visibility.Visible;
                LastTimeRun.Visibility = Visibility.Visible;
            }

            if (_selectedRun.RunTime != null)
            {
                RunTime.Text = _selectedRun.RunTime.ToString();
                RunTimeConst.Visibility = Visibility.Visible;
                RunTime.Visibility = Visibility.Visible;
            }

            if (_selectedRun.LogFilePath != null)
            {
                LogPath.Text = _selectedRun.LogFilePath.ToString();
                LogPathConst.Visibility = Visibility.Visible;
                LogPath.Visibility = Visibility.Visible;
            }

            TaskRunGuid.Text = _selectedRun.Guid.ToString();
            TaskRunGuid.Visibility = Visibility.Visible;
            TaskRunGuidConst.Visibility = Visibility.Visible;

            // TODO: Feature: Wire up test cases when we track those for TAEF
        }

        /// <summary>
        /// Updates UI with latest console output
        /// </summary>
        private List<(string text, bool isError)> PrepareOutput()
        {
            List<(string text, bool isError)> ret = new List<(string text, bool isError)>();

            var endCount = Math.Min(_selectedRun.TaskOutput.Count, lastOutput + 500);
            string text = "";
            bool errorBlock = false;

            for (int i = lastOutput; i < endCount; i++)
            {
                if (_selectedRun.TaskOutput[i] != null)
                {
                    if (errorBlock && _selectedRun.TaskOutput[i].StartsWith("ERROR: ", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Append error text
                        text += _selectedRun.TaskOutput[i];
                        errorBlock = true;
                    }
                    else if (errorBlock)
                    {
                        // Done with error text, write out the error text and start again
                        var tupl = (text, true);
                        ret.Add(tupl);

                        text = _selectedRun.TaskOutput[i];
                        errorBlock = false;
                    }
                    else if (!errorBlock && _selectedRun.TaskOutput[i].StartsWith("ERROR: ", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Done with normal text, write out the normal text and start again
                        var tupl = (text, false);
                        ret.Add(tupl);

                        text = _selectedRun.TaskOutput[i];
                        errorBlock = true;
                    }
                    else
                    {
                        // Append normal text
                        text += _selectedRun.TaskOutput[i];
                        errorBlock = false;
                    }

                    if (i != (endCount - 1))
                    {
                        text += System.Environment.NewLine;
                    }
                }
            }

            lastOutput = endCount;

            if (!String.IsNullOrEmpty(text))
            {
                if (errorBlock)
                {
                    var tupl = (text, true);
                    ret.Add(tupl);
                }
                else
                {
                    var tupl = (text, false);
                    ret.Add(tupl);
                }
            }

            return ret;
        }

        /// <summary>
        /// Updates UI with latest console output
        /// </summary>
        private void UpdateOutput(List<(string text, bool isError)> blocks)
        {
            foreach (var block in blocks)
            {
                var textBlock = new TextBlock()
                {
                    Text = block.text,
                    IsTextSelectionEnabled = true
                };

                if (block.isError)
                {
                    textBlock.FontWeight = Windows.UI.Text.FontWeights.Bold;
                    textBlock.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);
                }

                OutputStack.Children.Add(textBlock);
            }
        }

        public bool TryCreateTaskRunPoller(Guid? taskRunGuid)
        {
            lock (_taskRunPollLock)
            {
                if (taskRunGuid != null)
                {
                    if (_taskRunPoller != null)
                    {
                        if (_taskRunPoller.PollingGuid == taskRunGuid)
                        {
                            if (!_taskRunPoller.IsPolling)
                            {
                                _taskRunPoller.StartPolling(Client);
                            }
                            return true;
                        }
                        else
                        {
                            _taskRunPoller.StopPolling();
                        }
                    }

                    _taskRunPoller = new ServerPoller((Guid)taskRunGuid, typeof(TaskRun), 1000);
                    _taskRunPoller.OnUpdatedObject += OnUpdatedTaskRunAsync;
                    _taskRunPoller.OnException += OnPollingException;
                    _taskRunPoller.StartPolling(Client);
                    return true;
                }
            }
            return false;
        }

        private void UpdateTaskRunNav(TaskRun run)
        {
            if ((run == null) || (_test.TaskRunGuids.Count <= 1))
            {
                NextRunButton.IsEnabled = false;
                PreviousRunButton.IsEnabled = false;
            }
            else if (run != null)
            {
                if (_test.TaskRunGuids.IndexOf(run.Guid) < _test.TaskRunGuids.Count - 1)
                {
                    NextRunButton.IsEnabled = true;
                }
                if (_test.TaskRunGuids.IndexOf(run.Guid) > 0)
                {
                    PreviousRunButton.IsEnabled = true;
                }
            }
        }

        private void PreviousRunButton_Click(object sender, RoutedEventArgs e)
        {
            // Disable Navi until we get a new TaskRun object
            NextRunButton.IsEnabled = false;
            PreviousRunButton.IsEnabled = false;
            TryCreateTaskRunPoller(_test.TaskRunGuids[_test.TaskRunGuids.IndexOf(_selectedRun.Guid) - 1]);
        }

        private void NextRunButton_Click(object sender, RoutedEventArgs e)
        {
            NextRunButton.IsEnabled = false;
            PreviousRunButton.IsEnabled = false;
            TryCreateTaskRunPoller(_test.TaskRunGuids[_test.TaskRunGuids.IndexOf(_selectedRun.Guid) + 1]);
        }

        private void ScrollView_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            FollowOutput = false;

            if (e.IsIntermediate)
            {
                return;
            }

            if (ScrollView.ScrollableHeight == ScrollView.VerticalOffset)
            {
                FollowOutput = true;
            }
        }

        private void Temporary_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (e.IsIntermediate)
            {
                return;
            }
            ScrollView.ViewChanged -= Temporary_ViewChanged;
            ScrollView.ViewChanged -= ScrollView_ViewChanged;
            ScrollView.ViewChanged += ScrollView_ViewChanged;

            ScrollView.HorizontalScrollMode = ScrollMode.Enabled;
            ScrollView.VerticalScrollMode = ScrollMode.Enabled;
            ScrollView.ZoomMode = ZoomMode.Enabled;
        }

        private async void OnPollingException(object source, ServerPollerExceptionHandlerArgs e)
        {
            bool handled = false;
            if (e.Exception.GetType() == typeof(FactoryOrchestratorUnkownGuidException))
            {
                if (_isBootTask)
                {
                    // This was a Boot Task, but Boot Tasks are completed so the GUID is invalid. Ignore exception, stop polling.
                    if (!await Client.IsExecutingBootTasks())
                    {
                        handled = true;
                        StopPolling();
                    }
                }
            }

            // Pass to global exception handler
            if (!handled)
            {
                ((App)Application.Current).OnServerPollerException(source, e);
            }
        }

        public bool IsEmbedded
        {
            get
            {
                return _isEmbedded;
            }
            set
            {
                if (value == true)
                {
                    BackButton.Visibility = Visibility.Collapsed;
                    PreviousRunButton.Visibility = Visibility.Collapsed;
                    NextRunButton.Visibility = Visibility.Collapsed;
                    TaskHeader.Visibility = Visibility.Collapsed;
                    _isEmbedded = true;
                }
                else
                {
                    BackButton.Visibility = Visibility.Visible;
                    PreviousRunButton.Visibility = Visibility.Visible;
                    NextRunButton.Visibility = Visibility.Visible;
                    TaskHeader.Visibility = Visibility.Visible;
                    _isEmbedded = false;
                }
            }
        }

        public bool FollowOutput { get; set; }

        private TaskBase _test;
        private TaskRun _selectedRun;
        private ServerPoller _taskRunPoller;
        private ServerPoller _taskPoller;
        private object _taskRunPollLock;
        private SemaphoreSlim _updateSem;
        private int lastOutput;
        private bool _isEmbedded;
        private bool _isBootTask;
        private FactoryOrchestratorUWPClient Client = ((App)Application.Current).Client;
        private ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();
    }
}
