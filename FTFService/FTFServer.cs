using JKang.IpcServiceFramework;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using PeterKottas.DotNetCore.WindowsService.Base;
using PeterKottas.DotNetCore.WindowsService.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FTFInterfaces;
using FTFTestExecution;
using System.Threading;

namespace FTFService
{

    // find the FTFService singleton -> pass call to it
    public class FTFCommunicationHandler : IFTFCommunication
    {
        public TestList CreateTestListFromDirectory(string path, bool onlyTAEF)
        {
            return FTFService.Instance.TestExecutionManager.CreateTestListFromDirectory(path, onlyTAEF);
        }

        public TestList CreateTestListFromFile(string filePath)
        {
            throw new NotImplementedException();
        }

        public TestList CreateTestListFromTestList(TestList list)
        {
            throw new NotImplementedException();
        }

        public bool DeleteTestList(Guid listToDelete)
        {
            throw new NotImplementedException();
        }

        public TestOutput GetAllOutput(Guid guid)
        {
            throw new NotImplementedException();
        }

        public TestOutput GetErrors(Guid guid, ulong index)
        {
            throw new NotImplementedException();
        }

        public List<TestOutput> GetLatestOutput(Guid guid, DateTime fromTime)
        {
            throw new NotImplementedException();
        }

        public ServiceEventDatum GetServiceUpdate(List<FTFEventType> types)
        {
            throw new NotImplementedException();
        }

        public ServiceEventDatum GetServiceUpdate(FTFEventType type)
        {
            throw new NotImplementedException();
        }

        public ServiceEventDatum GetServiceUpdate()
        {
            throw new NotImplementedException();
        }

        public TestEventDatum GetStatus(Guid guid)
        {
            throw new NotImplementedException();
        }

        public List<Guid> GetTestListGuids()
        {
            throw new NotImplementedException();
        }

        public TestList QueryTestList(Guid guid)
        {
            throw new NotImplementedException();
        }

        public bool Run(Guid TestListToRun, bool allowOtherTestListsToRun, bool runListInParallel)
        {
            //return FTFService.Instance.TestExecutionManager.
        }

        public void SetUWPTestResult(Guid testGuid, TestEventDatum testEvent)
        {
            throw new NotImplementedException();
        }

        public bool UpdateTestList(Guid guid, TestList testList)
        {
            throw new NotImplementedException();
        }
    }

    public class FTFService : IMicroService
    {
        private static FTFService _singleton = null;
        private static readonly object _constructorLock = new object();

        private TestManager _testExecutionManager;
        private IMicroServiceController _controller;
        private ILogger<FTFService> _logger;
        private System.Threading.CancellationTokenSource _cancellationToken;

        public static FTFService Instance
        {
            get
            {
                return _singleton;
            }
        }

        public FTFService(IMicroServiceController controller, ILogger<FTFService> logger)
        {
            lock (_constructorLock)
            {
                if (_singleton == null)
                {
                    _controller = controller;
                    _logger = logger;
                    _singleton = this;
                    _testExecutionManager = new TestManager();
                }
                else
                {
                    throw new Exception("FTFService already created! Only one instance allowed.");
                }
            }
        }

        public TestManager TestExecutionManager { get => _testExecutionManager; }
        public void Start()
        {
            // Start IPC server
            _cancellationToken = new System.Threading.CancellationTokenSource();
            FTFServiceExe.ipcHost.RunAsync(_cancellationToken.Token);

            _logger.LogTrace("Started\n");
        }

        public void Stop()
        {
            _cancellationToken.Cancel();
            _logger.LogTrace("Stopped\n");
        }
    }
}
