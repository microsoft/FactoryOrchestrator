using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.UI.Core;

namespace Microsoft.FactoryOrchestrator.UWP
{

    public static class DnsSdConstants
    {

        /// <summary>
        /// The protocol ID that identifies DNS-SD.
        /// </summary>
        public const string ProtocolGuid = "{4526e8c1-8aac-4153-9b16-55e86ada0e54}";

        /// <summary>
        /// The host name property.
        /// </summary>
        public const string HostnameProperty = "System.Devices.Dnssd.HostName";

        /// <summary>
        /// The service name property.
        /// </summary>
        public const string ServiceNameProperty = "System.Devices.Dnssd.ServiceName";

        /// <summary>
        /// The instance name property.
        /// </summary>
        public const string InstanceNameProperty = "System.Devices.Dnssd.InstanceName";

        /// <summary>
        /// The IP address property.
        /// </summary>
        public const string IpAddressProperty = "System.Devices.IpAddress";

        /// <summary>
        /// The port number property.
        /// </summary>
        public const string PortNumberProperty = "System.Devices.Dnssd.PortNumber";

        /// <summary>
        /// The network protocol that will be accepting connections for responses.
        /// </summary>
        public const string NetworkProtocol = "_tcp";

        /// <summary>
        /// The domain of the DNS-SD registration.
        /// </summary>
        public const string Domain = "local";

        /// <summary>
        /// The service type of the DNS-SD registration.
        /// </summary>
        public const string Service = "_factorch";
    }

    public class DeviceInformationDisplay : INotifyPropertyChanged
    {
        public DeviceInformationDisplay(DeviceInformation deviceInfoIn)
        {
            DeviceInformation = deviceInfoIn;
        }

        public string Name => DeviceInformation.Name;
        public string Id => DeviceInformation.Id;
        public string IpAddressList
        {
            get
            {
                var ips = Properties[DnsSdConstants.IpAddressProperty] as String[];
                if (ips == null)
                {
                    return "";
                }

                return string.Join(" ", ips);
            }
        }
        public IReadOnlyDictionary<string, object> Properties => DeviceInformation.Properties;
        public DeviceInformation DeviceInformation { get; private set; }

        public void Update(DeviceInformationUpdate deviceInfoUpdate)
        {
            DeviceInformation.Update(deviceInfoUpdate);

            OnPropertyChanged("Name");
            OnPropertyChanged("Id");
            OnPropertyChanged("IpAddressList");
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
            this._resultCollection = resultCollection;
            this._dispatcher = dispatcher;
        }

        public delegate void DeviceChangedHandler(DeviceWatcher deviceWatcher, string id);
        public event DeviceChangedHandler DeviceChanged;

        public DeviceWatcher DeviceWatcher => _deviceWatcher;
        public bool UpdateStatus = true;

        public void StartWatcher(DeviceWatcher deviceWatcher)
        {
            this._deviceWatcher = deviceWatcher;

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

            if (IsWatcherStarted(_deviceWatcher))
            {
                // We do not null out the deviceWatcher yet because we want to receive
                // the Stopped event.
                _deviceWatcher.Stop();
            }
        }

        public void Reset()
        {
            if (_deviceWatcher != null)
            {
                StopWatcher();
                _deviceWatcher = null;
            }
        }

        DeviceWatcher _deviceWatcher;
        ObservableCollection<DeviceInformationDisplay> _resultCollection;
        CoreDispatcher _dispatcher;

        static bool IsWatcherStarted(DeviceWatcher watcher)
        {
            return (watcher.Status == DeviceWatcherStatus.Started) ||
                (watcher.Status == DeviceWatcherStatus.EnumerationCompleted);
        }

        public bool IsWatcherRunning()
        {
            if (_deviceWatcher == null)
            {
                return false;
            }

            DeviceWatcherStatus status = _deviceWatcher.Status;
            return (status == DeviceWatcherStatus.Started) ||
                (status == DeviceWatcherStatus.EnumerationCompleted) ||
                (status == DeviceWatcherStatus.Stopping);
        }

        private async void Watcher_DeviceAdded(DeviceWatcher sender, DeviceInformation deviceInfo)
        {
            // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
            await _dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                // Watcher may have stopped while we were waiting for our chance to run.
                if (IsWatcherStarted(sender))
                {
                    _resultCollection.Add(new DeviceInformationDisplay(deviceInfo));
                }
            });
        }

        private async void Watcher_DeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
        {
            // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
            await _dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                // Watcher may have stopped while we were waiting for our chance to run.
                if (IsWatcherStarted(sender))
                {
                    // Find the corresponding updated DeviceInformation in the collection and pass the update object
                    // to the Update method of the existing DeviceInformation. This automatically updates the object
                    // for us.
                    foreach (DeviceInformationDisplay deviceInfoDisp in _resultCollection)
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
            await _dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                // Watcher may have stopped while we were waiting for our chance to run.
                if (IsWatcherStarted(sender))
                {
                    // Find the corresponding DeviceInformation in the collection and remove it
                    foreach (DeviceInformationDisplay deviceInfoDisp in _resultCollection)
                    {
                        if (deviceInfoDisp.Id == deviceInfoUpdate.Id)
                        {
                            _resultCollection.Remove(deviceInfoDisp);
                            break;
                        }
                    }
                }
            });
        }
    }
}
