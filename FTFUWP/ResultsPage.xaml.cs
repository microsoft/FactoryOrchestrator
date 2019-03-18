using Microsoft.FactoryTestFramework.Client;
using Microsoft.FactoryTestFramework.Core;
using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Microsoft.FactoryTestFramework.UWP
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
            if (e.Parameter != null)
            {
                _test = (TestBase)e.Parameter;
                CreateHeader();
                UpdateArgs();
                _testPoller = new FTFPoller(_test.Guid, typeof(TestBase), IPCClientHelper.IpcClient, 5000);
                _testPoller.OnUpdatedObject += OnUpdatedTestAsync;
                _testPoller.StartPolling();
                if (!TryCreateTestRunPoller(_test.LastTestRunGuid))
                {
                    // Set test status to not run
                    OverallTestResult.Text = "❔ Not Run";
                }
            }
            else
            {
                _test = null; // todo
            }
            BackButton.IsEnabled = this.Frame.CanGoBack;
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (_testPoller != null)
            {
                _testPoller.StopPolling();
            }
            if (_testRunPoller != null)
            {
                _testRunPoller.StopPolling();
                _testRunPoller = null;
            }
        }

        private async void OnUpdatedTestAsync(object source, FTFPollEventArgs e)
        {
            _test = (TestBase)e.Result;
            if ((_test != null) && (_testRunPoller == null))
            {
                TryCreateTestRunPoller(_test.LastTestRunGuid);
            }

            if (_test != null)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    CreateHeader();
                    UpdateArgs();
                });
            }
        }

        private async void OnUpdatedTestRunAsync(object source, FTFPollEventArgs e)
        {
            _selectedRun = (TestRun)e.Result;

            if (_selectedRun != null)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    UpdateResultsSummary();
                    UpdateOutput();
                });
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
            TestHeader.Text = String.Format("{0} ({1})", _test.TestName, _test.Guid.ToString());
        }

        private void UpdateResultsSummary()
        {
            var children = TestResultSummaryStack.Children;
            switch (_selectedRun.TestStatus)
            {
                case TestStatus.TestPassed:
                    OverallTestResult.Text = "✔ Passed";
                    break;
                case TestStatus.TestFailed:
                    OverallTestResult.Text = "❌ Failed";
                    break;
                case TestStatus.TestRunning:
                    OverallTestResult.Text = "🕒 Running";
                    break;
                case TestStatus.TestNotRun:
                    OverallTestResult.Text = "❔ Not Run";
                    break;
                case TestStatus.TestAborted:
                    OverallTestResult.Text = "⛔ Aborted";
                    break;
                default:
                    OverallTestResult.Text = "❔ Unknown";
                    break;
            }


            switch (_selectedRun.TestStatus)
            {
                case TestStatus.TestPassed:
                case TestStatus.TestFailed:
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

            // TODO: Wire up test cases when we track those for TAEF
        }

        private void UpdateOutput()
        {
            for (int i = OutputStack.Children.Count; i < _selectedRun.TestOutput.Count; i++)
            {
                var line = (i + 1).ToString();

                if (_selectedRun.TestOutput[i] != null)
                {
                    var textBlock = new TextBlock()
                    {
                        Text = _selectedRun.TestOutput[i],
                        Name = "OuptutForLineNo" + line
                    };

                    if (line.StartsWith("ERROR: "))
                    {
                        textBlock.FontWeight = Windows.UI.Text.FontWeights.Bold;
                        textBlock.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);
                    }

                    OutputStack.Children.Add(textBlock);

                    LineNoStack.Children.Add(new TextBlock()
                    {
                        Text = line,
                        Name = "LineNo" + line
                    });
                }
            }
        }

        private void UpdateArgs()
        {
            if (_test.Arguments != null)
            {
                Args.Text = _test.Arguments.ToString();
                Args.IsReadOnly = true;
            }
            else
            {
                _test.Arguments = "";
            }
        }

        private void UpdateArgsOnServer()
        {

        }

        private bool TryCreateTestRunPoller(Guid? testRunGuid)
        {
            lock (_testRunPollLock)
            {
                if (testRunGuid != null)
                {
                    if (_testRunPoller != null)
                    {
                        if (_testRunPoller.PollingGuid == testRunGuid)
                        {
                            if (!_testRunPoller.IsPolling)
                            {
                                _testRunPoller.StartPolling();
                            }
                            return true;
                        }
                        else
                        {
                            _testRunPoller.StopPolling();
                        }
                    }

                    _testRunPoller = new FTFPoller((Guid)testRunGuid, typeof(TestRun), IPCClientHelper.IpcClient, 1000);
                    _testRunPoller.OnUpdatedObject += OnUpdatedTestRunAsync;
                    _testRunPoller.StartPolling();
                    return true;
                }
            }
            return false;
        }

        private TestBase _test;
        private TestRun _selectedRun;
        private FTFPoller _testRunPoller;
        private FTFPoller _testPoller;
        private object _testRunPollLock = new object();
    }
}
