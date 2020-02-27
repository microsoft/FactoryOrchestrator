using Microsoft.FactoryOrchestrator.Client;
using Microsoft.FactoryOrchestrator.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.ExtendedExecution;
using Windows.Foundation;
using Windows.Management.Deployment;
using Windows.UI.Core;
using Windows.UI.Core.Preview;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
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
            this.UnhandledException += UnhandledExceptionHandler;
            MainPageLastNavTag = null;
            RunWaitingForResult = null;
            Client = null;
            connectionFailureSem = new SemaphoreSlim(1,1);
            pollingFailureSem = new SemaphoreSlim(1,1);
        }

        private void UnhandledExceptionHandler(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            if (e.Exception.GetType() == typeof(FactoryOrchestratorConnectionException))
            {
                e.Handled = true;
                OnConnectionFailure();
            }
            else
            {
                e.Handled = false;
            }

            System.Diagnostics.Debug.WriteLine(e.Exception);
        }
        
        protected Frame PreLaunchSetUp()
        {
            Frame rootFrame = Window.Current.Content as Frame;

            extendedExecution = new ExtendedExecutionSession();
            extendedExecution.Reason = ExtendedExecutionReason.Unspecified;
            _ = extendedExecution.RequestExtensionAsync();

            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
                Window.Current.Activate();
            }

            rootFrame.CacheSize = 4;
            // Requires confirmAppClose restricted capability
            SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += App_CloseRequestedAsync;
            return rootFrame;
        }

        protected override async void OnActivated(IActivatedEventArgs args)
        {
            if (args.Kind == ActivationKind.Protocol)
            {
                ProtocolActivatedEventArgs eventArgs = args as ProtocolActivatedEventArgs;
                var path = eventArgs.Uri.LocalPath;
                Frame rootFrame = PreLaunchSetUp();
                if (Client != null)
                {
                    if (await Client.TryConnect())
                    {
                        rootFrame.Navigate(typeof(MainPage), path);
                    }
                }
                else
                {
                    if (rootFrame.Content == null)
                    {
                        Client = new FactoryOrchestratorUWPClient(IPAddress.Loopback, 45684);
                        Client.OnConnected += OnIpcConnected;

                        if (await Client.TryConnect())
                        {
                            rootFrame.Navigate(typeof(MainPage), path);
                        }
                        else
                        {
                            Client = null;
                            rootFrame.Navigate(typeof(ConnectionPage), path);
                        }
                    }
                   
                }

            }
        }

        internal async void OnServerPollerException(object source, ServerPollerExceptionHandlerArgs e)
        {
            var poller = source as ServerPoller;

            if (e.Exception.GetType() == typeof(FactoryOrchestratorConnectionException))
            {
                OnConnectionFailure();
            }
            else if (poller.IsPolling)
            {
                pollingFailureSem.Wait();
                
                if (e.Exception.GetType() == typeof(FactoryOrchestratorUnkownGuidException))
                {
                    // Service was likely restarted or the guid was deleted via another Client, stop polling it
                    poller.StopPolling();
                }

                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    ContentDialog errorDialog = new ContentDialog()
                    {
                        Title = "Polling Exception",
                        Content = e.Exception.Message + "\r\n\r\nThis can occur when the Factory Orchestrator Service is restarted during an operation.",
                        PrimaryButtonText = $"Continue"
                    };
                    try
                    {
                        await errorDialog.ShowAsync();
                    }
                    catch (Exception)
                    {
                        // TODO: Bug: i think this doesnt work ;)
                        // System.Exception is thrown if there is already a ContentDialog visible on the screen. Just ignore it
                    }
                });
                
                pollingFailureSem.Release();
            }

            System.Diagnostics.Debug.WriteLine(e.Exception);
        }

        public async void OnConnectionFailure()
        {
            connectionFailureSem.Wait();
            if (!OnConnectionPage && !Client.IsConnected)
            {
                IAsyncOperation<ContentDialogResult> resultTask = null;

                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    StackPanel s = new StackPanel();
                    s.Children.Add(
                        new TextBlock()
                        {
                            Text = "Cannot reach Factory Orchestrator Service!" + Environment.NewLine + "Attempting to reconnect...",
                            Margin = new Thickness(10)
                        });

                    s.Children.Add(
                        new ProgressBar()
                        {
                            IsIndeterminate = true
                        });

                    ContentDialog errorDialog = new ContentDialog()
                    {
                        Title = "Communication Error",
                        Content = s,
                        PrimaryButtonText = $"Disconnect from {Client.IpAddress}"
                    };

                    resultTask = errorDialog.ShowAsync();
                });

                while (!await Client.TryConnect())
                {
                    System.Diagnostics.Debug.WriteLine("Waiting for connection...");
                    if (resultTask.Status == AsyncStatus.Completed)
                    {
                        OnConnectionPage = true;

                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            var frame = Window.Current.Content as Frame;
                            frame.Navigate(typeof(ConnectionPage));
                        });

                        break;
                    }
                    else
                    {
                        await Task.Delay(100);
                    }
                }

                resultTask.Cancel();
            }

            connectionFailureSem.Release();
        }


        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = PreLaunchSetUp();

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

        public void OnIpcConnected()
        {
            // disable so this doesnt fire again
            Client.OnConnected -= OnIpcConnected;

            lock (onConnectionLock)
            {
                if (Client.IpAddress == lastIp)
                {
                    return;
                }
                else if (lastIp != null)
                {
                    // We connected to a new device
                    lastIp = Client.IpAddress;
                    return;
                }

                // First connection, do setup. Only runs once!
                lastIp = Client.IpAddress;

                // One thread queues events, another dequeues and handles them.
                // Poll every 1 second
                // TODO: Only start these tasks once, so we can handle new IPC connection correctly. Likely need state cleanup too.
                Task.Run(async () =>
                {
                    while (true)
                    {
                        await CheckForServiceEvents();
                        await Task.Delay(1000);
                    }
                });

                Task.Run(async () =>
                {
                    while (true)
                    {
                        await HandleServiceEvents();
                        await Task.Delay(1000);
                    }
                });
            }
        }

        private async Task CheckForServiceEvents()
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
            catch (FactoryOrchestratorConnectionException)
            {
                // We might change device or the system might have rebooted, start over
                eventSeen = false;
                OnConnectionFailure();
                while (OnConnectionPage || (!Client.IsConnected))
                {
                    await Task.Delay(1000);
                }
            }

            // Handle events in a queue
            if (newEvents.Count > 0)
            {
                eventSeen = true;
                lastEventIndex = newEvents[newEvents.Count - 1].EventIndex;

                foreach (var evnt in newEvents)
                {
                    serviceEventQueue.Enqueue(evnt);
                }
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
                    case ServiceEventType.WaitingForExternalTaskRun:
                        // Check if we are localhost, if so we are the DUT and need to run the UWP task for the server.
                        // If not, do nothing, as we are not the DUT.
                        if (Client.IsLocalHost)
                        {
                            // TODO: Performance: this should be in its own thread, so other service events can be handled
                            // Only allow one external run at a time
                            TaskRun run = null;

                            while (run == null)
                            {
                                try
                                {
                                    run = await Client.QueryTaskRun((Guid)evnt.Guid);
                                }
                                catch (FactoryOrchestratorUnkownGuidException)
                                {
                                    // Run is no longer valid, ignore it
                                    return;
                                }
                                catch (FactoryOrchestratorConnectionException)
                                {
                                    OnConnectionFailure();
                                    while ((OnConnectionPage) || (!Client.IsConnected))
                                    {
                                        await Task.Delay(1000);
                                    }
                                }
                            }
                            if (!run.TaskRunComplete)
                            {
                                await HandleExternalTaskRunAsync(run);
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private async Task HandleExternalTaskRunAsync(TaskRun run)
        {
            RunWaitingForResult = run;
            
            // Navigate to result page
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ((Frame)Window.Current.Content).Navigate(typeof(ExternalTestResultPage));
            });

            // TODO: Performance: Use signaling
            // Block from handing a new system event until the current one is handled
            // This is set by ExternalTestResultPage
            while (!RunWaitingForResult.TaskRunComplete)
            {
                    await Task.Delay(2000);
            }

            RunWaitingForResult = null;
        }

        public TaskRun RunWaitingForResult { get; private set; }
        public string MainPageLastNavTag { get; set; }
        public FactoryOrchestratorUWPClient Client { get; set; }
        public bool OnConnectionPage { get; set; }

        private SemaphoreSlim connectionFailureSem;
        private SemaphoreSlim pollingFailureSem;
        private bool eventSeen = false;
        private ulong lastEventIndex;
        private ConcurrentQueue<ServiceEvent> serviceEventQueue = new ConcurrentQueue<ServiceEvent>();
        private ExtendedExecutionSession extendedExecution = null;
        private object onConnectionLock = new object();
        private IPAddress lastIp = null;
    }
}
