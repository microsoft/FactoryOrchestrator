using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using Microsoft.FactoryTestFramework.Client;
using Microsoft.FactoryTestFramework.Core;
using System.Threading.Tasks;
using System.Threading;

namespace Microsoft.FactoryTestFramework.UWP
{
    /// <summary>
    /// A simple semi-interactive console.
    /// </summary>
    public sealed partial class ConsolePage : Page
    {
        public ConsolePage()
        {
            this.InitializeComponent();
            _cmdSem = new SemaphoreSlim(1, 1);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            BackButton.IsEnabled = this.Frame.CanGoBack;
            base.OnNavigatedTo(e);
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            ExecuteCommand();
        }

        private void CommandBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            // Send command if enter is pressed in command box
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                ExecuteCommand();
            }
        }

        /// <summary>
        /// Runs a command using cmd.exe
        /// </summary>
        private async void ExecuteCommand()
        {
            if (!String.IsNullOrWhiteSpace(CommandBox.Text))
            {
                // Cache the text box value from when the button was pressed
                var newCommand = CommandBox.Text;

                // Prevent another command from running until this one finishes
                _cmdSem.Wait();
                RunButton.IsEnabled = false;
                CommandBox.IsEnabled = false;

                // Execute command
                if (_testRunPoller != null)
                {
                    _testRunPoller.StopPolling();
                }
                _activeCmdTestRun = await IPCClientHelper.IpcClient.InvokeAsync(x => x.RunExecutableOutsideTestList(@"%systemroot%\system32\cmd.exe", $"/C \"{newCommand}\"", null));

                // Log command to console output
                var textBlock = new TextBlock()
                {
                    Text = $"{Environment.NewLine}>{newCommand}{Environment.NewLine}",
                    FontWeight = Windows.UI.Text.FontWeights.Bold,
                    Name = "Command" + _activeCmdTestRun.Guid
                };
                OutputStack.Children.Add(textBlock);

                // Watch for new output
                _testRunPoller = new FTFPoller((Guid)_activeCmdTestRun.Guid, typeof(TestRun), IPCClientHelper.IpcClient, 500);
                _testRunPoller.OnUpdatedObject += OnUpdatedCmdStatusAsync;
                _testRunPoller.StartPolling();
            }
        }

        /// <summary>
        /// Checks if the commands is finished, updates output
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private async void OnUpdatedCmdStatusAsync(object source, FTFPollEventArgs e)
        {
            _activeCmdTestRun = (TestRun)e.Result;

            if (_activeCmdTestRun != null)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    if (_activeCmdTestRun.TestRunComplete)
                    {
                        // The command finished, no need to poll more
                        _testRunPoller.StopPolling();
                        // Allow new commands to run
                        RunButton.IsEnabled = true;
                        CommandBox.IsEnabled = true;
                        _cmdSem.Release();
                    }
                    UpdateOutput();
                });
            }
        }

        /// <summary>
        /// Updates UI with latest console output
        /// </summary>
        private void UpdateOutput()
        {
            var startCount = OutputStack.Children.Count;
            for (int i = startCount; i < _activeCmdTestRun.TestOutput.Count + startCount; i++)
            {
                var line = (i + 1).ToString();

                if (_activeCmdTestRun.TestOutput[i - startCount] != null)
                {
                    var textBlock = new TextBlock()
                    {
                        Text = _activeCmdTestRun.TestOutput[i - startCount],
                        Name = "OuptutForLineNo" + line
                    };

                    if (line.StartsWith("ERROR: "))
                    {
                        textBlock.FontWeight = Windows.UI.Text.FontWeights.Bold;
                        textBlock.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);
                    }

                    OutputStack.Children.Add(textBlock);
                }
            }
        }

        private void OutputStack_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Scroll down when new output is received
            StackPanel stack = (StackPanel)sender;
            ScrollViewer scrollView = (ScrollViewer)stack.Parent;
            scrollView.ChangeView(null, scrollView.ScrollableHeight, null, true);
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            On_BackRequested();
        }

        private bool On_BackRequested()
        {
            if (this.Frame.CanGoBack)
            {
                this.Frame.GoBack();
                return true;
            }
            return false;
        }

        private void BackInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            On_BackRequested();
            args.Handled = true;
        }

        private TestRun _activeCmdTestRun;
        private FTFPoller _testRunPoller;
        private SemaphoreSlim _cmdSem;
    }
}
