// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.FactoryOrchestrator.Core;
using Windows.ApplicationModel.Resources;
using Windows.Devices.Enumeration;
using Windows.Storage;
using Windows.UI.Core;
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
    public sealed partial class ConnectionPage : Page, IDisposable
    {
        public ConnectionPage()
        {
            this.InitializeComponent();
            // Default to loopback on port 45684.
            ((App)Application.Current).Client = new FactoryOrchestratorUWPClient(IPAddress.Loopback);
            ((App)Application.Current).Client.OnConnected += ((App)Application.Current).OnIpcConnected;
            ((App)Application.Current).OnConnectionPage = true;
            connectionSem = new SemaphoreSlim(1, 1);
            localSettings = ApplicationData.Current.LocalSettings;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            resultsListView.ItemsSource = _resultCollection;

            if (e == null)
            {
                lastNavTag = null;
            }
            else
            {
                lastNavTag = e.Parameter as string;
            }

            if (localSettings.Values.ContainsKey("lastPort"))
            {
                PortTextBox.Text = (string)localSettings.Values["lastPort"];
            }

            if (localSettings.Values.ContainsKey("lastServer"))
            {
                ServerNameTextBox.Text = (string)localSettings.Values["lastServer"];
            }

            if (localSettings.Values.ContainsKey("lastHash"))
            {
                CertHashTextBox.Text = (string)localSettings.Values["lastHash"];
            }

            if (localSettings.Values.ContainsKey("lastIp"))
            {
                IpTextBox.Text = (string)localSettings.Values["lastIp"];
                ConnectButton.IsEnabled = true;
            }

            Task.Run(async () =>
            {
                // Attempt to connect to localhost every 2 seconds in a background task
                while (!((App)Application.Current).Client.IsConnected)
                {
                    await connectionSem.WaitAsync();
                    try
                    {
                        // Ensure we are not connected, user connection might have succeeded while waiting for semaphore
                        if (!((App)Application.Current).Client.IsConnected)
                        {
                            if (((App)Application.Current).Client.IpAddress != IPAddress.Loopback)
                            {
                                // User connection attempted and failed. Recreate default loopback Client.
                                ((App)Application.Current).Client = new FactoryOrchestratorUWPClient(IPAddress.Loopback);
                                ((App)Application.Current).Client.OnConnected += ((App)Application.Current).OnIpcConnected;
                            }

                            if (await ((App)Application.Current).Client.TryConnect(((App)Application.Current).IgnoreVersionMismatch))
                            {
                                ((App)Application.Current).OnConnectionPage = false;
                                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                                {
                                    this.Frame.Navigate(typeof(MainPage), lastNavTag);
                                });
                            }
                        }
                    }
                    finally
                    {
                        connectionSem.Release();
                    }

                    if (!((App)Application.Current).Client.IsConnected)
                    {
                        if (_firstLocalHostAttempt)
                        {
                            _firstLocalHostAttempt = false;
                            _deviceWatcher = DeviceInformation.CreateWatcher(_aqsQueryString, _propertyKeys, DeviceInformationKind.AssociationEndpointService);
                            _deviceWatcherHelper = new DeviceWatcherHelper(_resultCollection, Dispatcher);
                            _deviceWatcherHelper.StartWatcher(_deviceWatcher);
                        }
                        await Task.Delay(2000);
                    }
                }
            });
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            bool validIp = false;
            bool validPort = false;
            validIp = IPAddress.TryParse(IpTextBox.Text, out var ip);
            validPort = Int32.TryParse(PortTextBox.Text, out int port);
            string serverName = ServerNameTextBox.Text;
            string certHash = CertHashTextBox.Text;

            if (validIp && validPort)
            {
                ConnectButton.IsEnabled = false;
                await connectionSem.WaitAsync();
                try
                {
                    ((App)Application.Current).Client = new FactoryOrchestratorUWPClient(ip, port, serverName, certHash);
                    ((App)Application.Current).Client.OnConnected += ((App)Application.Current).OnIpcConnected;
                    if (await ((App)Application.Current).Client.TryConnect(((App)Application.Current).IgnoreVersionMismatch))
                    {
                        ((App)Application.Current).OnConnectionPage = false;
                        this.Frame.Navigate(typeof(MainPage), lastNavTag);
                        localSettings.Values["lastIp"] = IpTextBox.Text;
                        localSettings.Values["lastPort"] = PortTextBox.Text;
                        localSettings.Values["lastServer"] = ServerNameTextBox.Text;
                        localSettings.Values["lastHash"] = CertHashTextBox.Text;
                    }
                    else
                    {
                        ShowConnectFailure(ip.ToString());
                    }
                }
                finally
                {
                    connectionSem.Release();
                    ConnectButton.IsEnabled = true;
                }
            }
        }

        private async void ShowConnectFailure(string ipStr)
        {
            ContentDialog failedConnectDialog = new ContentDialog
            {
                Title = resourceLoader.GetString("BadIpTitle"),
                Content = string.Format(CultureInfo.CurrentCulture, resourceLoader.GetString("BadIpContent"), ipStr),
                CloseButtonText = resourceLoader.GetString("Ok")
            };

            _ = await failedConnectDialog.ShowAsync();
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(AboutPage));
        }

        List<TextBox> GetAllTextBoxes(DependencyObject parent)
        {
            var list = new List<TextBox>();
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is TextBox)
                    list.Add(child as TextBox);
                list.AddRange(GetAllTextBoxes(child));
            }
            return list;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var allTextBoxes = GetAllTextBoxes(this);
            bool validData = true;
            foreach (TextBox textbox in allTextBoxes)
            {
                if (String.IsNullOrWhiteSpace(textbox.Text))
                {
                    validData = false;
                }
            }
            if (validData)
            {
                ConnectButton.IsEnabled = true;
            }
        }

        private void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.PortTextBox.Visibility == Visibility.Collapsed)
            {
                ToggleVisibility(Visibility.Visible);
                OptionsButton.Content = resourceLoader.GetString("HideAdvancedOptionsButton");
            }
            else
            {
                ToggleVisibility(Visibility.Collapsed);
                OptionsButton.Content = resourceLoader.GetString("AdvancedOptionsButton/Content");
            }
        }

        private void ToggleVisibility(Visibility visibility)
        {
            ServerNameText.Visibility = visibility;
            ServerNameTextBox.Visibility = visibility;
            PortTextBox.Visibility = visibility;
            PortText.Visibility = visibility;
            CertHashText.Visibility = visibility;
            CertHashTextBox.Visibility = visibility;
        }

        private void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (ConnectButton.IsEnabled && e.Key == Windows.System.VirtualKey.Enter)
            {
                ConnectButton_Click(null, null);
            }
        }

        private void ConfirmExit_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).Exit();
        }

        private async void ValidateXMLButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker
            {
                ViewMode = Windows.Storage.Pickers.PickerViewMode.List,
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder
            };
            picker.FileTypeFilter.Add(".xml");
            Windows.Storage.StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile tempXml = await localFolder.CreateFileAsync("temp.xml", CreationCollisionOption.ReplaceExisting);

            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                var path = file.Path;
                await file.CopyAndReplaceAsync(tempXml);

                try
                {
                    FactoryOrchestratorXML.Load(tempXml.Path);

                    ContentDialog successLoadDialog = new ContentDialog
                    {
                        Title = resourceLoader.GetString("XmlValidatedTitle"),
                        Content = string.Format(CultureInfo.CurrentCulture, resourceLoader.GetString("XmlValidatedContent"), path),
                        CloseButtonText = resourceLoader.GetString("Ok")
                    };

                    _ = await successLoadDialog.ShowAsync();
                }
                catch (Exception ex)
                {
                    var msg = ex.AllExceptionsToString().Replace(tempXml.Path, path, StringComparison.OrdinalIgnoreCase);
                    ContentDialog failedLoadDialog = new ContentDialog
                    {
                        Title = resourceLoader.GetString("XmlFailedTitle"),
                        Content = msg,
                        CloseButtonText = resourceLoader.GetString("Ok")
                    };

                    _ = await failedLoadDialog.ShowAsync();
                }
            }
        }

        private string lastNavTag;
        private readonly SemaphoreSlim connectionSem;
        private readonly ApplicationDataContainer localSettings;
        private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();


        /// <summary>
        /// The protocol ID that identifies DNS-SD.
        /// </summary>
        private const string PROTOCOL_GUID = "{4526e8c1-8aac-4153-9b16-55e86ada0e54}";

        /// <summary>
        /// The host name property.
        /// </summary>
        private const string HOSTNAME_PROPERTY = "System.Devices.Dnssd.HostName";

        /// <summary>
        /// The service name property.
        /// </summary>
        private const string SERVICENAME_PROPERTY = "System.Devices.Dnssd.ServiceName";

        /// <summary>
        /// The instance name property.
        /// </summary>
        private const string INSTANCENAME_PROPERTY = "System.Devices.Dnssd.InstanceName";

        /// <summary>
        /// The IP address property.
        /// </summary>
        private const string IPADDRESS_PROPERTY = "System.Devices.IpAddress";

        /// <summary>
        /// The port number property.
        /// </summary>
        private const string PORTNUMBER_PROPERTY = "System.Devices.Dnssd.PortNumber";

        /// <summary>
        /// The network protocol that will be accepting connections for responses.
        /// </summary>
        private const string NETWORK_PROTOCOL = "_tcp";

        /// <summary>
        /// The domain of the DNS-SD registration.
        /// </summary>
        private const string DOMAIN = "local";

        /// <summary>
        /// All of the properties that will be returned when a DNS-SD instance has been found. 
        /// </summary>
        private string[] _propertyKeys = new String[] {
            HOSTNAME_PROPERTY,
            SERVICENAME_PROPERTY,
            INSTANCENAME_PROPERTY,
            IPADDRESS_PROPERTY,
            PORTNUMBER_PROPERTY
        };

        /// <summary>
        /// The service type of the DNS-SD registration.
        /// </summary>
        private const string SERVICE_TYPE = "_factorch";

        private string _aqsQueryString = $"System.Devices.AepService.ProtocolId:={PROTOCOL_GUID} AND System.Devices.Dnssd.Domain:=\"{DOMAIN}\" AND System.Devices.Dnssd.ServiceName:=\"{SERVICE_TYPE}.{NETWORK_PROTOCOL}\"";

        private ObservableCollection<DeviceInformationDisplay> _resultCollection = new ObservableCollection<DeviceInformationDisplay>();
        private DeviceWatcherHelper _deviceWatcherHelper;
        private DeviceWatcher _deviceWatcher;
        private bool _firstLocalHostAttempt = true;

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls
        private DeviceWatcher _watcher;

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    connectionSem?.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }

    public class DeviceInformationDisplay : INotifyPropertyChanged
    {
        public DeviceInformationDisplay(DeviceInformation deviceInfoIn)
        {
            DeviceInformation = deviceInfoIn;
        }

        public string Name => DeviceInformation.Name;
        public string Id => DeviceInformation.Id;
        public IReadOnlyDictionary<string, object> Properties => DeviceInformation.Properties;
        public DeviceInformation DeviceInformation { get; private set; }

        public void Update(DeviceInformationUpdate deviceInfoUpdate)
        {
            DeviceInformation.Update(deviceInfoUpdate);

            OnPropertyChanged("Name");
            OnPropertyChanged("DeviceInformation");
        }

        public string GetPropertyForDisplay(string key) => Properties[key]?.ToString();

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// Updates an ObservableCollection based on events from a DeviceWatcher.
    /// </summary>
    /// <remarks>
    /// Encapsulates the work necessary to register for watcher events,
    /// start and stop the watcher, handle race conditions, and break cycles.
    /// </remarks>
    class DeviceWatcherHelper
    {
        public DeviceWatcherHelper(
            ObservableCollection<DeviceInformationDisplay> resultCollection,
            CoreDispatcher dispatcher)
        {
            this.resultCollection = resultCollection;
            this.dispatcher = dispatcher;
        }

        public delegate void DeviceChangedHandler(DeviceWatcher deviceWatcher, string id);
        public event DeviceChangedHandler DeviceChanged;

        public DeviceWatcher DeviceWatcher => deviceWatcher;
        public bool UpdateStatus = true;

        public void StartWatcher(DeviceWatcher deviceWatcher)
        {
            this.deviceWatcher = deviceWatcher;

            // Connect events to update our collection as the watcher report results.
            deviceWatcher.Added += Watcher_DeviceAdded;
            deviceWatcher.Updated += Watcher_DeviceUpdated;
            deviceWatcher.Removed += Watcher_DeviceRemoved;
            deviceWatcher.Start();
        }

        public void StopWatcher()
        {
            // Since the device watcher runs in the background, it is possible that
            // a notification is "in flight" at the time we stop the watcher.
            // In other words, it is possible for the watcher to become stopped while a
            // handler is running, or for a handler to run after the watcher has stopped.

            if (IsWatcherStarted(deviceWatcher))
            {
                // We do not null out the deviceWatcher yet because we want to receive
                // the Stopped event.
                deviceWatcher.Stop();
            }
        }

        public void Reset()
        {
            if (deviceWatcher != null)
            {
                StopWatcher();
                deviceWatcher = null;
            }
        }

        DeviceWatcher deviceWatcher;
        ObservableCollection<DeviceInformationDisplay> resultCollection;
        CoreDispatcher dispatcher;

        static bool IsWatcherStarted(DeviceWatcher watcher)
        {
            return (watcher.Status == DeviceWatcherStatus.Started) ||
                (watcher.Status == DeviceWatcherStatus.EnumerationCompleted);
        }

        public bool IsWatcherRunning()
        {
            if (deviceWatcher == null)
            {
                return false;
            }

            DeviceWatcherStatus status = deviceWatcher.Status;
            return (status == DeviceWatcherStatus.Started) ||
                (status == DeviceWatcherStatus.EnumerationCompleted) ||
                (status == DeviceWatcherStatus.Stopping);
        }

        private async void Watcher_DeviceAdded(DeviceWatcher sender, DeviceInformation deviceInfo)
        {
            // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
            await dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                // Watcher may have stopped while we were waiting for our chance to run.
                if (IsWatcherStarted(sender))
                {
                    resultCollection.Add(new DeviceInformationDisplay(deviceInfo));
                }
            });
        }

        private async void Watcher_DeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
        {
            // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
            await dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                // Watcher may have stopped while we were waiting for our chance to run.
                if (IsWatcherStarted(sender))
                {
                    // Find the corresponding updated DeviceInformation in the collection and pass the update object
                    // to the Update method of the existing DeviceInformation. This automatically updates the object
                    // for us.
                    foreach (DeviceInformationDisplay deviceInfoDisp in resultCollection)
                    {
                        if (deviceInfoDisp.Id == deviceInfoUpdate.Id)
                        {
                            deviceInfoDisp.Update(deviceInfoUpdate);
                            break;
                        }
                    }
                }
            });
        }

        private async void Watcher_DeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
        {
            // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
            await dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                // Watcher may have stopped while we were waiting for our chance to run.
                if (IsWatcherStarted(sender))
                {
                    // Find the corresponding DeviceInformation in the collection and remove it
                    foreach (DeviceInformationDisplay deviceInfoDisp in resultCollection)
                    {
                        if (deviceInfoDisp.Id == deviceInfoUpdate.Id)
                        {
                            resultCollection.Remove(deviceInfoDisp);
                            break;
                        }
                    }
                }
            });
        }
    }
}
