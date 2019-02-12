using FTFInterfaces;
using FTFTestExecution;
using JKang.IpcServiceFramework;
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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace FTFUWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        IpcServiceClient<IFTFCommunication> client;

        public MainPage()
        {
            this.InitializeComponent();

            client = new IpcServiceClientBuilder<IFTFCommunication>()
                .UseTcp(IPAddress.Loopback, 45684)
                .Build();
        }

        private async void Button_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            this.result.Visibility = Visibility.Collapsed;
            bool result = false;
            if (client != null)
            {
                try
                {

                    var tests = await client.InvokeAsync(x => x.CreateTestListFromDirectory("c:\\data\\tests\\", false));
                    abc.Text = DateTime.Now.ToString();
                }
                catch (Exception ex)
                {
                    ex.ToString();
                }
            }

            this.result.IsChecked = result;
            this.result.Visibility = Visibility.Visible;
        }
    }
}
