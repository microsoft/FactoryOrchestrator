using JKang.IpcServiceFramework;
using System;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.FactoryOrchestrator.Core;
using TaskStatus = Microsoft.FactoryOrchestrator.Core.TaskStatus;

namespace Microsoft.FactoryOrchestrator.Client
{
    /// <summary>
    /// Factory Ochestrator uses a polling model. ServerPoller is used to create a polling thread for a given Factory Ochestrator GUID. It can optionally raise a ServerPollerEvent event via OnUpdatedObject.
    /// All Factory Orchestrator GUID types are supported.
    /// </summary>
    public class ServerPoller
    {
        /// <summary>
        /// Create a new ServerPoller. The ServerPoller is associated with a specific FactoryOrchestratorClient and object you want to poll. The desired object is referred to by its GUID. The GUID can be NULL for TaskRun polling.
        /// If it is NULL and the guidType is TaskList, List<TaskListSummary> is returned.
        /// </summary>
        /// <param name="guidToPoll">GUID of the object you want to poll</param>
        /// <param name="guidType">The type of object that GUID is for</param>
        /// <param name="client">A connected FactoryOrchestratorClient instance</param>
        /// <param name="pollingIntervalMs">How frequently the polling should be done, in milliseconds. Defaults to 500ms.</param>
        /// <param name="adaptiveInterval">If true, automatically adjust the polling interval for best performance. Defaults to true.</param>
        /// <param name="maxAdaptiveModifier">If adaptiveInterval is set, this defines the maximum multiplier/divisor that will be applied to the polling interval. For example, if maxAdaptiveModifier=2 and pollingIntervalMs=100, the object would be polled at a rate between 50ms to 200ms. Defaults to 5.</param>
        public ServerPoller(Guid? guidToPoll, Type guidType, FactoryOrchestratorClient client, int pollingIntervalMs = 500, bool adaptiveInterval = true, int maxAdaptiveModifier = 5)
        {
            _guidToPoll = guidToPoll;
            _client = client;
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
                    newObj = await _client.QueryTask((Guid)_guidToPoll);
                }
                else if (_guidType == typeof(TaskList))
                {
                    if (_guidToPoll != null)
                    {
                        newObj = await _client.QueryTaskList((Guid)_guidToPoll);
                    }
                    else
                    {
                        newObj = await _client.GetTaskListSummaries();
                    }
                }
                else //if (_guidType == typeof(TaskRun))
                {
                    newObj = await _client.QueryTaskRun((Guid)_guidToPoll);
                }

                if (((newObj == null) && (_latestObject != null)) || (!newObj.Equals(_latestObject)))
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
                                OnUpdatedObject?.Invoke(this, new ServerPollerEventArgs(_latestObject));
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
                            OnUpdatedObject?.Invoke(this, new ServerPollerEventArgs(_latestObject));
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

        /// <summary>
        /// Starts polling the object.
        /// </summary>
        public void StartPolling()
        {
            if (_stopped != false)
            {
                _stopped = false;
                _timer = new Timer(GetUpdatedObjectAsync, null, 0, _pollingInterval);
                _latestObject = null;
            }
        }

        /// <summary>
        /// Stops polling the object.
        /// </summary>
        public void StopPolling()
        {
            _stopped = true;
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }

        /// <summary>
        /// Returns the latest object retrieved from the server.
        /// </summary>
        public object LatestObject => _latestObject;

        /// <summary>
        /// The GUID of the object you are polling. Can be NULL for some scenarios.
        /// </summary>
        public Guid? PollingGuid { get => _guidToPoll; }

        /// <summary>
        /// If true, the poller is actively polling for updates.
        /// </summary>
        public bool IsPolling { get => !_stopped; }

        private Guid? _guidToPoll;
        private FactoryOrchestratorClient _client;
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

        /// <summary>
        /// Event thrown when a new object is received. It is only thrown if the object has changed since last polled.
        /// </summary>
        public event ServerPollerEventHandler OnUpdatedObject;
    }

    /// <summary>
    /// Class used to share the new object with the callee via OnUpdatedObject. 
    /// </summary>
    public class ServerPollerEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a new ServerPollerEventArgs instance.
        /// </summary>
        /// <param name="result">Object from latest poll of the Server.</param>
        public ServerPollerEventArgs(object result)
        {
            Result = result;
        }

        /// <summary>
        /// The updated object polled on the server.
        /// </summary>
        public object Result { get; }
    }

    public delegate void ServerPollerEventHandler(object source, ServerPollerEventArgs e);
}