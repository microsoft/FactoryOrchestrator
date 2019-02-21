using FTFInterfaces;
using FTFTestExecution;
using JKang.IpcServiceFramework;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace FTFClient
{
    public class TestListPoller
    {
        public TestListPoller (Guid testListGuidToPoll, IpcServiceClient<IFTFCommunication> ipcServiceClient, int pollingIntervalMs = 1000)
        {
            _testListGuid = testListGuidToPoll;
            _client = ipcServiceClient;
            _pollingInterval = pollingIntervalMs;
            _testList = null;
            _timer = new Timer(GetUpdatedTestListAsync, null, Timeout.Infinite, pollingIntervalMs);
        }

        private async void GetUpdatedTestListAsync(object state)
        {
            _testList = await _client.InvokeAsync(x => x.QueryTestList(_testListGuid));
            OnUpdatedTestList?.Invoke(this, new TestListPollEventArgs(_testList));
        }
        

        public void StartPolling()
        {
            _timer = new Timer(GetUpdatedTestListAsync, null, 0, _pollingInterval);
        }

        public void StopPolling()
        {
            _timer.Dispose();
            _timer = null;
            _testList = null;
        }

        public TestList GetLatestTestList()
        {
            if (_timer != null)
            {
                while (_testList == null)
                {
                    Thread.Sleep(_pollingInterval + 10);
                }

                return _testList;
            }
            else
            {
                throw new Exception("Start polling before calling this method!");
            }
        }

        private Guid _testListGuid;
        private IpcServiceClient<IFTFCommunication> _client;
        private TestList _testList;
        private int _pollingInterval;
        private Timer _timer;
        public event TestListPollerEventHandler OnUpdatedTestList;

    }
    public class TestListPollEventArgs : EventArgs
    {
        public TestListPollEventArgs(TestList testList)
        {
            TestList = testList;
        }

        public TestList TestList { get; }
    }

    public delegate void TestListPollerEventHandler(object source, TestListPollEventArgs e);
}
