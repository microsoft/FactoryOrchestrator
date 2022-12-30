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
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Resources;
using System.Linq;
using Windows.Media.Protection.PlayReady;
using System.Security.Cryptography;
using Newtonsoft.Json;

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
            // Default to loopback on port Constants.DefaultServerPort.
            ((App)Application.Current).Client = new FactoryOrchestratorUWPClient(IPAddress.Loopback);
            ((App)Application.Current).Client.OnConnected += ((App)Application.Current).OnIpcConnected;
            ((App)Application.Current).OnConnectionPage = true;
            connectionSem = new SemaphoreSlim(1, 1);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ResultsListView.ItemsSource = _resultCollection;

            if (e == null)
            {
                lastNavTag = null;
            }
            else
            {
                lastNavTag = e.Parameter as string;
            }

            // Initialize UI
            PortTextBox.Text = Settings.LastPort;
            ServerNameTextBox.Text = Settings.LastServer;
            CertHashTextBox.Text = Settings.LastHash;
            IpTextBox.Text = Settings.LastIp;
            ClientCertTextBox.Text = Settings.LastClientCertPath;
            ClientCertPwTextBox.Password = Settings.LastClientCertPw;

            if (!string.IsNullOrWhiteSpace(Settings.LastClientCertSerialized))
            {
                try
                {
                    byte[] clientCertBytes = JsonConvert.DeserializeObject<byte[]>(Settings.LastClientCertSerialized);
                    _clientCert = new X509Certificate2(clientCertBytes);
                }
                catch (Exception)
                {
                    Settings.LastClientCertPath = "";
                    Settings.LastClientCertPw = "";
                    Settings.LastClientCertSerialized = null;

                    ClientCertTextBox.Text = Settings.LastClientCertPath;
                    ClientCertPwTextBox.Password = Settings.LastClientCertPw;
                }
            }

            this.Loaded += ConnectionPage_Loaded;

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
                            _deviceWatcherHelper = new FactoryOrchestratorDeviceWatcher(_resultCollection, Dispatcher);
                            _deviceWatcherHelper.StartWatcher();
                        }
                        await Task.Delay(2000);
                    }
                }
            });
        }

        private void ConnectionPage_Loaded(object sender, RoutedEventArgs e)
        {
            CheckIfEnableConnectButton();
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            bool validIp = false;
            bool validPort = false;
            validIp = IPAddress.TryParse(IpTextBox.Text, out var ip);
            validPort = Int32.TryParse(PortTextBox.Text, out int port);
            string serverName = ServerNameTextBox.Text;
            string certHash = CertHashTextBox.Text;

            if (!await TrySetClientCertificateAsync(false))
            {
                return;
            }

            if (validIp && validPort)
            {
                ConnectButton.IsEnabled = false;
                await connectionSem.WaitAsync();
                try
                {
                    ((App)Application.Current).Client = new FactoryOrchestratorUWPClient(ip, port, serverName, certHash, _clientCert);
                    ((App)Application.Current).Client.OnConnected += ((App)Application.Current).OnIpcConnected;
                    if (await ((App)Application.Current).Client.TryConnect(((App)Application.Current).IgnoreVersionMismatch))
                    {
                        ((App)Application.Current).OnConnectionPage = false;
                        this.Frame.Navigate(typeof(MainPage), lastNavTag);
                        Settings.LastIp = IpTextBox.Text;
                        Settings.LastPort = PortTextBox.Text;
                        Settings.LastServer = serverName;
                        Settings.LastHash = certHash;
                        AnnounceConnectionSuccess();
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

        // Announce network connection success using a hidden TextBox
        private void AnnounceConnectionSuccess()
        {
            if (AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged))
            {
                NetConnectAnnounce.Visibility = Visibility.Visible;
                var peer = FrameworkElementAutomationPeer.CreatePeerForElement(NetConnectAnnounce);

                if (peer != null)
                {
                    peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
                }
                NetConnectAnnounce.Visibility = Visibility.Collapsed;
            }
        }

        private async void ShowConnectFailure(string ipStr)
        {
            ContentDialog failedConnectDialog = new ContentDialog
            {
                Title = string.Format(CultureInfo.CurrentCulture, resourceLoader.GetString("BadIpTitle"), ipStr),
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
            CheckIfEnableConnectButton();
        }

        private void CheckIfEnableConnectButton()
        {
            // Quick check to ensure every required text box has something in it
            var allTextBoxes = GetAllTextBoxes(this);
            bool validData = true;
            foreach (TextBox textbox in allTextBoxes)
            {
#pragma warning disable CA1307 // Specify StringComparison
                if (textbox.Name.Contains("ClientCert"))
                {
                    continue;
                }
#pragma warning restore CA1307 // Specify StringComparison

                if (String.IsNullOrWhiteSpace(textbox.Text))
                {
                    validData = false;
                }
            }

            ConnectButton.IsEnabled = validData;
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
            ClientCertText.Visibility = visibility;
            ClientCertTextBox.Visibility = visibility;
            ClientCertPwText.Visibility = visibility;
            ClientCertPwTextBox.Visibility = visibility;
            ClientCertSavePwCheckBox.Visibility = visibility;
            ClientCertBrowseButton.Visibility = Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily.ToString().Contains("desktop", StringComparison.InvariantCultureIgnoreCase) ? visibility : Visibility.Collapsed;

            IpText.Visibility = visibility;
            IpTextBox.Visibility = visibility;
            ConnectButton.Visibility = visibility;
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

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            AutomationProperties.SetName(ConfirmExit, "ConfirmExit");
        }
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            ExitFlyout.Hide();
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

        private async void ResultsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Get IP, port, and service certificate from DNS-SD information
            var item = e.ClickedItem as DeviceInformationDisplay;
            var ipStrings = item.Properties[DnsSdConstants.IpAddressProperty] as string[];
            var port = (UInt16)item.Properties[DnsSdConstants.PortNumberProperty];
            var attrs = (string[])item.Properties[DnsSdConstants.TextAttributesProperty];
            string serverIdentity = attrs.Where(x => x.StartsWith("ServerIdentity=", StringComparison.InvariantCultureIgnoreCase)).DefaultIfEmpty(Constants.DefaultServerIdentity).FirstOrDefault().Replace("ServerIdentity=", "", StringComparison.InvariantCultureIgnoreCase);
            string serverHash = attrs.Where(x => x.StartsWith("CertificateHash=", StringComparison.InvariantCultureIgnoreCase)).DefaultIfEmpty(Constants.DefaultServerCertificateHash).FirstOrDefault().Replace("CertificateHash=", "", StringComparison.InvariantCultureIgnoreCase);
            bool clientCertificateRequired = false;
            _ = bool.TryParse(attrs.Where(x => x.StartsWith("ClientCertificateRequired=", StringComparison.InvariantCultureIgnoreCase)).DefaultIfEmpty(Constants.DefaultServerCertificateHash).FirstOrDefault().Replace("ClientCertificateRequired=", "", StringComparison.InvariantCultureIgnoreCase), out clientCertificateRequired);

            if ((ipStrings == null) || !await TrySetClientCertificateAsync(clientCertificateRequired))
            {
                return;
            }

            try
            {
                await connectionSem.WaitAsync().ConfigureAwait(true);
                ConnectButton.IsEnabled = false;

                // Try to connect to every IP address listed
                for (int i = 0; i < ipStrings.Length; i++)
                {
                    string ipString = ipStrings[i];
                    IPAddress ip;
                    if (IPAddress.TryParse(ipString, out ip))
                    {
                        ((App)Application.Current).Client = new FactoryOrchestratorUWPClient(ip, port, serverIdentity, serverHash, _clientCert);
                        ((App)Application.Current).Client.OnConnected += ((App)Application.Current).OnIpcConnected;
                        if (await ((App)Application.Current).Client.TryConnect(((App)Application.Current).IgnoreVersionMismatch).ConfigureAwait(true))
                        {
                            // We were able to connect to an IP!
                            ((App)Application.Current).OnConnectionPage = false;
                            this.Frame.Navigate(typeof(MainPage), lastNavTag);
                            break;
                        }
                    }
                }

                if (!((App)Application.Current).Client.IsConnected)
                {
                    ShowConnectFailure(item.HostName);
                }
                else
                {
                    AnnounceConnectionSuccess();
                }
            }
            finally
            {
                connectionSem.Release();
                ConnectButton.IsEnabled = true;
            }
        }

        private async Task<bool> TrySetClientCertificateAsync(bool ClientCertificateRequired)
        {
            string clientCertPath = Environment.ExpandEnvironmentVariables(ClientCertTextBox.Text);
            string clientCertPw = ClientCertPwTextBox.Password;

            if (!string.IsNullOrWhiteSpace(clientCertPath))
            {
                if ((_clientCert != null) && (clientCertPath.Equals(Settings.LastClientCertPath, StringComparison.OrdinalIgnoreCase)))
                {
                    // We have a cert from settings
                    return true;
                }

                try
                {
                    if ((_clientCertFile == null) || (!string.Equals(_clientCertFile.Path, clientCertPath, StringComparison.OrdinalIgnoreCase)))
                    {
                        // We need to open the cert file manually, requires File System access was granted to our app
                        _clientCertFile = StorageFile.GetFileFromPathAsync(clientCertPath).AsTask().Result;
                    }

                    // Try to open the cert
                    var file = _clientCertFile.OpenStreamForReadAsync().Result;
                    byte[] bytes = new byte[file.Length];
                    file.Read(bytes, 0, (int)file.Length);

                    _clientCert?.Dispose();
                    _clientCert = null;
                    _clientCert = string.IsNullOrWhiteSpace(clientCertPw) ? new X509Certificate2(bytes) : new X509Certificate2(bytes, clientCertPw);

                    // We were able to open the cert. Cache it in settings.
                    // Cache both the cert data and its path. That way, if File System access is not enabled on our app, we can still reload this cert without requiring the user to browse to it again manually.
                    Settings.LastClientCertPath = clientCertPath;

                    if ((bool)ClientCertSavePwCheckBox.IsChecked)
                    {
                        Settings.LastClientCertPw = clientCertPw;
                    }
                    else if (string.IsNullOrWhiteSpace(clientCertPw))
                    {
                        Settings.LastClientCertSerialized = JsonConvert.SerializeObject(_clientCert.GetRawCertData());
                    }

                    return true;
                }
                catch (Exception)
                {
                    ContentDialog failedCertDialog = new ContentDialog
                    {
                        Title = resourceLoader.GetString("BadCertTitle"),
                        Content = string.Format(CultureInfo.CurrentCulture, resourceLoader.GetString("BadCertContent"), clientCertPath),
                        CloseButtonText = resourceLoader.GetString("Ok")
                    };
                    _ = await failedCertDialog.ShowAsync();

                    return false;
                }
            }
            else
            {
                // User cleared text box, remove the cert if we have one saved
                _clientCert?.Dispose();
                _clientCert = null;
                Settings.LastClientCertPath = "";
                Settings.LastClientCertPw = "";
                Settings.LastClientCertSerialized = null;

                if (ClientCertificateRequired)
                {
                    ContentDialog certRequiredDialog = new ContentDialog
                    {
                        Title = resourceLoader.GetString("CertRequiredTitle"),
                        Content = resourceLoader.GetString("CertRequiredContent"),
                        CloseButtonText = resourceLoader.GetString("Ok")
                    };
                    _ = await certRequiredDialog.ShowAsync();

                    return false;
                }
            }

            return true;
        }

        private async void ClientCertBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
            picker.FileTypeFilter.Add(".pfx");
            var storageFile = await picker.PickSingleFileAsync();

            if (storageFile != null)
            {
                _clientCertFile = storageFile;
                ClientCertTextBox.Text = storageFile.Path;
            }
        }

        private void ServerNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ServerNameTextBox.Text))
            {
                Settings.LastServer = null;
                ServerNameTextBox.Text = Settings.LastServer;
            }
        }

        private void CertHashTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(CertHashTextBox.Text))
            {
                Settings.LastHash = null;
                CertHashTextBox.Text = Settings.LastHash;
            }
        }

        private string lastNavTag;
        private readonly SemaphoreSlim connectionSem;
        private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();

        private ObservableCollection<DeviceInformationDisplay> _resultCollection = new ObservableCollection<DeviceInformationDisplay>();
        private FactoryOrchestratorDeviceWatcher _deviceWatcherHelper;
        private bool _firstLocalHostAttempt = true;
        private X509Certificate2 _clientCert = null;
        private StorageFile _clientCertFile = null;

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    connectionSem?.Dispose();
                    _clientCert?.Dispose();
                    _clientCert = null;
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
}
