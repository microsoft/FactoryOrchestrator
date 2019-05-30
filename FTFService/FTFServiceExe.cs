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

        public bool SetDefaultLogFolder(string logFolder, bool moveExistingLogs)
        {
            return FTFService.Instance.TestExecutionManager.SetDefaultLogFolder(logFolder, moveExistingLogs);
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

        public bool RunTestList(Guid TestListToRun)
        {
            return FTFService.Instance.TestExecutionManager.RunTestList(TestListToRun);
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
        enum RegKeyType
        {
            NonMutable,
            Mutable,
            Volatile
        }

        private static FTFService _singleton = null;
        private static readonly object _constructorLock = new object();

        private TestManager_Server _testExecutionManager;
        private IMicroServiceController _controller;
        public ILogger<FTFService> ServiceLogger;
        private System.Threading.CancellationTokenSource _ipcCancellationToken;
        private readonly string _nonMutableServiceRegKey = @"SYSTEM\CurrentControlSet\Control\FactoryTestFramework";
        private readonly string _mutableServiceRegKey = @"OSDATA\CurrentControlSet\Control\FactoryTestFramework";
        private readonly string _volatileServiceRegKey = @"SYSTEM\CurrentControlSet\Control\FactoryTestFramework\EveryBootTaskStatus";
        private readonly string _firstBootCompleteValue = @"FirstBootTestListsComplete";
        private readonly string _everyBootCompleteValue = @"EveryBootTestListsComplete";
        private readonly string _loopbackValue = @"UWPLocalLoopbackEnabled";
        private readonly string _firstBootTasksPathValue = @"FirstBootTestListsXML";
        private readonly string _everyBootTasksPathValue = @"EveryBootTestListsXML";

        //private readonly string _firewallValue = @"FirewallConfigured";
        //private RegistryKey _ftfPersistentKey = null;
        //private RegistryKey _ftfVolatileKey = null;

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
            // Execute "first run" tasks. They do nothing if already run, but might need to run every boot on a state separated WCOS image.
            ExecuteServerBootTasks();

            // Execute user defined tasks.
            ExecuteUserBootTasks();

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

            // Start IPC server. Only start after all boot tasks are complete.
            _ipcCancellationToken = new System.Threading.CancellationTokenSource();
            _testExecutionManager.OnTestManagerEvent += HandleTestManagerEvent;
            FTFServiceExe.ipcHost.RunAsync(_ipcCancellationToken.Token);

            ServiceLogger.LogTrace("FactoryTestFramework Service Started\n");
        }

        /// <summary>
        /// Service stop.
        /// </summary>
        public void Stop()
        {
            // Disable IPC interface
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
            // Enable local loopback every boot.
            return EnableUWPLocalLoopback();
        }

        /// <summary>
        /// Executes user defined tasks that should run on first boot (of FTF) or every boot.
        /// </summary>
        /// <returns></returns>
        public void ExecuteUserBootTasks()
        {
            RegistryKey mutableKey = null;
            RegistryKey nonMutableKey = null;
            RegistryKey volatileKey = null;
            bool firstBootTasksFailed = false;
            bool everyBootTasksFailed = false;
            var logFolder = _testExecutionManager.DefaultLogFolder;

            try
            {
                // OSDATA wont exist on Desktop, so try to open it on it's own
                mutableKey = OpenOrCreateRegKey(RegKeyType.Mutable);
            }
            catch (Exception)
            { }

            // First Boot tasks
            try
            {
                // Open remaining reg keys
                nonMutableKey = OpenOrCreateRegKey(RegKeyType.NonMutable);
                volatileKey = OpenOrCreateRegKey(RegKeyType.Volatile);

                // Check if first boot tasks were already completed
                bool? firstBootTasksCompleted = GetValueFromRegistry(mutableKey, nonMutableKey, _firstBootCompleteValue) as bool?;

                if ((firstBootTasksCompleted == null) || (firstBootTasksCompleted == false))
                {
                    ServiceLogger.LogInformation("Checking for first boot TestLists XML...");
                    // Find the TestLists XML path.
                    string firstBootTestListPath = GetValueFromRegistry(mutableKey, nonMutableKey, _firstBootTasksPathValue) as string;

                    if (firstBootTestListPath != null)
                    {
                        ServiceLogger.LogInformation($"First boot TestLists XML found, attempting to load {firstBootTestListPath}...");
                        // Create a new directory for the first boot logs
                        _testExecutionManager.SetDefaultLogFolder(Path.Combine(logFolder, "FirstBootTestLists"), false);

                        // Load the TestLists file specified in registry
                        var firstBootTestListGuids = _testExecutionManager.LoadTestListsFromXmlFile(firstBootTestListPath);

                        foreach (var listGuid in firstBootTestListGuids)
                        {
                            if (!_testExecutionManager.RunTestList(listGuid))
                            {
                                ServiceLogger.LogError($"Unable to run first boot TestList {listGuid}!");
                            }
                            else
                            {
                                ServiceLogger.LogInformation($"Running first boot TestList {listGuid}...");
                            }
                        }
                    }
                }
                else
                {
                    ServiceLogger.LogInformation("First boot TestLists already complete.");
                }
            }
            catch (Exception e)
            {
                ServiceLogger.LogError($"Unable to complete first boot TestLists! ({e.Message})");
                firstBootTasksFailed = true;
            }

            // Wait for all first boot tasks to complete
            while (_testExecutionManager.IsTestListRunning)
            {
                System.Threading.Thread.Sleep(1000);
            }

            // Every boot tasks
            if ((nonMutableKey != null) && (volatileKey != null))
            {
                try
                {
                    // Check if every boot tasks were already completed
                    var everyBootTasksCompleted = volatileKey.GetValue(_everyBootCompleteValue) as bool?;

                    if ((everyBootTasksCompleted == null) || (everyBootTasksCompleted == false))
                    {
                        ServiceLogger.LogInformation($"Checking for every boot TestLists XML...");
                        // Find the TestLists XML path.
                        var everyBootTestListPath = GetValueFromRegistry(mutableKey, nonMutableKey, _everyBootTasksPathValue) as string;

                        if (everyBootTestListPath != null)
                        {
                            ServiceLogger.LogInformation($"Every boot TestLists XML found, attempting to load {everyBootTestListPath}...");

                            // Create a new directory for the first boot logs
                            _testExecutionManager.SetDefaultLogFolder(Path.Combine(logFolder, "EveryBootTestLists"), false);

                            // Load the TestLists file specified in registry
                            var everyBootTestListGuids = _testExecutionManager.LoadTestListsFromXmlFile(everyBootTestListPath);

                            foreach (var listGuid in everyBootTestListGuids)
                            {
                                if (!_testExecutionManager.RunTestList(listGuid))
                                {
                                    ServiceLogger.LogError($"Unable to run every boot TestList {listGuid}!");
                                }
                                else
                                {
                                    ServiceLogger.LogInformation($"Running every boot TestList {listGuid}...");
                                }
                            }
                        }
                    }
                    else
                    {
                        ServiceLogger.LogInformation("Every boot TestLists already complete.");
                    }
                }
                catch (Exception e)
                {
                    ServiceLogger.LogError($"Unable to complete every boot TestLists! ({e.Message})");
                    everyBootTasksFailed = true;
                }
            }

            // Wait for all tasks to complete
            while (_testExecutionManager.IsTestListRunning)
            {
                System.Threading.Thread.Sleep(1000);
            }

            if (!firstBootTasksFailed)
            {
                // Mark first boot tasks as complete
                ServiceLogger.LogInformation("First boot TestLists complete or not found.");
                SetValueInRegistry(mutableKey, nonMutableKey, _firstBootCompleteValue, 1, RegistryValueKind.DWord);
            }
            if (!everyBootTasksFailed)
            {
                // Mark every boot tasks as complete. Mark in volatile registry location so it is reset after reboot.
                ServiceLogger.LogInformation("Every boot TestLists complete or not found.");
                volatileKey.SetValue(_everyBootCompleteValue, 1, RegistryValueKind.DWord);
            }

            if (volatileKey != null)
            {
                volatileKey.Close();
            }
            if (mutableKey != null)
            {
                mutableKey.Close();
            }
            if (nonMutableKey != null)
            {
                nonMutableKey.Close();
            }

            // Reset server state, clearing the first boot and first run testlists.
            _testExecutionManager.SetDefaultLogFolder(logFolder, false);
            _testExecutionManager.Reset();
        }

        /// <summary>
        /// Checks the given mutable and non-mutable registry keys for a given value. Mutable is always checked first.
        /// </summary>
        /// <returns>The value if it exists.</returns>
        private object GetValueFromRegistry(RegistryKey mutableKey, RegistryKey nonMutableKey, string valueName)
        {
            object ret = null;

            if (mutableKey != null)
            {
                ret = mutableKey.GetValue(valueName);
            }
            
            if ((ret == null) && (nonMutableKey != null))
            {
                ret = nonMutableKey.GetValue(valueName);
            }

            return ret;
        }

        /// <summary>
        /// Sets a given value in the registry. The mutable location is used if it exists.
        /// </summary>
        private void SetValueInRegistry(RegistryKey mutableKey, RegistryKey nonMutableKey, string valueName, object value, RegistryValueKind valueKind)
        {
            if (mutableKey != null)
            {
                mutableKey.SetValue(valueName, value, valueKind);
            }
            else if (nonMutableKey != null)
            {
                nonMutableKey.SetValue(valueName, value, valueKind);
            }
        }

        /// <summary>
        /// Check if UWP local loopback needs to be enabled. Turn it on if so.
        /// </summary>
        /// <returns></returns>
        private bool EnableUWPLocalLoopback()
        {
            bool success = false;
            RegistryKey volatileKey = null;
            try
            {
                volatileKey = OpenOrCreateRegKey(RegKeyType.Volatile);

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
                    success = true;
                    volatileKey.SetValue(_loopbackValue, 1, RegistryValueKind.DWord);
                }
                else
                {
                    throw new Exception($"checknetisolation exited with {runDev.ExitCode} and {runOfficial.ExitCode}");
                }
            }
            catch (Exception e)
            {
                ServiceLogger.LogError($"Unable to enable UWP local loopback! You may not be able to use the FTF UWP app locally ({e.Message})");
            }
            finally
            {
                if (volatileKey != null)
                {
                    volatileKey.Close();
                }
            }

            return success;
        }

        private RegistryKey OpenOrCreateRegKey(RegKeyType type)
        {
            RegistryKey key = null;
            switch (type)
            {
                case RegKeyType.Mutable:
                    key = Registry.LocalMachine.CreateSubKey(_mutableServiceRegKey, true);
                    break;
                case RegKeyType.NonMutable:
                    key = Registry.LocalMachine.CreateSubKey(_nonMutableServiceRegKey, true);
                    break;
                case RegKeyType.Volatile:
                    key = Registry.LocalMachine.CreateSubKey(_volatileServiceRegKey, true, RegistryOptions.Volatile);
                    break;
                default:
                    break;
            }

            return key;
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