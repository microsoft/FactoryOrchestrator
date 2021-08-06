// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.FactoryOrchestrator.Client;
using System;
using System.Diagnostics;
using System.IO;
using Windows.ApplicationModel.Resources;
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

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (((App)Application.Current).IsContainerDisabled)
            {
                ContainerCheckBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                ContainerCheckBox.Visibility = Visibility.Visible;
                if (Client.IsLocalHost)
                {
                    ContainerCheckBox.IsChecked = true;
                    ContainerCheckBox.IsEnabled = false;
                }
                else if (((App)Application.Current).IsContainerRunning)
                {
                    ContainerCheckBox.IsChecked = false;
                }
                else
                {
                    ContainerCheckBox.IsChecked = false;
                    ContainerCheckBox.IsEnabled = false;
                }
            }

            ((App)Application.Current).PropertyChanged += FileTransferPage_AppPropertyChanged;
        }

        private async void FileTransferPage_AppPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("IsContainerRunning", StringComparison.Ordinal))
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    if (((App)Application.Current).IsContainerRunning)
                    {
                        ContainerCheckBox.IsEnabled = true;
                    }
                    else
                    {
                        ContainerCheckBox.IsChecked = false;
                        ContainerCheckBox.IsEnabled = false;

                        if (Client.IsLocalHost && this.Frame.CanGoBack)
                        {
                            ContentDialog errorDialog = new ContentDialog
                            {
                                Title = resourceLoader.GetString("NoContainerTitle"),
                                Content = resourceLoader.GetString("NoContainerContent"),
                                CloseButtonText = resourceLoader.GetString("Ok")
                            };

                            await errorDialog.ShowAsync();

                            this.Frame.GoBack();
                        }
                    }
                });
            }
        }

        private void ClientServerFile_TextChanged(Object sender, TextChangedEventArgs e)
        {
            if ((!string.IsNullOrWhiteSpace(ServerFileTextBox.Text)) && (!string.IsNullOrWhiteSpace(ClientFileTextBox.Text)))
            {
                GetServerFileButton.IsEnabled = true;
                SendClientFileButton.IsEnabled = true;
            }
            else
            {
                GetServerFileButton.IsEnabled = false;
                SendClientFileButton.IsEnabled = false;
            }
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
                        await Client.SendFileToDevice(ClientFileTextBox.Text, ServerFileTextBox.Text, (bool)ContainerCheckBox.IsChecked);
                    }
                    else
                    {
                        await Client.SendDirectoryToDevice(ClientFileTextBox.Text, ServerFileTextBox.Text, (bool)ContainerCheckBox.IsChecked);
                    }
                }
                else
                {
                    try
                    {
                        await Client.GetFileFromDevice(ServerFileTextBox.Text, ClientFileTextBox.Text, (bool)ContainerCheckBox.IsChecked);
                    }
                    catch (FileNotFoundException)
                    {                        
                        await Client.GetDirectoryFromDevice(ServerFileTextBox.Text, ClientFileTextBox.Text, (bool)ContainerCheckBox.IsChecked);
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

        private void ContainerCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)ContainerCheckBox.IsChecked)
            {
                ServerText.Text = resourceLoader.GetString("ServerTextContainer/Text");
                GetText.Text = resourceLoader.GetString("GetTextContainer/Text");
                HeaderGet.Text = resourceLoader.GetString("HeaderGetContainer/Text");
                HeaderSend.Text = resourceLoader.GetString("HeaderSendContainer/Text");
            }
            else
            {
                ServerText.Text = resourceLoader.GetString("ServerText/Text");
                GetText.Text = resourceLoader.GetString("GetText/Text");
                HeaderGet.Text = resourceLoader.GetString("HeaderGet/Text");
                HeaderSend.Text = resourceLoader.GetString("HeaderSend/Text");
            }
        }

        private bool sending;
        private FactoryOrchestratorUWPClient Client = ((App)Application.Current).Client;
        private ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();
    }
}
