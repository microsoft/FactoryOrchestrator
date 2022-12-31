// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
// Based on https://github.com/microsoft/Windows-universal-samples/tree/master/Samples/DeviceEnumerationAndPairing
// Based on https://github.com/microsoft/Windows-appsample-networkhelper

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
    /// <summary>
    /// DNS-SD constants for Factory Orchestrator
    /// </summary>
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
        /// The text attributes property.
        /// </summary>
        public const string TextAttributesProperty = "System.Devices.Dnssd.TextAttributes";

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


        /// <summary>
        /// All of the properties that will be returned when a DNS-SD instance has been found. 
        /// </summary>
        public static readonly string[] PropertyKeys = new String[] {
            DnsSdConstants.HostnameProperty,
            DnsSdConstants.ServiceNameProperty,
            DnsSdConstants.InstanceNameProperty,
            DnsSdConstants.IpAddressProperty,
            DnsSdConstants.PortNumberProperty,
            DnsSdConstants.TextAttributesProperty
        };

        /// <summary>
        /// The AQS string to look for Factory Orchestrator devices.
        /// </summary>
        public static readonly string AqsQueryString = $"System.Devices.AepService.ProtocolId:={DnsSdConstants.ProtocolGuid} AND System.Devices.Dnssd.Domain:=\"{DnsSdConstants.Domain}\" AND System.Devices.Dnssd.ServiceName:=\"{DnsSdConstants.Service}.{DnsSdConstants.NetworkProtocol}\"";
    }

    /// <summary>
    /// A small class representing a Factory Orchestrator instance found by Windows.Devices.Enumeration
    /// </summary>
    public class DeviceInformationDisplay : INotifyPropertyChanged
    {
        /// <summary>
        /// Create a DeviceInformationDisplay instance.
        /// </summary>
        /// <param name="deviceInfoIn">DeviceInformation from Windows.Devices.Enumeration</param>
        public DeviceInformationDisplay(DeviceInformation deviceInfoIn)
        {
            DeviceInformation = deviceInfoIn;
        }

        /// <summary>
        /// DNS host name
        /// </summary>
        public string HostName => DeviceInformation.Name;

        /// <summary>
        /// Windows.Devices.Enumeration ID
        /// </summary>
        public string Id => DeviceInformation.Id;

        /// <summary>
        /// string representing the IP addresses of the device.
        /// </summary>
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

        /// <summary>
        /// Windows.Devices.Enumeration properties
        /// </summary>
        public IReadOnlyDictionary<string, object> Properties => DeviceInformation.Properties;

        /// <summary>
        /// Windows.Devices.Enumeration DeviceInformation
        /// </summary>
        public DeviceInformation DeviceInformation { get; private set; }

        /// <summary>
        /// Updates the object with new info from Windows.Devices.Enumeration
        /// </summary>
        /// <param name="deviceInfoUpdate"></param>
        public void Update(DeviceInformationUpdate deviceInfoUpdate)
        {
            DeviceInformation.Update(deviceInfoUpdate);

            OnPropertyChanged("HostName");
            OnPropertyChanged("Id");
            OnPropertyChanged("IpAddressList");
            OnPropertyChanged("DeviceInformation");
        }

        /// <summary>
        /// Event if a property changes
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Notifies about changed property
        /// </summary>
        /// <param name="name">The property name</param>
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// Updates an ObservableCollection based on events from a DeviceWatcher.
    /// </summary>
    /// <remarks>
    /// Encapsulates the work necessary to register for watcher events,
    /// start and stop the watcher, handle race conditions, and break cycles.
    /// </remarks>
    public class FactoryOrchestratorDeviceWatcher
    {
        /// <summary>
        /// Create a new watcher
        /// </summary>
        /// <param name="resultCollection"></param>
        /// <param name="dispatcher"></param>
        public FactoryOrchestratorDeviceWatcher(
            ObservableCollection<DeviceInformationDisplay> resultCollection,
            CoreDispatcher dispatcher)
        {
            this._resultCollection = resultCollection;
            this._dispatcher = dispatcher;
            this._deviceWatcher = DeviceInformation.CreateWatcher(DnsSdConstants.AqsQueryString, DnsSdConstants.PropertyKeys, DeviceInformationKind.AssociationEndpointService);
            // Connect events to update our collection as the watcher report results.
            _deviceWatcher.Added += Watcher_DeviceAdded;
            _deviceWatcher.Updated += Watcher_DeviceUpdated;
            _deviceWatcher.Removed += Watcher_DeviceRemoved;
        }

        /// <summary>
        /// Starts the watcher.
        /// </summary>
        public void StartWatcher()
        {
            _deviceWatcher.Start();
        }

        /// <summary>
        /// Stops the watcher.
        /// </summary>
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

        private readonly DeviceWatcher _deviceWatcher;
        private readonly ObservableCollection<DeviceInformationDisplay> _resultCollection;
        private readonly CoreDispatcher _dispatcher;

        /// <summary>
        /// Returns true if Windows.Devices.Enumeration DeviceWatcher is active
        /// </summary>
        static bool IsWatcherStarted(DeviceWatcher watcher)
        {
            return (watcher.Status == DeviceWatcherStatus.Started) ||
                (watcher.Status == DeviceWatcherStatus.EnumerationCompleted);
        }

        /// <summary>
        /// Returns true if the FactoryOrchestratorDeviceWatcher is active.
        /// </summary>
        /// <returns></returns>
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
