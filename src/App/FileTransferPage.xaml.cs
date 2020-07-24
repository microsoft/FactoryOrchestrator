// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.FactoryOrchestrator.Client;
using System;
using System.Diagnostics;
using System.IO;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Microsoft.FactoryOrchestrator.UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FileTransferPage : Page
    {
        public FileTransferPage()
        {
            this.InitializeComponent();
        }

        private void GetServerFileButton_Click(object sender, RoutedEventArgs e)
        {
            if ((!string.IsNullOrWhiteSpace(ServerFileTextBox.Text)) && (!string.IsNullOrWhiteSpace(ClientFileTextBox.Text)))
            {
                ClientFileTextBox.Text = ClientFileTextBox.Text.TrimStart(new char[] { '"' }).TrimEnd(new char[] { '"' });
                ServerFileTextBox.Text = ServerFileTextBox.Text.TrimStart(new char[] { '"' }).TrimEnd(new char[] { '"' });

                var button = sender as Button;
                HeaderGet.Visibility = Visibility.Visible;
                HeaderSend.Visibility = Visibility.Collapsed;
                SourceFileHeaderGet.Visibility = Visibility.Visible;
                SourceFileHeaderSend.Visibility = Visibility.Collapsed;
                TargetFileHeaderGet.Visibility = Visibility.Visible;
                TargetFileHeaderSend.Visibility = Visibility.Collapsed;
                SourceFileBody.Text = ServerFileTextBox.Text;
                TargetFileBody.Text = ClientFileTextBox.Text;

                sending = false;

                Flyout.ShowAttachedFlyout(button);
            }
        }

        private void SendClientFileButton_Click(object sender, RoutedEventArgs e)
        {
            if ((!string.IsNullOrWhiteSpace(ServerFileTextBox.Text)) && (!string.IsNullOrWhiteSpace(ClientFileTextBox.Text)))
            {
                ClientFileTextBox.Text = ClientFileTextBox.Text.TrimStart(new char[] { '"' }).TrimEnd(new char[] { '"' });
                ServerFileTextBox.Text = ServerFileTextBox.Text.TrimStart(new char[] { '"' }).TrimEnd(new char[] { '"' });
                
                var button = sender as Button;
                HeaderGet.Visibility = Visibility.Collapsed;
                HeaderSend.Visibility = Visibility.Visible;
                SourceFileHeaderGet.Visibility = Visibility.Collapsed;
                SourceFileHeaderSend.Visibility = Visibility.Visible;
                TargetFileHeaderGet.Visibility = Visibility.Collapsed;
                TargetFileHeaderSend.Visibility = Visibility.Visible;
                SourceFileBody.Text = ClientFileTextBox.Text;
                TargetFileBody.Text = ServerFileTextBox.Text;

                sending = true;

                Flyout.ShowAttachedFlyout(button);
            }
        }

        private void CancelCopy_Click(object sender, RoutedEventArgs e)
        {
            ConfirmTransferFlyout.Hide();
        }

        private async void ConfirmCopy_Click(object sender, RoutedEventArgs e)
        {
            // todo: quality: transfer file in chunks with progress bar & cancel option
            ClientFileTextBox.IsEnabled = false;
            ServerFileTextBox.IsEnabled = false;
            SendClientFileButton.IsEnabled = false;
            GetServerFileButton.IsEnabled = false;
            ConfirmTransferFlyout.Hide();
            TranferRing.IsActive = true;
            Stopwatch s = new Stopwatch();

            try
            {
                s.Start();

                if (sending)
                {
                    bool isFile = true;
                    try
                    {
                        await StorageFile.GetFileFromPathAsync(Environment.ExpandEnvironmentVariables(ClientFileTextBox.Text));
                    }
                    catch (Exception)
                    {
                        isFile = false;
                    }

                    if (isFile)
                    {
                        await Client.SendFileToDevice(ClientFileTextBox.Text, ServerFileTextBox.Text);
                    }
                    else
                    {
                        await Client.SendDirectoryToDevice(ClientFileTextBox.Text, ServerFileTextBox.Text);
                    }
                }
                else
                {
                    try
                    {
                        await Client.GetFileFromDevice(ServerFileTextBox.Text, ClientFileTextBox.Text);
                    }
                    catch (FileNotFoundException)
                    {                        
                        await Client.GetDirectoryFromDevice(ServerFileTextBox.Text, ClientFileTextBox.Text);
                    }
                }

                s.Stop();

                ContentDialog errorDialog = new ContentDialog
                {
                    Title = "Transfer Succeeded!",
                    Content = $"Transfer completed in {s.Elapsed}",
                    CloseButtonText = "Ok"
                };

                _ = await errorDialog.ShowAsync();
            }
            catch (Exception ex)
            {
                ContentDialog errorDialog = new ContentDialog
                {
                    Title = "Transfer Error!",
                    Content = $"{ex.Message}",
                    CloseButtonText = "Ok"
                };

                _ = await errorDialog.ShowAsync();
            }

            ClientFileTextBox.IsEnabled = true;
            ServerFileTextBox.IsEnabled = true;
            SendClientFileButton.IsEnabled = true;
            GetServerFileButton.IsEnabled = true;
            TranferRing.IsActive = false;
        }

        private bool sending;
        private FactoryOrchestratorUWPClient Client = ((App)Application.Current).Client;
    }
}
