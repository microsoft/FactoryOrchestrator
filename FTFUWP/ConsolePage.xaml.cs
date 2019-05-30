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
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
            _cmdSem = new SemaphoreSlim(1, 1);
            outputLock = new object();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if ((_testRunPoller != null) && (!_activeCmdTestRun.TestRunComplete))
            {
                // Only restart polling if the command is still running.
                _testRunPoller.StartPolling();
            }
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (_testRunPoller != null)
            {
                _testRunPoller.StopPolling();
            }
            base.OnNavigatedFrom(e);
        }

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            if (RunButtonIcon.Symbol == Symbol.Stop)
            {
                _testRunPoller.StopPolling();
                _cmdSem.Release();
                _testRunPoller = null;
                await IPCClientHelper.IpcClient.InvokeAsync(x => x.AbortTestRun(_activeCmdTestRun));
                CommandBox.IsEnabled = true;
                RunButtonIcon.Symbol = Symbol.Play;
            }
            else
            {
                if (!String.IsNullOrWhiteSpace(CommandBox.Text))
                {
                    // Asynchronously run the command
                    await ExecuteCommand(CommandBox.Text);
                }
            }
        }

        private async void CommandBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            // Send command if enter is pressed in command box
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                if (!String.IsNullOrWhiteSpace(CommandBox.Text))
                {
                    // Asynchronously run the command
                    await ExecuteCommand(CommandBox.Text);
                }
            }
        }

        /// <summary>
        /// Runs a command using cmd.exe
        /// </summary>
        private async Task ExecuteCommand(string command)
        {
            // Prevent another command from running until this one finishes
            _cmdSem.Wait();

            // Update UI
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {

                RunButtonIcon.Symbol = Symbol.Stop;
                CommandBox.IsEnabled = false;

                // Log command to console output
                var textBlock = new TextBlock()
                {
                    Text = $"{Environment.NewLine}>{command}{Environment.NewLine}",
                    FontWeight = Windows.UI.Text.FontWeights.Bold
                };
                OutputStack.Children.Add(textBlock);
            });

            // Execute command
            if (_testRunPoller != null)
            {
                _testRunPoller.StopPolling();
            }
            _activeCmdTestRun = await IPCClientHelper.IpcClient.InvokeAsync(x => x.RunExecutableAsBackgroundTask(@"%systemroot%\system32\cmd.exe", $"/C \"{command}\"", null));

            // Watch for new output
            _testRunPoller = new FTFPoller((Guid)_activeCmdTestRun.Guid, typeof(TestRun), IPCClientHelper.IpcClient, 1000);
            _testRunPoller.OnUpdatedObject += OnUpdatedCmdStatusAsync;
            _testRunPoller.StartPolling();
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
                if (_activeCmdTestRun.TestRunComplete)
                {
                    // The command finished, no need to poll more
                    _testRunPoller.StopPolling();
                }
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    if (_activeCmdTestRun.TestRunComplete)
                    {
                        // Allow new commands to run
                        CommandBox.IsEnabled = true;
                        RunButtonIcon.Symbol = Symbol.Play;
                        _cmdSem.Release();
                    }
                    UpdateOutput();
                });
            }
        }


        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            lock (outputLock)
            {
                OutputStack.Children.Clear();
            }
        }

        /// <summary>
        /// Updates UI with latest console output
        /// </summary>
        private void UpdateOutput()
        {
            lock (outputLock)
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
        }

        private void OutputStack_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Scroll down when new output is received
            StackPanel stack = (StackPanel)sender;
            ScrollViewer scrollView = (ScrollViewer)stack.Parent;
            scrollView.ChangeView(null, scrollView.ScrollableHeight, null, true);
        }

        private TestRun _activeCmdTestRun;
        private FTFPoller _testRunPoller;
        private SemaphoreSlim _cmdSem;
        private object outputLock;
    }
}
