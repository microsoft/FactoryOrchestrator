using FTFClient;
using FTFTestExecution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace FTFUWP
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
                UpdateResultsSummary();
                UpdateOutput();
                UpdateArgs();
                _poller = new FTFClient.FTFPoller(_test.Guid, typeof(TestBase), IPCClientHelper.IpcClient, 1000);
                _poller.OnUpdatedObject += OnUpdatedTestAsync;
                _poller.StartPolling();
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
            if (_poller != null)
            {
                _poller.StopPolling();
            }
        }

        private async void OnUpdatedTestAsync(object source, FTFPollEventArgs e)
        {
            if (_test != null)
            {
                _test = (TestBase)e.Result;
            }
            //CreateHeader();

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                UpdateResultsSummary();
                UpdateOutput();
            });
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
            switch (_test.TestStatus)
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


            switch (_test.TestStatus)
            {
                case TestStatus.TestPassed:
                case TestStatus.TestFailed:
                    ExitCode.Text = _test.ExitCode.ToString();
                    ExitCodeConst.Visibility = Visibility.Visible;
                    ExitCode.Visibility = Visibility.Visible;
                    break;
                default:
                    break;
            }

            if (_test.LastTimeStarted != null)
            {
                LastTimeRun.Text = _test.LastTimeStarted.ToString();
                LastTimeRunConst.Visibility = Visibility.Visible;
                LastTimeRun.Visibility = Visibility.Visible;
            }

            if (_test.TestRunTime != null)
            {
                RunTime.Text = _test.TestRunTime.ToString();
                RunTimeConst.Visibility = Visibility.Visible;
                RunTime.Visibility = Visibility.Visible;
            }

            if (_test.TestRunTime != null)
            {
                RunTime.Text = _test.TestRunTime.ToString();
                RunTimeConst.Visibility = Visibility.Visible;
                RunTime.Visibility = Visibility.Visible;
            }

            if (_test.LogFilePath != null)
            {
                LogPath.Text = _test.LogFilePath.ToString();
                LogPathConst.Visibility = Visibility.Visible;
                LogPath.Visibility = Visibility.Visible;
            }

            // TODO: Wire up test cases when we track those for TAEF
        }

        private void UpdateOutput()
        {
            for (int i = OutputStack.Children.Count; i < _test.TestOutput.Count; i++)
            {
                var line = (i + 1).ToString();

                if (_test.TestOutput[i] != null)
                {
                    var textBlock = new TextBlock()
                    {
                        Text = _test.TestOutput[i],
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

        private TestBase _test;
        private FTFClient.FTFPoller _poller;
    }
}
