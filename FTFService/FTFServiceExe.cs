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
using System.Reflection;
using System.Linq;
using Microsoft.Win32;
using Microsoft.Extensions.PlatformAbstractions;
using System.IO;

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
        //[MethodImpl(MethodImplOptions.NoInlining)]
        //public static string GetCurrentMethod()
        //{
        //    var st = new StackTrace();
        //    var sf = st.GetFrame(1);

        //    return sf.GetMethod().Name;
        //}

        // TODO: Logging: Catch exceptions & log them
        public List<ServiceEvent> GetAllServiceEvents()
        {
            return FTFService.Instance.ServiceEvents.Values.ToList();
        }

        public List<ServiceEvent> GetServiceEventsByTime(DateTime timeLastChecked)
        {
            if (timeLastChecked < FTFService.Instance.LastEventTime)
            {
                return FTFService.Instance.ServiceEvents.Values.Where(x => x.EventTime > timeLastChecked).ToList();
            }
            else
            {
                return new List<ServiceEvent>();
            }
        }

        public List<ServiceEvent> GetServiceEventsByIndex(ulong lastEventIndex)
        {
            if (lastEventIndex < FTFService.Instance.LastEventIndex)
            {
                return FTFService.Instance.ServiceEvents.Where(x => x.Key > lastEventIndex).Select(x => x.Value).ToList();
            }
            else
            {
                return new List<ServiceEvent>();
            }
        }

        public TestList CreateTestListFromDirectory(string path, bool onlyTAEF)
        {
            FTFService.Instance.ServiceLogger.LogTrace($"Start: CreateTestListFromDirectory {path}");
            var tl = FTFService.Instance.TestExecutionManager.CreateTestListFromDirectory(path, onlyTAEF);
            FTFService.Instance.ServiceLogger.LogTrace($"Finish: CreateTestListFromDirectory {path}");
            return tl;
        }

        public List<Guid> LoadTestListsFromXmlFile(string filename)
        {
            FTFService.Instance.ServiceLogger.LogTrace($"Start: LoadTestListsFromXmlFile {filename}");
            var tls = FTFService.Instance.TestExecutionManager.LoadTestListsFromXmlFile(filename);
            FTFService.Instance.ServiceLogger.LogTrace($"Finish: LoadTestListsFromXmlFile {filename}");
            return tls;
        }

        public TestList CreateTestListFromTestList(TestList list)
        {
            FTFService.Instance.ServiceLogger.LogTrace($"Start: CreateTestListFromTestList {list.Guid}");
            var serverList = FTFService.Instance.TestExecutionManager.CreateTestListFromTestList(list);
            FTFService.Instance.ServiceLogger.LogTrace($"Finish: CreateTestListFromTestList {list.Guid}");
            return serverList;
        }

        public bool SaveTestListToXmlFile(Guid guid, string filename)
        {
            FTFService.Instance.ServiceLogger.LogTrace($"Start: SaveTestListToXmlFile {guid} {filename}");
            var saved = FTFService.Instance.TestExecutionManager.SaveTestListToXmlFile(guid, filename);
            FTFService.Instance.ServiceLogger.LogTrace($"Finish: SaveTestListToXmlFile {guid} {filename}");
            return saved;
        }

        public bool SaveAllTestListsToXmlFile(string filename)
        {
            FTFService.Instance.ServiceLogger.LogTrace($"Start: SaveAllTestListsToXmlFile {filename}");
            var saved = FTFService.Instance.TestExecutionManager.SaveAllTestListsToXmlFile(filename);
            FTFService.Instance.ServiceLogger.LogTrace($"Finish: SaveAllTestListsToXmlFile {filename}");
            return saved;
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

        public bool UpdateTest(TestBase updatedTest)
        {
            return FTFService.Instance.TestExecutionManager.UpdateTest(updatedTest);
        }

        public bool UpdateTestList(TestList testList)
        {
            return FTFService.Instance.TestExecutionManager.UpdateTestList(testList);
        }

        public bool SetDefaultTePath(string teExePath)
        {
            try
            {
                TestRunner.GlobalTeExePath = teExePath;
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public bool SetDefaultLogFolder(string logFolder)
        {
            return FTFService.Instance.TestExecutionManager.SetDefaultLogFolder(logFolder);
        }

        public void StopAll()
        {
            FTFService.Instance.TestExecutionManager.Abort();
        }

        public void Stop(Guid testListGuid)
        {
            FTFService.Instance.TestExecutionManager.Abort(testListGuid);
        }

        public string GetServiceVersionString()
        {
            return FTFService.GetServiceVersionString();
        }

        public TestRun QueryTestRun(Guid testRunGuid)
        {
            return TestRun_Server.GetTestRunByGuid(testRunGuid);
        }

        public bool SetTestRunStatus(TestRun testRunStatus)
        {
            return FTFService.Instance.TestExecutionManager.UpdateTestRunStatus(testRunStatus);
        }

        public bool Run(Guid TestListToRun, bool allowOtherTestListsToRun, bool runListInParallel)
        {
            return FTFService.Instance.TestExecutionManager.Run(TestListToRun, allowOtherTestListsToRun, runListInParallel);
        }

        public TestRun RunExecutableOutsideTestList(string exeFilePath, string arguments, string consoleLogFilePath = null)
        {
            return FTFService.Instance.TestExecutionManager.RunExecutableOutsideTestList(exeFilePath, arguments, consoleLogFilePath);
        }


        public TestRun RunTestOutsideTestList(Guid testGuid)
        {
            return FTFService.Instance.TestExecutionManager.RunTestOutsideTestList(testGuid);
        }

        public TestRun RunUWPOutsideTestList(string packageFamilyName)
        {
            return FTFService.Instance.TestExecutionManager.RunUWPOutsideTestList(packageFamilyName);
        }

        public byte[] GetFile(string sourceFilename)
        {
            if (!File.Exists(sourceFilename))
            {
                // todo: logging
                return null;
            }

            return File.ReadAllBytes(sourceFilename);
        }

        public bool SendFile(string targetFilename, byte[] fileData)
        {
            // Create target folder, if needed.
            Directory.CreateDirectory(Path.GetDirectoryName(targetFilename));

            File.WriteAllBytes(targetFilename, fileData);

            return true;
        }
    }

    public class FTFService : IMicroService
    {
        private static FTFService _singleton = null;
        private static readonly object _constructorLock = new object();

        private TestManager_Server _testExecutionManager;
        private IMicroServiceController _controller;
        public ILogger<FTFService> ServiceLogger;
        private System.Threading.CancellationTokenSource _ipcCancellationToken;
        private readonly string _serviceRegPath = @"System\CurrentControlSet\Control\FactoryTestFramework";
        private readonly string _loopbackValue = @"UWPLocalLoopbackEnabled";
        //private readonly string _firewallValue = @"FirewallConfigured";
        private RegistryKey _ftfKey = null;

        public Dictionary<ulong, ServiceEvent> ServiceEvents { get; }
        public ulong LastEventIndex { get; private set; }
        public DateTime LastEventTime { get; private set; }

        /// <summary>
        /// FTFService singleton
        /// </summary>
        public static FTFService Instance
        {
            get
            {
                return _singleton;
            }
        }

        /// <summary>
        /// Returns build number of FTFService.
        /// </summary>
        /// <returns></returns>
        public static string GetServiceVersionString()
        {
            var assembly = Assembly.GetExecutingAssembly();
            string assemblyVersion = assembly.GetName().Version.ToString();
            object[] attributes = assembly.GetCustomAttributes(true);

            string description = "";

            var descrAttr = attributes.OfType<AssemblyDescriptionAttribute>().FirstOrDefault();
            if (descrAttr != null)
            {
                description = descrAttr.Description;
            }

#if DEBUG
            description = "Debug" + description;
#endif

            return $"{assemblyVersion} ({description})";
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
                    ServiceEvents = new Dictionary<ulong, ServiceEvent>();
                    LastEventIndex = 0;
                    LastEventTime = DateTime.MinValue;

                    if (Environment.GetEnvironmentVariable("OSDataDrive") != null)
                    {
                        // Save logs to DATA if on a WCOS system
                        _testExecutionManager = new TestManager_Server(@"U:\FTFLogs");
                    }
                    else
                    {
                        // Otherwise save them next to the FTFService.exe
                        _testExecutionManager = new TestManager_Server(Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "FTFLogs")); ;
                    }
                }
                else
                {
                    throw new Exception("FTFService already created! Only one instance allowed.");
                }
            }
        }

        public TestManager_Server TestExecutionManager { get => _testExecutionManager; }
        
        /// <summary>
        /// Service start.
        /// </summary>
        public void Start()
        {
            // Try to load known TestLists from the state file
            if (File.Exists(_testExecutionManager.TestListStateFile))
            {
                try
                {
                    _testExecutionManager.LoadTestListsFromXmlFile(_testExecutionManager.TestListStateFile);
                }
                catch (Exception e)
                {
                    ServiceLogger.LogWarning($"FactoryTestFramework Service could not load {_testExecutionManager.TestListStateFile}\n {e.Message}");
                }
            }


            // Execute "first run" tasks. They do nothing if already run, but might need to run every boot on a state separated WCOS image.
            ExecuteServerBootTasks();

            // Start IPC server
            _ipcCancellationToken = new System.Threading.CancellationTokenSource();
            _testExecutionManager.OnTestManagerEvent += HandleTestManagerEvent;
            FTFServiceExe.ipcHost.RunAsync(_ipcCancellationToken.Token);

            // Execute user defined tasks.
            Task.Run(ExecuteUserBootTasks);

            ServiceLogger.LogTrace("FactoryTestFramework Service Started\n");
        }

        /// <summary>
        /// Service stop.
        /// </summary>
        public void Stop()
        {
            _ipcCancellationToken.Cancel();

            // Update state file
            TestExecutionManager.SaveAllTestListsToXmlFile(TestExecutionManager.TestListStateFile);

            ServiceLogger.LogTrace("FactoryTestFramework Service Stopped\n");
        }

        private void HandleTestManagerEvent(object source, TestManagerEventArgs e)
        {
            ServiceEvent serviceEvent = null;
            switch (e.Event)
            {
                case TestManagerEventType.WaitingForExternalTestRunResult:
                    serviceEvent = new ServiceEvent(ServiceEventType.WaitingForTestRunByClient, e.Guid, $"TestRun {e.Guid} is waiting on an external result.");
                    break;
                case TestManagerEventType.ExternalTestRunFinished:
                    serviceEvent = new ServiceEvent(ServiceEventType.WaitingForTestRunByClient, e.Guid, $"TestRun {e.Guid} received an external result and is finished.");
                    break;
                default:
                    break;
            }

            if (serviceEvent != null)
            {
                LogServiceEvent(serviceEvent);
            }
        }

        public void LogServiceEvent(ServiceEvent serviceEvent)
        {
            ServiceEvents.Add(serviceEvent.EventIndex, serviceEvent);
            LastEventIndex = serviceEvent.EventIndex;
            LastEventTime = serviceEvent.EventTime;
            ServiceLogger.LogInformation($"{serviceEvent.EventTime}: {serviceEvent.ServiceEventType} - {serviceEvent.Message}");
        }

        /// <summary>
        /// Executes server required tasks that should run on first boot (of FTF) or every boot.
        /// </summary>
        /// <returns></returns>
        public bool ExecuteServerBootTasks()
        {
            return EnableUWPLocalLoopback();
        }

        /// <summary>
        /// Executes user defined tasks that should run on first boot (of FTF) or every boot.
        /// </summary>
        /// <returns></returns>
        public void ExecuteUserBootTasks()
        {
            List<string> everyBootTasks = new List<string>();
            List<TestRun_Server> everyBootRuns = new List<TestRun_Server>();
            List<string> firstBootTasks = new List<string>();
            List<TestRun_Server> firstBootRuns = new List<TestRun_Server>();

            foreach (var taskString in firstBootTasks)
            {
                var command = taskString.Split(" ")[0];
                var args = taskString.Replace(command, "");
                firstBootRuns.Add((TestRun_Server)TestExecutionManager.RunExecutableOutsideTestList(@"%systemroot%\system32\cmd.exe", "/C \"checknetisolation loopbackexempt -a -n=Microsoft.FactoryTestFrameworkUWP_8wekyb3d8bbwe\"", Path.Combine(TestExecutionManager.DefaultLogFolder, ));
            }
        }


        /// <summary>
        /// Check if UWP local loopback needs to be enabled. Turn it on if so.
        /// </summary>
        /// <returns></returns>
        private bool EnableUWPLocalLoopback()
        {
            try
            {
                if (_ftfKey == null)
                {
                    OpenOrCreateRegKey();
                }

                // Check if this was successfully run once
                var value = (int)_ftfKey.GetValue(_loopbackValue, 0);

                if (value != 1)
                {
                    // Run localloopback command for both "official" and "DEV" apps
                    var runDev = (TestRun_Server)TestExecutionManager.RunExecutableOutsideTestList(@"%systemroot%\system32\cmd.exe", "/C \"checknetisolation loopbackexempt -a -n=Microsoft.FactoryTestFrameworkUWP.DEV_8wekyb3d8bbwe\"");
                    var runOfficial = (TestRun_Server)TestExecutionManager.RunExecutableOutsideTestList(@"%systemroot%\system32\cmd.exe", "/C \"checknetisolation loopbackexempt -a -n=Microsoft.FactoryTestFrameworkUWP_8wekyb3d8bbwe\"");

                    // Wait 2 seconds for both processes to start
                    int waitCount = 0;
                    const int waitMS = 100;
                    const int maxWaits = waitMS * 20; // 2 seconds

                    while (((runDev.TimeStarted == null) || (runOfficial.TimeStarted == null)) && (waitCount < maxWaits))
                    {
                        waitCount++;
                        System.Threading.Thread.Sleep(100);
                    }

                    if ((runDev.TimeStarted == null) || (runOfficial.TimeStarted == null))
                    {
                        throw new Exception($"checknetisolation never started");
                    }

                    // Wait 5 seconds for both process to exit
                    if ((!runDev.OwningTestRunner.WaitForExit(5000)) || (!runOfficial.OwningTestRunner.WaitForExit(5000)))
                    {
                        runDev.OwningTestRunner.StopTest();
                        runOfficial.OwningTestRunner.StopTest();
                        throw new Exception("checknetisolation did not exit after 5 seconds!");
                    }

                    if ((runDev.TestStatus == TestStatus.TestPassed) && (runOfficial.TestStatus == TestStatus.TestPassed))
                    {
                        // Success! Set regkey so this isn't run again.
                        _ftfKey.SetValue(_loopbackValue, 1, RegistryValueKind.DWord);
                        return true;
                    }
                    else
                    {
                        throw new Exception($"checknetisolation exited with {runDev.ExitCode} and {runOfficial.ExitCode}");
                    }
                }
            }
            catch (Exception e)
            {
                ServiceLogger.LogError($"Unable to enable UWP local loopback! You may not be able to use the FTF UWP app locally ({e.Message})");
            }

            return false;
        }

        private void OpenOrCreateRegKey()
        {
            _ftfKey = Registry.LocalMachine.CreateSubKey(_serviceRegPath, true);
        }

        // Firewall is configured in FTFServiceTemplate.wm.xml Windows Manifest file
        //private bool AddFirewallRules()
        //{
        //    try
        //    {
        //        if (_ftfKey == null)
        //        {
        //            OpenOrCreateRegKey();
        //        }

        //        var value = (int)_ftfKey.GetValue(_firewallValue, 0);

        //        if (value == 0)
        //        {
        //            // Run firewall commands
        //       //     netsh advfirewall firewall add rule name = ftfservice_tcp_in program =< Path to FTFService.exe > protocol = tcp dir =in enable = yes action = allow profile =public,private,domain

        //     //netsh advfirewall firewall add rule name=ftfservice_tcp_out program =< Path to FTFService.exe> protocol= tcp dir=out enable= yes action= allow profile=public,private,domain

        //            _ftfKey.SetValue(_firewallValue, 1, RegistryValueKind.DWord);
        //            return true;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        ServiceLogger.LogError($"Unable to create FTFService firewall rules! You may not be able to use the FTF UWP app over the network ({e.Message})");
        //    }

        //    return false;
        //}
    }
}