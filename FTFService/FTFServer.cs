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

    // Find the FTFService singleton -> pass call to it
    public class FTFCommunicationHandler : IFTFCommunication
    {
        // TODO: Catch exceptions & log them
        public TestList CreateTestListFromDirectory(string path, bool onlyTAEF)
        {
            return FTFService.Instance.TestExecutionManager.CreateTestListFromDirectory(path, onlyTAEF);
        }

        public List<Guid> LoadTestListsFromXmlFile(string filePath)
        {
            throw new NotImplementedException();
        }

        public TestList CreateTestListFromTestList(TestList list)
        {
            return FTFService.Instance.TestExecutionManager.CreateTestListFromTestList(list);
        }

        public List<Guid> GetTestListGuids()
        {
            return FTFService.Instance.TestExecutionManager.GetKnownTestListGuids();
        }

        public TestList QueryTestList(Guid testListGuid)
        {
            return FTFService.Instance.TestExecutionManager.GetKnownTestList(testListGuid);
        }

        public bool DeleteTestList(Guid listToDelete)
        {
            return FTFService.Instance.TestExecutionManager.DeleteTestList(listToDelete);
        }

        public TestOutput GetAllOutput(Guid guid)
        {
            throw new NotImplementedException();
        }

        public TestOutput GetErrors(Guid guid, ulong index)
        {
            throw new NotImplementedException();
        }

        public TestEventDatum GetExecutionStatus(Guid guid)
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

        public void ResetService()
        {
            FTFService.Instance.TestExecutionManager.Reset();
        }

        public bool Run(Guid TestListToRun, bool allowOtherTestListsToRun, bool runListInParallel)
        {
            return FTFService.Instance.TestExecutionManager.Run(TestListToRun, allowOtherTestListsToRun, runListInParallel);
        }

        public bool UpdateTestStatus(TestBase latestTestStatus)
        {
            return FTFService.Instance.TestExecutionManager.UpdateTestStatus(latestTestStatus);
        }

        public bool UpdateTestList(TestList testList)
        {
            return FTFService.Instance.TestExecutionManager.UpdateTestList(testList);
        }

        public bool SetDefaultTePath(string teExePath)
        {
            return TestRunner.SetDefaultTePath(teExePath);
        }

        public bool SetDefaultLogFolder(string logFolder)
        {
            return TestRunner.SetDefaultLogFolder(logFolder);
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
