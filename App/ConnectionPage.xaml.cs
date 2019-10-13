using Microsoft.FactoryOrchestrator.Core;
using Microsoft.FactoryOrchestrator.Client;
using System;
using System.Net;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using TaskStatus = Microsoft.FactoryOrchestrator.Core.TaskStatus;

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
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            IPAddress ip = null;
            bool validIp = false;

            if ((bool)LocalDeviceCheckBox.IsChecked)
            {
                ip = IPAddress.Loopback;
                validIp = true;
            }
            else
            {
                validIp = IPAddress.TryParse(IpTextBox.Text, out ip);
            }

            if (validIp)
            {
                ((App)Application.Current).Client = new FactoryOrchestratorUWPClient(ip, 45684);
                if (await ((App)Application.Current).Client.TryConnect())
                {
                    this.Frame.Navigate(typeof(MainPage));
                }
                else
                {
                    var ipStr = ((App)Application.Current).Client.IpAddress.ToString();
                    ((App)Application.Current).Client = null;
                    ShowConnectFailure(ipStr);
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

            ContentDialogResult r = await failedConnectDialog.ShowAsync();
        }

        private void LocalDeviceCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)LocalDeviceCheckBox.IsChecked)
            {
                IpTextBox.IsEnabled = false;
                ConnectButton.IsEnabled = true;
            }
            else
            {
                IpTextBox.IsEnabled = true;
            }
        }

        private void IpTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(!string.IsNullOrWhiteSpace(IpTextBox.Text))
            {
                ConnectButton.IsEnabled = true;
            }
            else if (!(bool)LocalDeviceCheckBox.IsChecked)
            {
                ConnectButton.IsEnabled = false;
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
    }
}
