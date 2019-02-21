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
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is TestBase)
            {
                _test = (TestBase)e.Parameter;
                CreateHeader();
                CreateResultsSummary();
                CreateOutput();
            }
            else
            {
                _test = null; // todo
            }
            BackButton.IsEnabled = this.Frame.CanGoBack;
            base.OnNavigatedTo(e);
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

        private void CreateResultsSummary()
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
            
            // TODO: Wire up test cases when we track those for TAEF
        }

        private void CreateOutput()
        {
            for (int i = 0; i < _test.TestOutput.Count; i++)
            {
                var line = (i + 1).ToString();
                LineNoStack.Children.Add(new TextBlock()
                {
                    Text = line,
                    Name = "LineNo" + line
                });

                OutputStack.Children.Add(new TextBlock()
                {
                    Text = _test.TestOutput[i],
                    Name = "OuptutForLineNo" + line
                });
            }
        }

        private TestBase _test;
    }
}
