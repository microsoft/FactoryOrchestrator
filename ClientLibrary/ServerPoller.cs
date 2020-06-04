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
        /// If it is NULL and the guidType is TaskList, a List of TaskListSummary objects is returned.
        /// </summary>
        /// <param name="guidToPoll">GUID of the object you want to poll</param>
        /// <param name="guidType">The type of object that GUID is for</param>
        /// <param name="pollingIntervalMs">How frequently the polling should be done, in milliseconds. Defaults to 500ms.</param>
        /// <param name="adaptiveInterval">If true, automatically adjust the polling interval for best performance. Defaults to true.</param>
        /// <param name="maxAdaptiveModifier">If adaptiveInterval is set, this defines the maximum multiplier/divisor that will be applied to the polling interval. For example, if maxAdaptiveModifier=2 and pollingIntervalMs=100, the object would be polled at a rate between 50ms to 200ms. Defaults to 5.</param>
        public ServerPoller(Guid? guidToPoll, Type guidType, int pollingIntervalMs = 500, bool adaptiveInterval = true, int maxAdaptiveModifier = 3)
        {
            PollingGuid = guidToPoll;
            _pollingInterval = pollingIntervalMs;
            _initialPollingInterval = pollingIntervalMs;
            _pollingIntervalStep = pollingIntervalMs / 10;
            LatestObject = null;
            _lastEventObject = null;
            _adaptiveInterval = adaptiveInterval;
            _adaptiveModifier = maxAdaptiveModifier;
            _timer = new Timer(GetUpdatedObjectAsync, null, Timeout.Infinite, pollingIntervalMs);
            _invokeSem = new SemaphoreSlim(1, 1);
            _stopped = true;
            OnUpdatedObject = null;
            OnException = null;
            OnlyRaiseOnExceptionEventForConnectionException = false;

            if ((guidType != typeof(TaskBase)) && (guidType != typeof(ExecutableTask)) && (guidType != typeof(UWPTask)) && (guidType != typeof(TAEFTest)) && (guidType != typeof(TaskList)) && (guidType != typeof(TaskRun)))
            {
                throw new FactoryOrchestratorException("Unsupported guid type to poll!");
            }
            _guidType = guidType;
        }

        private async void GetUpdatedObjectAsync(object state)
        {
            object newObj;
            try
            {
                if (_client.IsConnected)
                {
                    // TODO: Logging: check for failure
                    if ((_guidType == typeof(TaskBase)) || (_guidType == typeof(ExecutableTask)) || (_guidType == typeof(UWPTask)) || (_guidType == typeof(TAEFTest)))
                    {
                    	newObj = await _client.QueryTask((Guid)PollingGuid);
                    }
                    else if (_guidType == typeof(TaskList))
                    {
                    	if (PollingGuid != null)
                        {
                        	newObj = await _client.QueryTaskList((Guid)PollingGuid);
                        }
                        else
                        {
                            newObj = await _client.GetTaskListSummaries();
                        }
                    }
                    else //if (_guidType == typeof(TaskRun))
                    {
                    	newObj = await _client.QueryTaskRun((Guid)PollingGuid);
                    }

                    if (!_stopped)
                    {
                    	LatestObject = newObj;

                    	if (!Equals(newObj, _lastEventObject))
                        {
                            if (_adaptiveInterval)
                            {
                                // Adaptive detects if the invoke method is taking too long. If it is, it increases the poll time by 10% of initial value.
                                // Adaptive also throws away an invoke if it can't get the semaphore. 
                                int newInterval;
                                if (_invokeSem.Wait(0))
                                {
                                try
                                {
                                    OnUpdatedObject?.Invoke(this, new ServerPollerEventArgs(newObj));
                                }
                                catch (Exception)
                                {}

                                    _lastEventObject = newObj;
                                    _invokeSem.Release();
                                    newInterval = Math.Max(_initialPollingInterval / _adaptiveModifier, _pollingInterval - _pollingIntervalStep);
                                }
                                else
                                {
	                            newInterval = Math.Min(_initialPollingInterval * _adaptiveModifier, _pollingInterval + _pollingIntervalStep);
                                }

                                if (newInterval != _pollingInterval)
                                {
                                    _pollingInterval = newInterval;
                                    _timer?.Change(_pollingInterval, _pollingInterval);
                                }
                            }
                            else
                            {
	                            try
	                            {
	                                // update object seen as it could be changed when the invoke returns
	                                _lastEventObject = newObj;
	                                OnUpdatedObject?.Invoke(this, new ServerPollerEventArgs(newObj));
	                            }
	                            catch (Exception)
	                            {}
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (!OnlyRaiseOnExceptionEventForConnectionException || e.GetType() != typeof(FactoryOrchestratorConnectionException))
                {
                    OnException?.Invoke(this, new ServerPollerExceptionHandlerArgs(e));
                }
            }
        }

        /// <summary>
        /// Starts polling the object.
        /// </summary>
        /// <param name="client">The FactoryOrchestratorClient object to use for polling.</param>
        public void StartPolling(FactoryOrchestratorClient client)
        {
            _client = client;

            if (!_client.IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            if (_stopped != false)
            {
                _stopped = false;
                LatestObject = null;
                _lastEventObject = null;
                _timer = new Timer(GetUpdatedObjectAsync, null, 0, _pollingInterval);
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
        public object LatestObject { get; private set; }

        /// <summary>
        /// The GUID of the object you are polling. Can be NULL for some scenarios.
        /// </summary>
        public Guid? PollingGuid { get; }

        /// <summary>
        /// If true, the poller is actively polling for updates.
        /// </summary>
        public bool IsPolling { get => !_stopped; }

        private FactoryOrchestratorClient _client;
        private object _lastEventObject;
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
        /// Event raised when a new object is received. It is only thrown if the object has changed since last polled.
        /// </summary>
        public event ServerPollerEventHandler OnUpdatedObject;

        /// <summary>
        /// Event raised when a poll attempt throws an exception.
        /// </summary>
        public event ServerPollerExceptionHandler OnException;

        /// <summary>
        /// If true, OnException only raised when the exception is a FactoryOrchestratorConnectionException.
        /// Other exceptions are ignored!
        /// </summary>
        public bool OnlyRaiseOnExceptionEventForConnectionException { get; set; }
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

    /// <summary>
    /// Event handler delegate for a when a new object has been retrieved from the Server.
    /// </summary>
    /// <param name="source">The ServerPoller that retrieved the object.</param>
    /// <param name="e">The result of the latest poll operation.</param>
    public delegate void ServerPollerEventHandler(object source, ServerPollerEventArgs e);

    /// <summary>
    /// Event handler delegate for a when the poller hit an exception while polling.
    /// </summary>
    /// <param name="source">The ServerPoller that retrieved the object.</param>
    /// <param name="e">The exception from the latest poll operation.</param>
    public delegate void ServerPollerExceptionHandler(object source, ServerPollerExceptionHandlerArgs e);

    /// <summary>
    /// Class containing the exception thrown from the latest poll operation.
    /// </summary>
    public class ServerPollerExceptionHandlerArgs : EventArgs
    {
        /// <summary>
        /// Creates a new ServerPollerExceptionArgs instance.
        /// </summary>
        /// <param name="exception">Exception from latest poll of the Server.</param>
        public ServerPollerExceptionHandlerArgs(Exception exception)
        {
            Exception = exception;
        }

        /// <summary>
        /// The updated object polled on the server.
        /// </summary>
        public Exception Exception { get; }
    }
}