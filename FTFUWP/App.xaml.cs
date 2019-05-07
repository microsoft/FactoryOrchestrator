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
using Windows.Management.Deployment;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// TODO: APP TODOS
// Load & Save testlist from folder, file, etc
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
            // One thread queues events, another dequeues and handles them
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
            List<ServiceEvent> newEvents;

            if (firstPoll)
            {
                newEvents = await IPCClientHelper.IpcClient.InvokeAsync(x => x.GetServiceEvents());
            }
            else
            {
                newEvents = await IPCClientHelper.IpcClient.InvokeAsync(x => x.GetServiceEvents(lastEventIndex));
            }

            // Handle events in a queue
            lastEventIndex = newEvents[newEvents.Count - 1].EventIndex;
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
                    case ServiceEventType.WaitingForTestRunByClient:
                        // Check if we are localhost, if so we are the DUT and need to run the UWP test for the server.
                        // If not, do nothing, as we are not the DUT.
                        if (IPCClientHelper.IsLocalHost)
                        {
                            var run = await IPCClientHelper.IpcClient.InvokeAsync(x => x.QueryTestRun((Guid)evnt.Guid));
                            DoExternalAppTestRunAsync(run);
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private async void DoExternalAppTestRunAsync(TestRun run)
        {
            if (run.TestType == TestType.UWP)
            {
                // Launch UWP for results using the PFN in the testrun
                var app = await GetAppByPackageFamilyNameAsync(run.TestPath);

                // TODO: Check if it implements a FTF protocol?
                if (app != null)
                {
                    // Start testRun
                    run.TimeStarted = DateTime.Now;
                    await app.LaunchAsync();
                }
            }

            // Go to result entry page, passing in the active TestRun
            ((Frame)Window.Current.Content).Navigate(typeof(ExternalTestResultPage), run);

            // TODO: Block until result is reported by ExternalTestResultPage
        }

        private static async Task<AppListEntry> GetAppByPackageFamilyNameAsync(string packageFamilyName)
        {
            var pkgManager = new PackageManager();
            var pkg = pkgManager.FindPackagesForUser("", packageFamilyName).FirstOrDefault();

            if (pkg == null)
            {
                return null;
            }

            var apps = await pkg.GetAppListEntriesAsync();
            var firstApp = apps.FirstOrDefault();
            return firstApp;
        }

        private bool firstPoll = true;
        private ulong lastEventIndex;
        private ConcurrentQueue<ServiceEvent> serviceEventQueue = new ConcurrentQueue<ServiceEvent>();
    }
}
