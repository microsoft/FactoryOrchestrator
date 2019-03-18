using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using PeterKottas.DotNetCore.WindowsService;
using JKang.IpcServiceFramework;
using System.Net;
using System.Threading.Tasks;
using JKang.IpcServiceFramework.Services;
using System.Collections.Generic;
using PeterKottas.DotNetCore.WindowsService.Interfaces;
using System;
using Microsoft.FactoryTestFramework.Core;
using Microsoft.FactoryTestFramework.Server;

namespace Microsoft.FactoryTestFramework.Service
{
    class FTFServiceExe
    {
        public static IIpcServiceHost ipcHost;

        public static void Main(string[] args)
        {
#if DEBUG
            var _logLevel = LogLevel.Debug;
#else
    var _logLevel = LogLevel.Information;
#endif
            // Create service collection
            var services = new ServiceCollection();


            // Configure IPC service framework server
            services = (ServiceCollection)services.AddIpc(builder =>
            {
                builder
                    .AddTcp()
                    .AddService<IFTFCommunication, FTFCommunicationHandler>();
            });

            // Configure service provider for logger creation and managment
            ServiceProvider svcProvider = services
                .AddLogging(builder =>
                {
                    builder
                    .SetMinimumLevel(_logLevel);

                })
                .AddOptions()
                .AddSingleton(new LoggerFactory())
                .BuildServiceProvider();

            // Enable both console logging and file logging
            svcProvider.GetService<ILoggerFactory>().AddConsole();
            svcProvider.GetRequiredService<ILoggerFactory>().AddProvider(new LogFileProvider());

            // Allow any client on the network to connect to the FTFService, including loopback (other processes on this device)
            // For network clients to work, we need to createa firewall entry:
            // netsh advfirewall firewall add rule name=ftfservice_tcp_in program=<Path to FTFService.exe> protocol=tcp dir=in enable=yes action=allow profile=public,private,domain
            // netsh advfirewall firewall add rule name=ftfservice_tcp_out program=<Path to FTFService.exe> protocol=tcp dir=out enable=yes action=allow profile=public,private,domain
            ipcHost = new IpcServiceHostBuilder(svcProvider).AddTcpEndpoint<IFTFCommunication>("tcp", IPAddress.Any, 45684)
                                                            .Build();

            var _logger = svcProvider.GetRequiredService<ILoggerFactory>().CreateLogger<FTFServiceExe>();

            // FTFService handler
            ServiceRunner<FTFService>.Run(config =>
            {
                var name = config.GetDefaultName();
                config.Service(serviceConfig =>
                {
                    serviceConfig.ServiceFactory((extraArguments, controller) =>
                    {
                        return new FTFService(controller, svcProvider.GetRequiredService<ILoggerFactory>().CreateLogger<FTFService>());
                    });

                    serviceConfig.OnStart((service, extraParams) =>
                    {
                        _logger.LogInformation("Service {0} started", name);
                        service.Start();
                    });

                    serviceConfig.OnStop(service =>
                    {
                        _logger.LogInformation("Service {0} stopped", name);
                        service.Stop();
                    });

                    serviceConfig.OnError(e =>
                    {
                        _logger.LogError(e, string.Format("Service {0} errored with exception", name));
                    });
                });
            });

            // Dispose of loggers, this needs to be done manually
            svcProvider.GetService<ILoggerFactory>().Dispose();
        }
    }

    // Find the FTFService singleton -> pass call to it
    public class FTFCommunicationHandler : IFTFCommunication
    {
        // TODO: Catch exceptions & log them
        public TestList CreateTestListFromDirectory(string path, bool onlyTAEF)
        {
            FTFService.Instance.ServiceLogger.LogTrace("hit create tl");
            var tl = FTFService.Instance.TestExecutionManager.CreateTestListFromDirectory(path, onlyTAEF);
            FTFService.Instance.ServiceLogger.LogTrace("finished create tl");
            return tl;
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
        public TestBase QueryTest(Guid testGuid)
        {
            return FTFService.Instance.TestExecutionManager.GetKnownTest(testGuid);
        }

        public bool DeleteTestList(Guid listToDelete)
        {
            return FTFService.Instance.TestExecutionManager.DeleteTestList(listToDelete);
        }

        public void ResetService(bool preserveLogs = true)
        {
            FTFService.Instance.TestExecutionManager.Reset(preserveLogs);
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

        public void StopAll()
        {
            FTFService.Instance.TestExecutionManager.Abort();
        }

        public void Stop(Guid testListGuid)
        {
            FTFService.Instance.TestExecutionManager.Abort(testListGuid);
        }

        public List<ServiceEvent> GetServiceEvents(DateTime timeLastChecked, ServiceEventType serviceEventType)
        {
            throw new NotImplementedException();
        }

        public List<ServiceEvent> GetServiceEvents(long lastEventIndex, ServiceEventType serviceEventType)
        {
            throw new NotImplementedException();
        }

        public TestRun QueryTestRun(Guid testRunGuid)
        {
            return TestRun_Server.GetTestRunByGuid(testRunGuid);
        }

        public bool SetTestRunStatus(TestRun testRunStatuus)
        {
            throw new NotImplementedException();
        }

        public bool Run(Guid TestListToRun, bool allowOtherTestListsToRun, bool runListInParallel)
        {
            return FTFService.Instance.TestExecutionManager.Run(TestListToRun, allowOtherTestListsToRun, runListInParallel);
        }
    }

    public class FTFService : IMicroService
    {
        private static FTFService _singleton = null;
        private static readonly object _constructorLock = new object();

        private TestManager_Server _testExecutionManager;
        private IMicroServiceController _controller;
        public ILogger<FTFService> ServiceLogger;
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
                    ServiceLogger = logger;
                    _singleton = this;
                    _testExecutionManager = new TestManager_Server();
                }
                else
                {
                    throw new Exception("FTFService already created! Only one instance allowed.");
                }
            }
        }

        public TestManager_Server TestExecutionManager { get => _testExecutionManager; }
        public void Start()
        {
            // Start IPC server
            _cancellationToken = new System.Threading.CancellationTokenSource();
            FTFServiceExe.ipcHost.RunAsync(_cancellationToken.Token);

            ServiceLogger.LogTrace("Started\n");
        }

        public void Stop()
        {
            _cancellationToken.Cancel();
            ServiceLogger.LogTrace("Stopped\n");
        }
    }
}