// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.FactoryOrchestrator.Core;
using System;
using System.Net;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Storage;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using System.Threading;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Microsoft.FactoryOrchestrator.UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ConnectionPage : Page
    {
        public ConnectionPage()
        {
            this.InitializeComponent();
            ((App)Application.Current).Client = new FactoryOrchestratorUWPClient(IPAddress.Loopback, 45684);
            ((App)Application.Current).Client.OnConnected += ((App)Application.Current).OnIpcConnected;
            ((App)Application.Current).OnConnectionPage = true;
            connectionSem = new SemaphoreSlim(1, 1);
        }
        
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            lastNavTag = e.Parameter as string;
            Task.Run(async ()  =>
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
                                // User connection attempted and failed. Recreate Client
                                ((App)Application.Current).Client = new FactoryOrchestratorUWPClient(IPAddress.Loopback, 45684);
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
                        await Task.Delay(2000);
                    }
                }
            });
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            IPAddress ip = null;
            bool validIp = false;

            validIp = IPAddress.TryParse(IpTextBox.Text, out ip);

            if (validIp)
            {
                ConnectButton.IsEnabled = false;
                await connectionSem.WaitAsync();
                try
                {
                    ((App)Application.Current).Client = new FactoryOrchestratorUWPClient(ip, 45684);
                    ((App)Application.Current).Client.OnConnected += ((App)Application.Current).OnIpcConnected;
                    if (await ((App)Application.Current).Client.TryConnect(((App)Application.Current).IgnoreVersionMismatch))
                    {
                        ((App)Application.Current).OnConnectionPage = false;
                        this.Frame.Navigate(typeof(MainPage), lastNavTag);
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
                Title = "Unable to connect to target IP",
                Content = $"Could not connect to {ipStr}.\n\nCheck that the IP address is correct and that the Factory Orchestrator Service is running on the target IP.",
                CloseButtonText = "Ok"
            };

            _ = await failedConnectDialog.ShowAsync();
        }

        private void IpTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(!string.IsNullOrWhiteSpace(IpTextBox.Text))
            {
                ConnectButton.IsEnabled = true;
            }
        }

        private void IpTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                if (!String.IsNullOrWhiteSpace(IpTextBox.Text))
                {
                    ConnectButton_Click(null, null);
                }
            }
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(AboutPage));
        }

        private void ConfirmExit_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).Exit();
        }

        private async void ValidateXMLButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
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
                        Title = "FactoryOrchestratorXML successfully validated!",
                        Content = $"{path} is valid FactoryOrchestratorXML.",
                        CloseButtonText = "Ok"
                    };

                    _ = await successLoadDialog.ShowAsync();
                }
                catch (Exception ex)
                {
                    var msg = ex.AllExceptionsToString().Replace(tempXml.Path, path);
                    ContentDialog failedLoadDialog = new ContentDialog
                    {
                        Title = "FactoryOrchestratorXML failed validation!",
                        Content = $"{msg}",
                        CloseButtonText = "Ok"
                    };

                    _ = await failedLoadDialog.ShowAsync();
                }
            }
        }

        private string lastNavTag;
        private SemaphoreSlim connectionSem;
    }
}
