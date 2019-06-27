using JKang.IpcServiceFramework;
using System;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.FactoryOrchestrator.Core;
using TaskStatus = Microsoft.FactoryOrchestrator.Core.TaskStatus;

namespace Microsoft.FactoryOrchestrator.Client
{
    /// <summary>
    /// FTFPoller is used to create a polling thread for a given FTF GUID. It can optionally raise a FTFPollerEvent event via OnUpdatedObject.
    /// All FTF GUID types are supported.
    /// </summary>
    public class FTFPoller
    {
        public FTFPoller(Guid? guidToPoll, Type guidType, IpcServiceClient<IFTFCommunication> ipcServiceClient, int pollingIntervalMs = 500, bool adaptiveInterval = true, int maxAdaptiveModifier = 5)
        {
            _guidToPoll = guidToPoll;
            _client = ipcServiceClient;
            _pollingInterval = pollingIntervalMs;
            _initialPollingInterval = pollingIntervalMs;
            _pollingIntervalStep = pollingIntervalMs / 10;
            _latestObject = null;
            _adaptiveInterval = adaptiveInterval;
            _adaptiveModifier = maxAdaptiveModifier;
            _timer = new Timer(GetUpdatedObjectAsync, null, Timeout.Infinite, pollingIntervalMs);
            _invokeSem = new SemaphoreSlim(1, 1);
            _stopped = true;
            OnUpdatedObject = null;

            if ((guidType != typeof(TaskBase)) && (guidType != typeof(ExecutableTask)) && (guidType != typeof(UWPTask)) && (guidType != typeof(TAEFTest)) && (guidType != typeof(TaskList)) && (guidType != typeof(TaskRun)))
            {
                throw new Exception("Unsupported guid type to poll!");
            }
            _guidType = guidType;
        }

        private async void GetUpdatedObjectAsync(object state)
        {
            object newObj;
            try
            {
                // TODO: Logging: check for failure
                if ((_guidType == typeof(TaskBase)) || (_guidType == typeof(ExecutableTask)) || (_guidType == typeof(UWPTask)) || (_guidType == typeof(TAEFTest)))
                {
                    newObj = await _client.InvokeAsync(x => x.QueryTask((Guid)_guidToPoll));
                }
                else if (_guidType == typeof(TaskList))
                {
                    if (_guidToPoll != null)
                    {
                        newObj = await _client.InvokeAsync(x => x.QueryTaskList((Guid)_guidToPoll));
                    }
                    else
                    {
                        newObj = await _client.InvokeAsync(x => x.GetTaskListGuids());
                    }
                }
                else //if (_guidType == typeof(TaskRun))
                {
                    newObj = await _client.InvokeAsync(x => x.QueryTaskRun((Guid)_guidToPoll));
                }

                if (!newObj.Equals(_latestObject))
                {
                    _latestObject = newObj;
                    if (!_stopped)
                    {
                        if (_adaptiveInterval)
                        {
                            // Adaptive detects if the invoke method is taking too long. If it is, it increases the poll time by 10% of initial value.
                            // Adaptive also throws away an invoke if it can't get the semaphore. 
                            // Max change is maxAdaptiveModifier initial interval (5x default)
                            int newInterval;
                            if (_invokeSem.Wait(0))
                            {
                                OnUpdatedObject?.Invoke(this, new FTFPollEventArgs(_latestObject));
                                _invokeSem.Release();
                                newInterval = Math.Max(_initialPollingInterval / _adaptiveModifier, _pollingInterval - _pollingIntervalStep);
                            }
                            else
                            {
                                newInterval = Math.Max(_initialPollingInterval * _adaptiveModifier, _pollingInterval + _pollingIntervalStep);
                            }

                            if (newInterval != _pollingInterval)
                            {
                                _pollingInterval = newInterval;
                                _timer.Change(_pollingInterval, _pollingInterval);
                            }
                        }
                        else
                        {
                            _invokeSem.Wait();
                            OnUpdatedObject?.Invoke(this, new FTFPollEventArgs(_latestObject));
                            _invokeSem.Release();
                        }
                    }
                }
            }
            catch (Exception)
            {
                // TODO: Logging: Log exception
            }
        }

        public void StartPolling()
        {
            if (_stopped != false)
            {
                _stopped = false;
                _timer = new Timer(GetUpdatedObjectAsync, null, 0, _pollingInterval);
                _latestObject = null;
            }
        }

        public void StopPolling()
        {
            _stopped = true;
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }

        public object LatestObject
        {
            get
            {
                if (_timer != null)
                {
                    while ((_timer != null) && (_latestObject == null))
                    {
                        Thread.Sleep(_pollingInterval + 10);
                    }

                    return _latestObject;
                }
                else
                {
                    return _latestObject;
                }
            }
        }

        public Guid? PollingGuid { get => _guidToPoll; }
        public bool IsPolling { get => !_stopped; }

        private Guid? _guidToPoll;
        private IpcServiceClient<IFTFCommunication> _client;
        private object _latestObject;
        private int _pollingInterval;
        private int _initialPollingInterval;
        private int _pollingIntervalStep;
        private Timer _timer;
        private SemaphoreSlim _invokeSem;
        private Type _guidType;
        private bool _stopped;
        private bool _adaptiveInterval;
        private int _adaptiveModifier;
        public event FTFPollerEventHandler OnUpdatedObject;
    }

    public class FTFPollEventArgs : EventArgs
    {
        public FTFPollEventArgs(object result)
        {
            Result = result;
        }

        public object Result { get; }
    }

    public delegate void FTFPollerEventHandler(object source, FTFPollEventArgs e);
}