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
            _outSem = new SemaphoreSlim(1, 1);
            newCmd = false;
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
                await IPCClientHelper.IpcClient.InvokeAsync(x => x.AbortTestRun(_activeCmdTestRun.Guid));
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
                    FontWeight = Windows.UI.Text.FontWeights.Bold,
                    IsTextSelectionEnabled = true
                };
                OutputStack.Children.Add(textBlock);
            });

            // Execute command
            newCmd = true;
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

                _outSem.Wait();
                while (lastOutput != _activeCmdTestRun.TestOutput.Count)
                {
                    var blocks = PrepareOutput();
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        UpdateOutput(blocks);
                    });
                }
                _outSem.Release();

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    if (_activeCmdTestRun.TestRunComplete)
                    {
                        // Allow new commands to run
                        CommandBox.IsEnabled = true;
                        RunButtonIcon.Symbol = Symbol.Play;
                        _cmdSem.Release();
                    }
                });
            }
        }


        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            _outSem.Wait();
            OutputStack.Children.Clear();
            _outSem.Release();
        }


        /// <summary>
        /// Updates UI with latest console output
        /// </summary>
        private List<(string text, bool isError)> PrepareOutput()
        {
            List <(string text, bool isError)> ret = new List<(string text, bool isError)>();

            if (newCmd)
            {
                lastOutput = 0;
                newCmd = false;
            }


            var endCount = Math.Min(_activeCmdTestRun.TestOutput.Count, lastOutput + 500);
            string text = "";
            bool errorBlock = false;

            for (int i = lastOutput; i < endCount; i++)
            {
                if (_activeCmdTestRun.TestOutput[i] != null)
                {
                    if (errorBlock && _activeCmdTestRun.TestOutput[i].StartsWith("ERROR: "))
                    {
                        // Append error text
                        text += _activeCmdTestRun.TestOutput[i];
                        errorBlock = true;
                    }
                    else if (errorBlock)
                    {
                        // Done with error text, write out the error text and start again
                        var tupl = (text, true);
                        ret.Add(tupl);

                        text = _activeCmdTestRun.TestOutput[i];
                        errorBlock = false;
                    }
                    else if (!errorBlock && _activeCmdTestRun.TestOutput[i].StartsWith("ERROR: "))
                    {
                        // Done with normal text, write out the normal text and start again
                        var tupl = (text, false);
                        ret.Add(tupl);

                        text = _activeCmdTestRun.TestOutput[i];
                        errorBlock = true;
                    }
                    else
                    {
                        // Append normal text
                        text += _activeCmdTestRun.TestOutput[i];
                        errorBlock = false;
                    }
                }

                if (i != (endCount - 1))
                {
                    text += System.Environment.NewLine;
                }
            }

            lastOutput = endCount;

            if (!String.IsNullOrEmpty(text))
            {
                if (errorBlock)
                {
                    var tupl = (text, true);
                    ret.Add(tupl);
                }
                else
                {
                    var tupl = (text, false);
                    ret.Add(tupl);
                }
            }

            return ret;
        }

        /// <summary>
        /// Updates UI with latest console output
        /// </summary>
        private void UpdateOutput(List<(string text, bool isError)> blocks)
        {
            foreach (var block in blocks)
            {
                var textBlock = new TextBlock()
                {
                    Text = block.text,
                    IsTextSelectionEnabled = true
                };

                if (block.isError)
                {
                    textBlock.FontWeight = Windows.UI.Text.FontWeights.Bold;
                    textBlock.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);
                }

                if (OutputStack.Children.Count >= maxBlocks)
                {
                    OutputStack.Children.RemoveAt(0);
                }
                OutputStack.Children.Add(textBlock);
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
        private bool newCmd;
        private int lastOutput;
        private int maxBlocks = 400;
        private FTFPoller _testRunPoller;
        private SemaphoreSlim _cmdSem;
        private SemaphoreSlim _outSem;
    }
}
