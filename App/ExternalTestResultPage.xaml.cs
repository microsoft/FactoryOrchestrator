using Microsoft.FactoryOrchestrator.Core;
using Microsoft.FactoryOrchestrator.Client;
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
using Windows.ApplicationModel.Core;
using System.Threading.Tasks;
using TaskStatus = Microsoft.FactoryOrchestrator.Core.TaskStatus;
using Windows.Devices.PointOfService;

namespace Microsoft.FactoryOrchestrator.UWP
{
    /// <summary>
    /// A result entry page that queries the user if a given TaskRun for an external/UWP task passed or failed on the DUT.
    /// The page is automatically navigated from if the TaskRun is "completed" by a remote FO client.
    /// </summary>
    public sealed partial class ExternalTestResultPage : Page
    {
        public ExternalTestResultPage()
        {
            this.InitializeComponent();
            updateLock = new object();
            testReportReady = false;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Client = ((App)Application.Current).Client;

            // Get TaskRun we are reporting results for
            taskRun = ((App)Application.Current).RunWaitingForResult;

            taskRunPoller = new ServerPoller(taskRun.Guid, typeof(TaskRun), 1000);
            taskRunPoller.OnUpdatedObject += OnUpdatedRun;
            taskRunPoller.OnException += TaskRunPoller_OnException;
            taskRunPoller.StartPolling(Client);

            // Append task details to UI
            TestText.Text += taskRun.TaskName;
            if (taskRun.TaskPath != taskRun.TaskName)
            {
                PathText.Text += taskRun.TaskPath;
                PathText.Visibility = Visibility.Visible;
            }

            ArgsText.Text += taskRun.Arguments;
            TaskRunText.Text += taskRun.Guid.ToString();

            base.OnNavigatedTo(e);
        }

        private void TaskRunPoller_OnException(object source, ServerPollerExceptionHandlerArgs e)
        {
            if (e.Exception.GetType() == typeof(FactoryOrchestratorUnkownGuidException))
            {
                // Run no longer valid, mark as aborted
                taskRun.TaskStatus = TaskStatus.Aborted;

                _ = CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    ExitPage();
                });
            }
            else
            {
                // Call global error handler
                ((App)Application.Current).OnServerPollerException(source, e);
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (taskRunPoller != null)
            {
                taskRunPoller.StopPolling();
                taskRunPoller = null;
            }
            base.OnNavigatedFrom(e);
        }

        /// <summary>
        /// Periodically checks if the TaskRun has been completed.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnUpdatedRun(object source, ServerPollerEventArgs e)
        {
            lock (updateLock)
            {
                if (!testReportReady)
                {
                    taskRun = (TaskRun)e.Result;

                    if (taskRun.TaskRunComplete)
                    {
                        _ = CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                            {
                                ExitPage();
                            });
                    }
                }
            }
        }

        private void AbortButton_Click(object sender, RoutedEventArgs e)
        {
            ReportTaskRunResultAsync(TaskStatus.Aborted);
        }

        private void FailButton_Click(object sender, RoutedEventArgs e)
        {
            ReportTaskRunResultAsync(TaskStatus.Failed);
        }

        private void PassButton_Click(object sender, RoutedEventArgs e)
        {
            ReportTaskRunResultAsync(TaskStatus.Passed);
        }

        private async void ReportTaskRunResultAsync(TaskStatus result)
        {
            lock (updateLock)
            {   
                // Prevent OnUpdatedRun from firing
                taskRunPoller.StopPolling();

                if (taskRun.TaskRunComplete)
                {
                    // The task was finished right before user interaction. Return, the poll event handler will exit the page.
                    return;
                }

                testReportReady = true;
                taskRun.TaskStatus = result;
                if (!String.IsNullOrWhiteSpace(CommentBox.Text))
                {
                    taskRun.TaskOutput.Add("------- Start Comments -------");
                    foreach (var line in CommentBox.Text.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
                    {
                        taskRun.TaskOutput.Add(line);
                    }
                    taskRun.TaskOutput.Add("------- End Comments -------");
                }

                if (result != TaskStatus.Aborted)
                {
                    // Don't consider the task "done" until the task passed/failed and that result was chosen by the user.
                    // This is consistent with how FactoryOrchestratorServer handles exe & TAEF tests.
                    taskRun.TimeFinished = DateTime.Now;

                    // Set the exit code
                    taskRun.ExitCode = result == (TaskStatus.Passed) ? 0 : -1;
                }

            }

            // Report selected result to server
            bool updated = false;
            while (!updated)
            {
                try
                {
                    await Client.UpdateTaskRun(taskRun);
                    updated = true;
                }
                catch (FactoryOrchestratorUnkownGuidException)
                {
                    // Run no longer valid, mark as aborted
                    taskRun.TaskStatus = TaskStatus.Aborted;
                }
                catch (FactoryOrchestratorConnectionException)
                {
                    ((App)Application.Current).OnConnectionFailure();
                    while ((((App)Application.Current).OnConnectionPage) || (!Client.IsConnected))
                    {
                        await Task.Delay(1000);
                    }
                }
            }

            ExitPage();
        }

        private void ExitPage()
        {   
            // Update App task, so the ServiceEvent code knows we finished
            ((App)Application.Current).RunWaitingForResult.TaskStatus = taskRun.TaskStatus;

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

        private bool testReportReady;
        private TaskRun taskRun;
        private ServerPoller taskRunPoller;
        private object updateLock;
        private FactoryOrchestratorUWPClient Client = ((App)Application.Current).Client;
    }
}
