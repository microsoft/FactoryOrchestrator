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

    // find the singleton -> pass call to it
    public class FTFCommunicationHandler : IFTFCommunication
    {
        // test is running?
        public TestList EnumerateTests(string path, bool onlyTAEF)
        {
            return FTFService.Instance.EnumerateTests(path, onlyTAEF);
        }

        public bool RunTestList(TestList listToRun, bool runInParallel)
        {
            return FTFService.Instance.RunTestList(listToRun, runInParallel);
        } 
    }
    
    public class FTFService : IMicroService
    {
        private static FTFService _singleton = null;
        private static readonly Mutex _constructorLock = new Mutex();


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
            _constructorLock.WaitOne();
            if (_singleton == null)
            {
                _controller = controller;
                _logger = logger;
                _singleton = this;
            }
            else
            {
                throw new Exception("FTFService already created! Only one instance allowed.");
            }
            _constructorLock.ReleaseMutex();
        }

        public TestList EnumerateTests(string path, bool onlyTAEF)
        {
            return TestManager.EnumerateTests(path, onlyTAEF);
        }

        public bool RunTestList(TestList listToRun, bool runInParallel)
        {
            return TestManager.RunTestList(listToRun, runInParallel);
        }


        public void Start()
        {
            _cancellationToken = new System.Threading.CancellationTokenSource();
            FTFExecutable.ipcHost.RunAsync(_cancellationToken.Token);
            
            
            _logger.LogTrace("Started\n");
        }

        public void Stop()
        {
            _cancellationToken.Cancel();
            _logger.LogTrace("Stopped\n");
        }
    }
}
