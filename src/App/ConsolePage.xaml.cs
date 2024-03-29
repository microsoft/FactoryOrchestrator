﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.FactoryOrchestrator.Client;
using Microsoft.FactoryOrchestrator.Core;
using System.Threading.Tasks;
using System.Threading;
using TaskStatus = Microsoft.FactoryOrchestrator.Core.TaskStatus;
using Windows.UI.Core;

namespace Microsoft.FactoryOrchestrator.UWP
{
    /// <summary>
    /// A simple semi-interactive console.
    /// </summary>
    public sealed partial class ConsolePage : Page, IDisposable
    {
        public ConsolePage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
            _cmdSem = new SemaphoreSlim(1, 1);
            _outSem = new SemaphoreSlim(1, 1);
            _activeRunSem = new SemaphoreSlim(1, 1);
            _newCmd = false;
            _cmdHistory = new List<string>(20);
            _cmdHistoryIndex = 0;
            // Use a custom routed event handler to ensure we see ALL key events,
            // without this, some are silently handled by the UI framework
            CommandBox.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(CommandBox_KeyDown), true);
            ((App)Application.Current).PropertyChanged += ConsolePage_AppPropertyChanged;
        }

        private async void ConsolePage_AppPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("IsContainerRunning", StringComparison.Ordinal))
            {
                if (((App)Application.Current).IsContainerRunning)
                {
                    _containerIp = (await Client.GetContainerIpAddresses().ConfigureAwait(false))[0];
                }

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    if (((App)Application.Current).IsContainerRunning)
                    {
                        ContainerCheckBox.IsEnabled = true;
                    }
                    else
                    {
                        ContainerCheckBox.IsEnabled = false;
                        ContainerCheckBox.IsChecked = false;
                    }
                });
            }
            else if (e.PropertyName.Equals("IsContainerDisabled", StringComparison.Ordinal))
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    if (((App)Application.Current).IsContainerDisabled)
                    {
                        ContainerCheckBox.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        ContainerCheckBox.Visibility = Visibility.Visible;
                    }
                });
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            Client = ((App)Application.Current).Client;

            if ((_taskRunPoller != null) && (!_activeCmdTaskRun.TaskRunComplete))
            {
                // Only restart polling if the command is still running.
                _taskRunPoller.StartPolling(Client);
            }

            if (((App)Application.Current).IsContainerDisabled)
            {
                ContainerCheckBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                ContainerCheckBox.Visibility = Visibility.Visible;
                if (((App)Application.Current).IsContainerRunning)
                {
                    ContainerCheckBox.IsEnabled = true;
                    _containerIp = (await Client.GetContainerIpAddresses())[0];
                }
                else
                {
                    ContainerCheckBox.IsEnabled = false;
                    ContainerCheckBox.IsChecked = false;
                }
            }

            try
            {
                _isWindows = await Client.GetOSPlatform() == PlatformID.Win32NT;
            }
            catch (FactoryOrchestratorVersionMismatchException)
            {
                // Assume service is Windows and this API isn't implemented (Service version < 9.1.0)
                _isWindows = true;
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

            try
            {
                if (RunButtonIcon.Symbol == Symbol.Stop)
                {
                    _taskRunPoller.StopPolling();
                    _taskRunPoller = null;
                    await Client.AbortTaskRun(_activeCmdTaskRun.Guid);
                    CommandComplete();
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
            finally
            {
                _cmdSem.Release();
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

            if (e.Key == Windows.System.VirtualKey.Up)
            {
                if (_cmdHistoryIndex > 0)
                {
                    CommandBox.Text = _cmdHistory[--_cmdHistoryIndex];
                    CommandBox.SelectionStart = CommandBox.Text.Length;
                    CommandBox.SelectionLength = 0;
                }
            }
            if (e.Key == Windows.System.VirtualKey.Down)
            {
                if ((_cmdHistoryIndex + 1 < _cmdHistory.Count))
                {
                    CommandBox.Text = _cmdHistory[++_cmdHistoryIndex];
                    CommandBox.SelectionStart = CommandBox.Text.Length;
                    CommandBox.SelectionLength = 0;
                }
            }
        }

        /// <summary>
        /// Runs a command using cmd.exe
        /// </summary>
        private async Task ExecuteCommand(string command)
        {
            // Update history & reset index
            if ((_cmdHistory.Count == 0) || (!_cmdHistory[_cmdHistory.Count - 1].Equals(command, StringComparison.CurrentCulture)))
            {
                _cmdHistory.Add(command);
                if (_cmdHistory.Count > MaxCmdHistory)
                {
                    // TODO: this is O(n) - not super efficient. Using a cicular list would be ideal.
                    // However it is unlikely to occur, and with MaxCmdHistory set to 100 it still only took a few hundred ticks, not even a millisecond.
                    _cmdHistory.RemoveAt(0);
                }

                _cmdHistory.Add(command);
            }

            _cmdHistoryIndex = _cmdHistory.Count;

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
                OutputStack.Text = textBlock.Text;
            });

            // Execute command
            _newCmd = true;
            if (_taskRunPoller != null)
            {
                _taskRunPoller.StopPolling();
            }

            try
            {
                _activeRunSem.Wait();
                if (_isWindows)
                {
                    _activeCmdTaskRun = await Client.RunExecutable(@"cmd.exe", $"/C \"{command}\"", null, (bool)ContainerCheckBox.IsChecked);
                }
                else
                {
                    // Unlike Windows, every command is a file in $PATH, so we need to split the given string into the program & arguments
                    var spaceIndex = command.IndexOf(" ", StringComparison.InvariantCultureIgnoreCase);
                    string program;
                    string args = null;
                    if (spaceIndex == -1)
                    {
                        program = command;
                    }
                    else
                    {
                        program = command.Substring(0, spaceIndex);
                        args = command.Substring(spaceIndex);
                    }

                    _activeCmdTaskRun = await Client.RunExecutable(program, args, null, (bool)ContainerCheckBox.IsChecked);
                }
            }
            finally
            {
                _activeRunSem.Release();
            }

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
            _activeRunSem.Wait();
            _activeCmdTaskRun = (TaskRun)e.Result;
            _activeRunSem.Release();

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
                        try
                        {
                            _outSem.Wait();
                            UpdateOutput(blocks);
                        }
                        finally
                        {
                            _outSem.Release();
                        }
                    });
                }

                if (_activeCmdTaskRun.TaskRunComplete)
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        CommandComplete();
                    });
                }
            }
        }

        /// <summary>
        /// Allows new commands to run, focuses on text box.
        /// </summary>
        private void CommandComplete()
        {
            CommandBox.IsEnabled = true;
            CommandBox.Text = "";
            RunButtonIcon.Symbol = Symbol.Play;
            OutputStack.Focus(FocusState.Programmatic);
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            _outSem.Wait();
            try
            {
                OutputStack.Text = String.Empty; 
            }
            finally
            {
                _outSem.Release();
            }
        }

        /// <summary>
        /// Updates UI with latest console output
        /// </summary>
        private List<(string text, bool isError)> PrepareOutput()
        {
            List<(string text, bool isError)> ret = new List<(string text, bool isError)>();

            try
            {
                _activeRunSem.Wait();

                if (_newCmd)
                {
                    _lastOutput = 0;
                    _newCmd = false;
                }


                var endCount = _activeCmdTaskRun.TaskOutput.Count;
                string text = "";
                bool errorBlock = false;

                for (int i = _lastOutput; i < endCount; i++)
                {
                    if (_activeCmdTaskRun.TaskOutput[i] != null)
                    {
                        if (errorBlock && _activeCmdTaskRun.TaskOutput[i].StartsWith("ERROR: ", StringComparison.InvariantCulture))
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
                        else if (!errorBlock && _activeCmdTaskRun.TaskOutput[i].StartsWith("ERROR: ", StringComparison.InvariantCulture))
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
            }
            finally
            {
                _activeRunSem.Release();
            }
            
            return ret;
        }

        /// <summary>
        /// Updates UI with latest console output
        /// </summary>
        private void UpdateOutput(List<(string text, bool isError)> blocks)
        {
            foreach (var (text, isError) in blocks)
            {
                var textBlock = new TextBlock()
                {
                    Text = text,
                    IsTextSelectionEnabled = true
                };

                if (isError)
                {
                    textBlock.FontWeight = Windows.UI.Text.FontWeights.Bold;
                }
                OutputStack.Text =  textBlock.Text;
            }
        }

        private void ContainerCheckBox_StateChanged(object sender, RoutedEventArgs e)
        {
            ContainerGuiWarning.Visibility = (bool)ContainerCheckBox.IsChecked ? Visibility.Visible : Visibility.Collapsed;
            ContainerGuiWarningExample.Visibility = ContainerGuiWarning.Visibility;

            if (Client.IsLocalHost)
            {
                // RD button only works on LocalHost
                LaunchRD.Visibility = ContainerGuiWarning.Visibility;
            }
        }

        private async void LaunchRD_Click(object sender, RoutedEventArgs e)
        {
            var result = await Windows.System.Launcher.LaunchUriAsync(new Uri($"ms-rd:factoryosconnect?ip={_containerIp}&username=Abby"));
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _cmdSem?.Dispose();
                    _outSem?.Dispose();
                    _activeRunSem?.Dispose();
                    _taskRunPoller.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        private TaskRun _activeCmdTaskRun;
        private bool _newCmd;
        private int _lastOutput;
        private ServerPoller _taskRunPoller;
        private readonly SemaphoreSlim _cmdSem;
        private readonly SemaphoreSlim _outSem;
        private readonly SemaphoreSlim _activeRunSem;
        private FactoryOrchestratorUWPClient Client = ((App)Application.Current).Client;
        private bool _isWindows;
        private List<string> _cmdHistory;
        private int _cmdHistoryIndex;
        private string _containerIp;
        private const int MaxCmdHistory = 100;
    }
}
