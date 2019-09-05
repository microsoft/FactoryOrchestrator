using Microsoft.FactoryOrchestrator.Client;
using Microsoft.FactoryOrchestrator.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

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
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            lastOutput = 0;
            if (e.Parameter != null)
            {
                _test = (TaskBase)e.Parameter;
                CreateHeader();
                _testPoller = new ServerPoller(_test.Guid, typeof(TaskBase), IPCClientHelper.IpcClient, 5000);
                _testPoller.OnUpdatedObject += OnUpdatedTestAsync;
                _testPoller.StartPolling();
                if (!TryCreateTaskRunPoller(_test.LatestTaskRunGuid))
                {
                    // Set task status to not run
                    OverallTestResult.Text = "❔ Not Run";
                }
            }
            else
            {
                _test = null;
            }

            UpdateTaskRunNav(null);

            BackButton.IsEnabled = this.Frame.CanGoBack;
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (_testPoller != null)
            {
                _testPoller.StopPolling();
            }
            if (_taskRunPoller != null)
            {
                _taskRunPoller.StopPolling();
                _taskRunPoller = null;
            }
        }

        private async void OnUpdatedTestAsync(object source, ServerPollerEventArgs e)
        {
            _test = (TaskBase)e.Result;
            if ((_test != null) && (_taskRunPoller == null))
            {
                TryCreateTaskRunPoller(_test.LatestTaskRunGuid);
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
                    // Clear output, update navi
                    lastOutput = 0;
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        OutputStack.Children.Clear();
                        UpdateTaskRunNav(newRun);

                    });
                }

                _selectedRun = newRun;

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    UpdateResultsSummary();
                });


                while (lastOutput != _selectedRun.TaskOutput.Count)
                {
                    var blocks = PrepareOutput();
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        UpdateOutput(blocks);
                    });
                }
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
            TestHeader.Text = String.Format("{0} ({1})", _test.Name, _test.Guid.ToString());
            if (_test.Arguments != null)
            {
                Args.Text = _test.Arguments.ToString();
                Args.Visibility = Visibility.Visible;
                ArgsConst.Visibility = Visibility.Visible;
            }
            else
            {
                Args.Visibility = Visibility.Collapsed;
                ArgsConst.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateResultsSummary()
        {
            var children = TestResultSummaryStack.Children;
            switch (_selectedRun.TaskStatus)
            {
                case TaskStatus.Passed:
                    OverallTestResult.Text = "✔ Passed";
                    break;
                case TaskStatus.Failed:
                    OverallTestResult.Text = "❌ Failed";
                    break;
                case TaskStatus.Running:
                    OverallTestResult.Text = "▶ Running";
                    break;
                case TaskStatus.NotRun:
                    OverallTestResult.Text = "❔ Not Run";
                    break;
                case TaskStatus.Aborted:
                    OverallTestResult.Text = "⛔ Aborted";
                    break;
                case TaskStatus.Timeout:
                    OverallTestResult.Text = "⏱ Timed-out";
                    break;
                default:
                    OverallTestResult.Text = "❔ Unknown";
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
            TaskRunStack.Visibility = Visibility.Visible;

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
                    if (errorBlock && _selectedRun.TaskOutput[i].StartsWith("ERROR: "))
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
                    else if (!errorBlock && _selectedRun.TaskOutput[i].StartsWith("ERROR: "))
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

        private bool TryCreateTaskRunPoller(Guid? taskRunGuid)
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
                                _taskRunPoller.StartPolling();
                            }
                            return true;
                        }
                        else
                        {
                            _taskRunPoller.StopPolling();
                        }
                    }

                    _taskRunPoller = new ServerPoller((Guid)taskRunGuid, typeof(TaskRun), IPCClientHelper.IpcClient, 1000);
                    _taskRunPoller.OnUpdatedObject += OnUpdatedTaskRunAsync;
                    _taskRunPoller.StartPolling();
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

        private TaskBase _test;
        private TaskRun _selectedRun;
        private ServerPoller _taskRunPoller;
        private ServerPoller _testPoller;
        private object _taskRunPollLock = new object();
        private int lastOutput;

    }
}
