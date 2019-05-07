using Microsoft.FactoryTestFramework.Core;
using Microsoft.FactoryTestFramework.Client;
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

namespace Microsoft.FactoryTestFramework.UWP
{
    // TODO: Periodically check that the testRun was not completed externally

    /// <summary>
    /// A result entry page that queries the user if a given TestRun for an external/UWP test passed or failed on the DUT.
    /// The page is automatically navigated from if the TestRun is "completed" by a remote FTF client.
    /// </summary>
    public sealed partial class ExternalTestResultPage : Page
    {
        public ExternalTestResultPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Get TestRun we are reporting results for
            testRun = (TestRun)e.Parameter;

            // Append test details to UI
            TestText.Text += testRun.TestName;
            TestRunText.Text += testRun.Guid.ToString();

            base.OnNavigatedTo(e);
        }

        private void AbortButton_Click(object sender, RoutedEventArgs e)
        {
            ReportTestRunResultAsync(TestStatus.TestAborted);
        }

        private void FailButton_Click(object sender, RoutedEventArgs e)
        {
            ReportTestRunResultAsync(TestStatus.TestFailed);
        }

        private void PassButton_Click(object sender, RoutedEventArgs e)
        {
            ReportTestRunResultAsync(TestStatus.TestPassed);
        }

        private async void ReportTestRunResultAsync(TestStatus result)
        {
            testRun.TestStatus = result;

            if (result != TestStatus.TestAborted)
            {
                // Don't consider the test "done" until the test passed/failed and that result was chosen by the user.
                // This is consistent with how FTFServer handles exe & TAEF tests.
                testRun.TimeFinished = DateTime.Now;

                // Set the exit code
                testRun.ExitCode = result == (TestStatus.TestPassed) ? 0 : -1;
            }

            // Return result to server
            await IPCClientHelper.IpcClient.InvokeAsync(x => x.SetTestRunStatus(testRun));

            if (this.Frame.CanGoBack)
            {
                // Return to last page
                this.Frame.GoBack();
            }
            else
            {
                // Return to MainPage
                this.Frame.Navigate(typeof(MainPage));
            }
        }

        private TestRun testRun;
    }
}
