using Microsoft.FactoryTestFramework.Client;
using System.Net;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Microsoft.FactoryTestFramework.UWP
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

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
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
                IPCClientHelper.StartIPCConnection(ip, 45684);
                this.Frame.Navigate(typeof(MainPage));
            }
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
    }
}
