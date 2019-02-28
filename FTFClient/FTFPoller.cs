using FTFInterfaces;
using FTFTestExecution;
using JKang.IpcServiceFramework;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace FTFClient
{
    public class FTFPoller
    {
        public FTFPoller(Guid guidToPoll, Type guidType, IpcServiceClient<IFTFCommunication> ipcServiceClient, int pollingIntervalMs = 1000)
        {
            _guidToPoll = guidToPoll;
            _client = ipcServiceClient;
            _pollingInterval = pollingIntervalMs;
            _latestObject = null;
            _timer = new Timer(GetUpdatedObjectAsync, null, Timeout.Infinite, pollingIntervalMs);
            _stoplock = new object();
            _stopped = false;
            OnUpdatedObject = null;

            if ((guidType != typeof(TestBase)) && (guidType != typeof(ExecutableTest)) && (guidType != typeof(UWPTest)) && (guidType != typeof(TAEFTest)) && (guidType != typeof(TestList)))
            {
                throw new Exception("Unsupported type!");
            }
            _guidType = guidType;
        }

        private async void GetUpdatedObjectAsync(object state)
        {
            object newObj;
            try // TODO: Try to improve resiliency here
            {
                if ((_guidType == typeof(TestBase)) || (_guidType == typeof(ExecutableTest)) || (_guidType == typeof(UWPTest)) || (_guidType == typeof(TAEFTest)))
                {
                    newObj = await _client.InvokeAsync(x => x.QueryTest(_guidToPoll));
                }
                else // if (_guidType == typeof(TestList))
                {
                    newObj = await _client.InvokeAsync(x => x.QueryTestList(_guidToPoll));
                }

                if (newObj != _latestObject)
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
            catch(Exception)
            {

            }
        }
        

        public void StartPolling()
        {
            _stopped = false;
            _timer = new Timer(GetUpdatedObjectAsync, null, 0, _pollingInterval);
            _latestObject = null;
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

        private Guid _guidToPoll;
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
