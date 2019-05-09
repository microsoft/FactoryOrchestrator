using Microsoft.FactoryTestFramework.Client;
using Microsoft.FactoryTestFramework.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.ExtendedExecution;
using Windows.Management.Deployment;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Microsoft.FactoryTestFramework.UWP
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            rootFrame.CacheSize = 4;

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    IPCClientHelper.OnConnected += OnIpcConnected;
                    rootFrame.Navigate(typeof(ConnectionPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        private void OnIpcConnected()
        {
            // Prevent the app from suspending
            // extendedExecutionUnconstrained capability helps with this also
            // TODO: Performance: Properly handle suspend/resume instead?
            extendedExecution = new ExtendedExecutionSession();
            extendedExecution.Reason = ExtendedExecutionReason.Unspecified;
            extendedExecution.RequestExtensionAsync();

            // One thread queues events, another dequeues and handles them
            // TODO: Only start these tasks once, so we can handle new IPC connection correctly. Likely need state cleanup too.
            Task.Run(async () =>
            {
                while (true)
                {
                    await CheckForServiceEvents();
                    System.Threading.Thread.Sleep(1000);
                }
            });

            Task.Run(async () =>
            {
                while (true)
                {
                    await HandleServiceEvents();
                    System.Threading.Thread.Sleep(1000);
                }
            });
        }

        private async Task CheckForServiceEvents()
        {
            List<ServiceEvent> newEvents;

            if (!eventSeen)
            {
                newEvents = await IPCClientHelper.IpcClient.InvokeAsync(x => x.GetAllServiceEvents());
            }
            else
            {
                newEvents = await IPCClientHelper.IpcClient.InvokeAsync(x => x.GetServiceEventsByIndex(lastEventIndex));
            }

            // Handle events in a queue
            if (newEvents.Count > 0)
            {
                eventSeen = true;
                lastEventIndex = newEvents[newEvents.Count - 1].EventIndex;
            }

            foreach (var evnt in newEvents)
            {
                serviceEventQueue.Enqueue(evnt);
            }
        }

        private async Task HandleServiceEvents()
        {
            // Handle one event at a time, oldest first
            ServiceEvent evnt;
            while (serviceEventQueue.TryDequeue(out evnt))
            {
                switch(evnt.ServiceEventType)
                {
                    case ServiceEventType.WaitingForTestRunByClient:
                        // Check if we are localhost, if so we are the DUT and need to run the UWP test for the server.
                        // If not, do nothing, as we are not the DUT.
                        if (IPCClientHelper.IsLocalHost)
                        {
                            // TODO: Performance: this should be in its own thread, so other service events can be handled
                            // TODO: Bug 21505535: System.Reflection.AmbiguousMatchException in FTF
                            // Only allow one external run at a time though
                            var run = await IPCClientHelper.IpcClient.InvokeAsync(x => x.QueryTestRun((Guid)evnt.Guid));
                            if (!run.TestRunComplete)
                            {
                                await DoExternalAppTestRunAsync(run);
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private async Task DoExternalAppTestRunAsync(TestRun run)
        {
            RunWaitingForResult = run;
            if (RunWaitingForResult.TestType == TestType.UWP)
            {
                // Launch UWP for results using the PFN in saved in the testrun
                RunWaitingForResult.TestOutput.Add($"Preparing to launch {RunWaitingForResult.TestPath} App");
                var app = await GetAppByPackageFamilyNameAsync(RunWaitingForResult.TestPath);

                if (app != null)
                {
                    // Start testRun
                    RunWaitingForResult.TimeStarted = DateTime.Now;
                    bool launched = false;

                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        RunWaitingForResult.TestOutput.Add($"Attempting to launch {app.ToString()}");

                        launched = await app.LaunchAsync();

                        if (launched)
                        {
                            // Go to result entry page
                            RunWaitingForResult.TestOutput.Add($"{app.ToString()} was launched successfully");
                            RunWaitingForResult.TestStatus = TestStatus.TestRunning;
                            ((Frame)Window.Current.Content).Navigate(typeof(ExternalTestResultPage));
                        }
                        else
                        {
                            // Report failure to server
                            RunWaitingForResult.TestOutput.Add($"Error: {app.ToString()} was unable to launch");
                            ReportAppLaunchFailure();
                        }
                    });
                }
                else
                {
                    ReportAppLaunchFailure();
                }
            }

            // TODO: Performance: Use signaling
            // Block from handing a new system event until the current one is handled
            // This is set by ExternalTestResultPage
            while (!RunWaitingForResult.TestRunComplete)
            {
                System.Threading.Thread.Sleep(2000);
            }

            RunWaitingForResult = null;
        }

        private async Task<AppListEntry> GetAppByPackageFamilyNameAsync(string packageFamilyName)
        {
            RunWaitingForResult.TestOutput.Add($"Looking for installed package with Package Family Name {RunWaitingForResult.TestPath}");

            var pkgManager = new PackageManager();
            var pkg = pkgManager.FindPackagesForUserWithPackageTypes("", packageFamilyName, PackageTypes.Main).FirstOrDefault();

            if (pkg == null)
            {
                RunWaitingForResult.TestOutput.Add($"Error: Could not find installed package with Package Family Name {RunWaitingForResult.TestPath}");
                return null;
            }

            var apps = await pkg.GetAppListEntriesAsync();
            var appToLaunch = apps.FirstOrDefault();

            if (appToLaunch != null)
            {
                RunWaitingForResult.TestOutput.Add($"Found App entry {appToLaunch.ToString()} for {RunWaitingForResult.TestPath}");
            }
            else
            {
                RunWaitingForResult.TestOutput.Add($"Error: {RunWaitingForResult.TestPath} had no App entry!");
            }

            return appToLaunch;
        }

        private async void ReportAppLaunchFailure()
        {
            RunWaitingForResult.TestOutput.Add($"Error: Failed to launch an app for Package Family Name {RunWaitingForResult.TestPath}");
            RunWaitingForResult.TimeFinished = DateTime.Now;
            RunWaitingForResult.TestStatus = TestStatus.TestFailed;
            RunWaitingForResult.ExitCode = -1;
            await IPCClientHelper.IpcClient.InvokeAsync(x => x.SetTestRunStatus(RunWaitingForResult));
        }

        public TestRun RunWaitingForResult { get; private set; }
        private bool eventSeen = false;
        private ulong lastEventIndex;
        private ConcurrentQueue<ServiceEvent> serviceEventQueue = new ConcurrentQueue<ServiceEvent>();
        private ExtendedExecutionSession extendedExecution = null;
    }
}
