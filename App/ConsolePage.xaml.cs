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
using Microsoft.FactoryOrchestrator.Client;
using Microsoft.FactoryOrchestrator.Core;
using System.Threading.Tasks;
using System.Threading;
using TaskStatus = Microsoft.FactoryOrchestrator.Core.TaskStatus;

namespace Microsoft.FactoryOrchestrator.UWP
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
            _newCmd = false;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Client = ((App)Application.Current).Client;
            if ((_taskRunPoller != null) && (!_activeCmdTaskRun.TaskRunComplete))
            {
                // Only restart polling if the command is still running.
                _taskRunPoller.StartPolling(Client);
            }
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (_taskRunPoller != null)
            {
                _taskRunPoller.StopPolling();
            }
            base.OnNavigatedFrom(e);
        }

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            _cmdSem.Wait();

            if (RunButtonIcon.Symbol == Symbol.Stop)
            {
                _taskRunPoller.StopPolling();
                _taskRunPoller = null;
                await Client.AbortTaskRun(_activeCmdTaskRun.Guid);
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

            _cmdSem.Release();
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
            _newCmd = true;
            if (_taskRunPoller != null)
            {
                _taskRunPoller.StopPolling();
            }
            _activeCmdTaskRun = await Client.RunExecutable(@"cmd.exe", $"/C \"{command}\"", null);

            // Watch for new output
            _taskRunPoller = new ServerPoller((Guid)_activeCmdTaskRun.Guid, typeof(TaskRun), 1000);
            _taskRunPoller.OnUpdatedObject += OnUpdatedCmdStatusAsync;
            _taskRunPoller.OnException += ((App)Application.Current).OnServerPollerException;
            _taskRunPoller.StartPolling(Client);
        }

        /// <summary>
        /// Checks if the commands is finished, updates output
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private async void OnUpdatedCmdStatusAsync(object source, ServerPollerEventArgs e)
        {
            _activeCmdTaskRun = (TaskRun)e.Result;

            if (_activeCmdTaskRun != null)
            {
                if (_activeCmdTaskRun.TaskRunComplete)
                {
                    // The command finished, no need to poll more
                    _taskRunPoller.StopPolling();
                }

                while (_lastOutput != _activeCmdTaskRun.TaskOutput.Count)
                {
                    var blocks = PrepareOutput();
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        _outSem.Wait();
                        UpdateOutput(blocks);
                        _outSem.Release();
                    });
                }

                if (_activeCmdTaskRun.TaskRunComplete)
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        // Allow new commands to run
                        CommandBox.IsEnabled = true;
                        RunButtonIcon.Symbol = Symbol.Play;
                    });
                }
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

            if (_newCmd)
            {
                _lastOutput = 0;
                _newCmd = false;
            }


            var endCount = Math.Min(_activeCmdTaskRun.TaskOutput.Count, _lastOutput + _maxLinesPerBlock);
            string text = "";
            bool errorBlock = false;

            for (int i = _lastOutput; i < endCount; i++)
            {
                if (_activeCmdTaskRun.TaskOutput[i] != null)
                {
                    if (errorBlock && _activeCmdTaskRun.TaskOutput[i].StartsWith("ERROR: "))
                    {
                        // Append error text
                        text += _activeCmdTaskRun.TaskOutput[i];
                        errorBlock = true;
                    }
                    else if (errorBlock)
                    {
                        // Done with error text, write out the error text and start again
                        var tupl = (text, true);
                        ret.Add(tupl);

                        text = _activeCmdTaskRun.TaskOutput[i];
                        errorBlock = false;
                    }
                    else if (!errorBlock && _activeCmdTaskRun.TaskOutput[i].StartsWith("ERROR: "))
                    {
                        // Done with normal text, write out the normal text and start again
                        var tupl = (text, false);
                        ret.Add(tupl);

                        text = _activeCmdTaskRun.TaskOutput[i];
                        errorBlock = true;
                    }
                    else
                    {
                        // Append normal text
                        text += _activeCmdTaskRun.TaskOutput[i];
                        errorBlock = false;
                    }
                }

                if (i != (endCount - 1))
                {
                    text += System.Environment.NewLine;
                }
            }

            _lastOutput = endCount;

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

                if (OutputStack.Children.Count >= _maxBlocks)
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

        private TaskRun _activeCmdTaskRun;
        private bool _newCmd;
        private int _lastOutput;
        private ServerPoller _taskRunPoller;
        private SemaphoreSlim _cmdSem;
        private SemaphoreSlim _outSem;
        private FactoryOrchestratorUWPClient Client = ((App)Application.Current).Client;

        private const int _maxBlocks = 10; // @500 lines per block this is 5000 lines or 10 commands maximum
        private const int _maxLinesPerBlock = 500;
    }
}
