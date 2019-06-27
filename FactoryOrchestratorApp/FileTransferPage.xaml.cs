using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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
            if (sending)
            {
                await FactoryOrchestrator.UWP.FileTransferHelper.SendFileToServer(ClientFileTextBox.Text, ServerFileTextBox.Text);
            }
            else
            {
                await FactoryOrchestrator.UWP.FileTransferHelper.GetFileFromServer(ServerFileTextBox.Text, ClientFileTextBox.Text);
            }

            ConfirmTransferFlyout.Hide();
        }

        private bool sending;
    }
}
