using Microsoft.FactoryOrchestrator.Client;
using Microsoft.FactoryOrchestrator.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.ExtendedExecution;
using Windows.Management.Deployment;
using Windows.UI.Core;
using Windows.UI.Core.Preview;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using TaskStatus = Microsoft.FactoryOrchestrator.Core.TaskStatus;

namespace Microsoft.FactoryOrchestrator.UWP
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
            MainPageLastNavTag = null;
            uwpRunGuidFromAppsPage = Guid.Empty;
            RunWaitingForResult = null;
            Client = null;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
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
                    Client = new FactoryOrchestratorUWPClient(IPAddress.Loopback, 45684);
                    Client.OnConnected += OnIpcConnected;
                    if (await Client.TryConnect())
                    {
                        rootFrame.Navigate(typeof(MainPage), e.Arguments);
                    }
                    else
                    {
                        // When the navigation stack isn't restored navigate to the first page,
                        // configuring the new page by passing required information as a navigation
                        // parameter
                        Client = null;
                        rootFrame.Navigate(typeof(ConnectionPage), e.Arguments);
                    }
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }

            // Requires confirmAppClose restricted capability
            SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += App_CloseRequestedAsync;
        }

        // Ask the user to confirm before quitting, as on Factory it might not be easy to relaunch the app
        private async void App_CloseRequestedAsync(object sender, SystemNavigationCloseRequestedPreviewEventArgs e)
        {
            var deferral = e.GetDeferral();

            ContentDialog exitFlyout = new ContentDialog()
            {
                Title = "Exit?",
                Content = "Exit Factory Orchestrator?",
                CloseButtonText = "No",
                PrimaryButtonText = "Yes",
            };

            var result = await exitFlyout.ShowAsync();

            if (result == ContentDialogResult.None)
            {
                e.Handled = true;
            }

            deferral.Complete();
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
            _ = extendedExecution.RequestExtensionAsync();

            // One thread queues events, another dequeues and handles them
            // TODO: Only start these tasks once, so we can handle new IPC connection correctly. Likely need state cleanup too.
            Task.Run(() =>
            {
                while (true)
                {
                    CheckForServiceEvents();
                    System.Threading.Thread.Sleep(1000);
                }
            });

            Task.Run(() =>
            {
                while (true)
                {
                    HandleServiceEvents();
                    System.Threading.Thread.Sleep(1000);
                }
            });
        }

        private async void CheckForServiceEvents()
        {
            List<ServiceEvent> newEvents = new List<ServiceEvent>();

            try
            {
                if (!eventSeen)
                {
                    newEvents = await Client.GetServiceEvents();
                }
                else
                {
                    newEvents = await Client.GetServiceEvents(lastEventIndex);
                }
            }
            catch (Exception)
            {
                // TODO: Logging
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

        private async void HandleServiceEvents()
        {
            // Handle one event at a time, oldest first
            ServiceEvent evnt;
            while (serviceEventQueue.TryDequeue(out evnt))
            {
                switch(evnt.ServiceEventType)
                {
                    case ServiceEventType.WaitingForExternalTaskRun:
                        // Check if we are localhost, if so we are the DUT and need to run the UWP task for the server.
                        // If not, do nothing, as we are not the DUT.
                        if (Client.IsLocalHost)
                        {
                            // TODO: Performance: this should be in its own thread, so other service events can be handled
                            // TODO: Bug 21505535: System.Reflection.AmbiguousMatchException in FactoryOrchestrator
                            // Only allow one external run at a time though
                            var run = await Client.QueryTaskRun((Guid)evnt.Guid);
                            if (!run.TaskRunComplete)
                            {
                                await DoExternalAppTaskRunAsync(run);
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private async Task DoExternalAppTaskRunAsync(TaskRun run)
        {
            RunWaitingForResult = run;
            if (RunWaitingForResult.TaskType == TaskType.UWP)
            {
                // Launch UWP for results using the PFN in saved in the testrun
                RunWaitingForResult.TaskOutput.Add($"Preparing to launch {RunWaitingForResult.TaskPath} App");
                var app = await GetAppByPackageFamilyNameAsync(RunWaitingForResult.TaskPath);

                if (app != null)
                {
                    // Start taskRun
                    RunWaitingForResult.TimeStarted = DateTime.Now;
                    bool launched = false;

                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        RunWaitingForResult.TaskOutput.Add($"Attempting to launch {app.ToString()}");

                        launched = await app.LaunchAsync();

                        if (launched)
                        {
                            RunWaitingForResult.TaskOutput.Add($"{app.DisplayInfo.DisplayName} was launched successfully");
                            RunWaitingForResult.TaskStatus = TaskStatus.Running;

                            if (RunWaitingForResult.Guid != uwpRunGuidFromAppsPage)
                            {
                                // Go to result entry page if this was a task, not an invocation from AppsPage
                                ((Frame)Window.Current.Content).Navigate(typeof(ExternalTestResultPage));
                            }
                            else
                            {
                                // Just report it as passed, dont show external result UI
                                RunWaitingForResult.TaskStatus = TaskStatus.Passed;
                                await Client.UpdateTaskRun(RunWaitingForResult);
                                uwpRunGuidFromAppsPage = Guid.Empty;
                            }
                        }
                        else
                        {
                            // Report failure to server
                            RunWaitingForResult.TaskOutput.Add($"Error: {app.DisplayInfo.DisplayName} was unable to launch");
                            ReportAppLaunchFailure();
                        }
                    });
                }
                else
                {
                    ReportAppLaunchFailure();
                }
            }
            else // Not a UWP task, just an external task, launch the result page
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    ((Frame)Window.Current.Content).Navigate(typeof(ExternalTestResultPage));
                });
            }

            // TODO: Performance: Use signaling
            // Block from handing a new system event until the current one is handled
            // This is set by ExternalTestResultPage
            while (!RunWaitingForResult.TaskRunComplete)
            {
                System.Threading.Thread.Sleep(2000);
            }

            RunWaitingForResult = null;
        }

        private async Task<AppListEntry> GetAppByPackageFamilyNameAsync(string packageFamilyName)
        {
            RunWaitingForResult.TaskOutput.Add($"Looking for installed package with Package Family Name {RunWaitingForResult.TaskPath}");

            var pkgManager = new PackageManager();
            var pkg = pkgManager.FindPackagesForUserWithPackageTypes(string.Empty, packageFamilyName, PackageTypes.Main).FirstOrDefault();
            if (pkg == null)
            {
                RunWaitingForResult.TaskOutput.Add($"Error: Could not find installed package with Package Family Name {RunWaitingForResult.TaskPath}");
                return null;
            }

            var apps = await pkg.GetAppListEntriesAsync();
            var appToLaunch = apps.FirstOrDefault();

            if (appToLaunch != null)
            {
                RunWaitingForResult.TaskOutput.Add($"Found App entry {appToLaunch.DisplayInfo.DisplayName} for {RunWaitingForResult.TaskPath}");
            }
            else
            {
                RunWaitingForResult.TaskOutput.Add($"Error: {RunWaitingForResult.TaskPath} had no App entry!");
            }

            return appToLaunch;
        }

        private async void ReportAppLaunchFailure()
        {
            RunWaitingForResult.TaskOutput.Add($"Error: Failed to launch an app for Package Family Name {RunWaitingForResult.TaskPath}");
            RunWaitingForResult.TimeFinished = DateTime.Now;
            RunWaitingForResult.TaskStatus = TaskStatus.Failed;
            RunWaitingForResult.ExitCode = -1;
            await Client.UpdateTaskRun(RunWaitingForResult);
        }

        public TaskRun RunWaitingForResult { get; private set; }
        public Guid uwpRunGuidFromAppsPage { get; set; }
        public string MainPageLastNavTag { get; set; }
        public FactoryOrchestratorUWPClient Client { get; set; }

        private bool eventSeen = false;
        private ulong lastEventIndex;
        private ConcurrentQueue<ServiceEvent> serviceEventQueue = new ConcurrentQueue<ServiceEvent>();
        private ExtendedExecutionSession extendedExecution = null;
    }
}
