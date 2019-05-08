using JKang.IpcServiceFramework;
using System;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.FactoryTestFramework.Core;

namespace Microsoft.FactoryTestFramework.Client
{
    /// <summary>
    /// FTFPoller is used to create a polling thread for a given FTF GUID. It can optionally raise a FTFPollerEvent event via OnUpdatedObject.
    /// All FTF GUID types are supported.
    /// </summary>
    public class FTFPoller
    {
        public FTFPoller(Guid? guidToPoll, Type guidType, IpcServiceClient<IFTFCommunication> ipcServiceClient, int pollingIntervalMs = 1000)
        {
            _guidToPoll = guidToPoll;
            _client = ipcServiceClient;
            _pollingInterval = pollingIntervalMs;
            _latestObject = null;
            _timer = new Timer(GetUpdatedObjectAsync, null, Timeout.Infinite, pollingIntervalMs);
            _stoplock = new object();
            _stopped = true;
            OnUpdatedObject = null;

            if ((guidType != typeof(TestBase)) && (guidType != typeof(ExecutableTest)) && (guidType != typeof(UWPTest)) && (guidType != typeof(TAEFTest)) && (guidType != typeof(TestList)) && (guidType != typeof(TestRun)))
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
                if ((_guidType == typeof(TestBase)) || (_guidType == typeof(ExecutableTest)) || (_guidType == typeof(UWPTest)) || (_guidType == typeof(TAEFTest)))
                {
                    newObj = await _client.InvokeAsync(x => x.QueryTest((Guid)_guidToPoll));
                }
                else if (_guidType == typeof(TestList))
                {
                    if (_guidToPoll != null)
                    {
                        newObj = await _client.InvokeAsync(x => x.QueryTestList((Guid)_guidToPoll));
                    }
                    else
                    {
                        newObj = await _client.InvokeAsync(x => x.GetTestListGuids());
                    }
                }
                else //if (_guidType == typeof(TestRun))
                {
                    newObj = await _client.InvokeAsync(x => x.QueryTestRun((Guid)_guidToPoll));
                }

                if (!newObj.Equals(_latestObject))
                {
                    _latestObject = newObj;
                    lock (_stoplock)
                    {
                        if (!_stopped)
                        {
                            OnUpdatedObject?.Invoke(this, new FTFPollEventArgs(_latestObject));
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
            lock (_stoplock)
            {
                _stopped = true;
                if (_timer != null)
                {
                    _timer.Dispose();
                    _timer = null;
                }
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
        private Timer _timer;
        private object _stoplock;
        private Type _guidType;
        private bool _stopped;
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