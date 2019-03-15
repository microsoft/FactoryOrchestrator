using FTFClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace FTFUWP
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
            }
            else
            {
                IpTextBox.IsEnabled = true;
            }
        }
    }
}
