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
using Microsoft.FactoryOrchestrator.Core;
using Microsoft.FactoryOrchestrator.Server;
using System.Reflection;
using System.Linq;
using Microsoft.Win32;
using Microsoft.Extensions.PlatformAbstractions;
using System.IO;
using Windows.Management.Deployment;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using TaskStatus = Microsoft.FactoryOrchestrator.Core.TaskStatus;
using JKang.IpcServiceFramework.Tcp;

namespace Microsoft.FactoryOrchestrator.Service
{
    class FOServiceExe
    {
        public static IIpcServiceHost ipcHost;
        public static ServiceProvider ipcSvcProvider;

        public static void Main(string[] args)
        {
#if DEBUG
            var _logLevel = LogLevel.Debug;
#else
    var _logLevel = LogLevel.Information;
#endif
            // Create service collection
            var servicesIpc = new ServiceCollection();
            var services = new ServiceCollection();


            // Configure IPC service framework server
            servicesIpc = (ServiceCollection)servicesIpc.AddIpc(builder =>
            {
                builder
                    .AddTcp()
                    .AddService<IFactoryOrchestratorService, FOCommunicationHandler>();
            });

            // Configure service providers for logger creation and managment
            ipcSvcProvider = servicesIpc
                .AddLogging(builder =>
                {
                    // Only log IPC framework errors
                    builder
                    .SetMinimumLevel(LogLevel.Error).AddConsole().AddProvider(new LogFileProvider());

                })
                .AddOptions()
                .AddSingleton(new LoggerFactory())
                .BuildServiceProvider();

            ServiceProvider foSvcProvider = services
                .AddLogging(builder =>
                {
                    // Log level based on DEBUG ifdef
                    builder
                    .SetMinimumLevel(_logLevel).AddConsole().AddProvider(new LogFileProvider());

                })
                .AddOptions()
                .AddSingleton(new LoggerFactory())
                .BuildServiceProvider();

            var _logger = foSvcProvider.GetRequiredService<ILoggerFactory>().CreateLogger<FOServiceExe>();

            // FactoryOrchestratorService handler
            ServiceRunner<FOService>.Run(config =>
            {
                var name = config.GetDefaultName();
                config.Service(serviceConfig =>
                {
                    serviceConfig.ServiceFactory((extraArguments, controller) =>
                    {
                        return new FOService(controller, foSvcProvider.GetRequiredService<ILoggerFactory>().CreateLogger<FOService>());
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
                        if (FOService.Instance != null)
                        {
                            _logger.LogError(e, string.Format("Service {0} errored with exception", name));
                        }
                        else
                        {
                            FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, $"Service {name} errored with exception {e.AllExceptionsToString()}"));
                        }
                    });
                });
            });

            // Dispose of loggers, this needs to be done manually
            ipcSvcProvider.GetService<ILoggerFactory>().Dispose();
            foSvcProvider.GetService<ILoggerFactory>().Dispose();
        }
    }

    // Find the FactoryOrchestratorService singleton -> pass call to it
    public class FOCommunicationHandler : IFactoryOrchestratorService
    {
        // TODO: Logging: Catch exceptions & log them
        public List<ServiceEvent> GetServiceEvents()
        {
            return FOService.Instance.ServiceEvents.Values.ToList();
        }

        public List<ServiceEvent> GetServiceEvents(DateTime timeLastChecked)
        {
            if (timeLastChecked < FOService.Instance.LastEventTime)
            {
                return FOService.Instance.ServiceEvents.Values.Where(x => x.EventTime > timeLastChecked).ToList();
            }
            else
            {
                return new List<ServiceEvent>();
            }
        }

        public List<ServiceEvent> GetServiceEvents(ulong lastEventIndex)
        {
            if (lastEventIndex < FOService.Instance.LastEventIndex)
            {
                return FOService.Instance.ServiceEvents.Where(x => x.Key > lastEventIndex).Select(x => x.Value).ToList();
            }
            else
            {
                return new List<ServiceEvent>();
            }
        }
        public ServiceEvent GetLastServiceError()
        {
            return FOService.Instance.ServiceEvents.Values.Where(x => x.ServiceEventType == ServiceEventType.ServiceError).DefaultIfEmpty(null).LastOrDefault();
        }

        public TaskList CreateTaskListFromDirectory(string path, bool onlyTAEF)
        {
            FOService.Instance.ServiceLogger.LogDebug($"Start: CreateTaskListFromDirectory {path}");
            TaskList tl = null;

            try
            {
                tl = FOService.Instance.TestExecutionManager.CreateTaskListFromDirectory(path, onlyTAEF);
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
            }

            FOService.Instance.ServiceLogger.LogDebug($"Finish: CreateTaskListFromDirectory {path}");
            return tl;
        }

        public List<Guid> LoadTaskListsFromXmlFile(string filename)
        {
            FOService.Instance.ServiceLogger.LogDebug($"Start: LoadTaskListsFromXmlFile {filename}");
            List<Guid> taskLists = null;

            try
            {
                taskLists = FOService.Instance.TestExecutionManager.LoadTaskListsFromXmlFile(filename);
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
            }

            FOService.Instance.ServiceLogger.LogDebug($"Finish: LoadTaskListsFromXmlFile {filename}");
            return taskLists;
        }

        public TaskList CreateTaskListFromTaskList(TaskList list)
        {
            FOService.Instance.ServiceLogger.LogDebug($"Start: CreateTaskListFromTaskList {list.Guid}");
            var serverList = FOService.Instance.TestExecutionManager.CreateTaskListFromTaskList(list);
            FOService.Instance.ServiceLogger.LogDebug($"Finish: CreateTaskListFromTaskList {list.Guid}");
            return serverList;
        }

        public bool SaveTaskListToXmlFile(Guid guid, string filename)
        {
            FOService.Instance.ServiceLogger.LogDebug($"Start: SaveTaskListToXmlFile {guid} {filename}");
            var saved = FOService.Instance.TestExecutionManager.SaveTaskListToXmlFile(guid, filename);
            FOService.Instance.ServiceLogger.LogDebug($"Finish: SaveTaskListToXmlFile {guid} {filename}");
            return saved;
        }

        public bool SaveAllTaskListsToXmlFile(string filename)
        {
            FOService.Instance.ServiceLogger.LogDebug($"Start: SaveAllTaskListsToXmlFile {filename}");
            var saved = FOService.Instance.TestExecutionManager.SaveAllTaskListsToXmlFile(filename);
            FOService.Instance.ServiceLogger.LogDebug($"Finish: SaveAllTaskListsToXmlFile {filename}");
            return saved;
        }

        public List<Guid> GetTaskListGuids()
        {
            FOService.Instance.ServiceLogger.LogDebug($"Start: GetTaskListGuids");
            var guids = FOService.Instance.TestExecutionManager.GetTaskListGuids();
            FOService.Instance.ServiceLogger.LogDebug($"Finish: GetTaskListGuids");
            return guids;
        }

        public List<TaskListSummary> GetTaskListSummaries()
        {
            FOService.Instance.ServiceLogger.LogDebug($"Start: GetTaskListSummaries");
            var guids = FOService.Instance.TestExecutionManager.GetTaskListGuids();
            var ret = new List<TaskListSummary>();
            foreach (var guid in guids)
            {
                var list = FOService.Instance.TestExecutionManager.GetTaskList(guid);
                if (list != null)
                {
                    ret.Add(new TaskListSummary(guid, list.Name, list.TaskListStatus, list.RunInParallel, list.AllowOtherTaskListsToRun, list.TerminateBackgroundTasksOnCompletion));
                }
            }

            FOService.Instance.ServiceLogger.LogDebug($"Start: GetTaskListSummaries");
            return ret;
        }

        public TaskList QueryTaskList(Guid taskListGuid)
        {
            FOService.Instance.ServiceLogger.LogDebug($"Start: QueryTaskList {taskListGuid}");
            var list = FOService.Instance.TestExecutionManager.GetTaskList(taskListGuid);
            FOService.Instance.ServiceLogger.LogDebug($"Finish: QueryTaskList {taskListGuid}");
            return list;
        }
        public TaskBase QueryTask(Guid taskGuid)
        {
            FOService.Instance.ServiceLogger.LogDebug($"Start: QueryTask {taskGuid}");
            var task = FOService.Instance.TestExecutionManager.GetTask(taskGuid);
            FOService.Instance.ServiceLogger.LogDebug($"Finish: QueryTask {taskGuid}");
            return task;
        }

        public bool DeleteTaskList(Guid listToDelete)
        {
            FOService.Instance.ServiceLogger.LogDebug($"Start: DeleteTaskList {listToDelete}");
            var deleted = FOService.Instance.TestExecutionManager.DeleteTaskList(listToDelete);
            FOService.Instance.ServiceLogger.LogDebug($"Finish: DeleteTaskList {listToDelete}");
            return deleted;
        }

        public void ResetService(bool preserveLogs, bool factoryReset)
        {
            // Kill all processes including bg tasks, delete all state except registry configuration
            FOService.Instance.TestExecutionManager.Reset(preserveLogs, true);

            if (factoryReset)
            {
                // Pause a bit to allow the IPC call to return before we kill it off
                Task.Run(() => { System.Threading.Thread.Sleep(100); FOService.Instance.Stop(); FOService.Instance.Start(true); });
            }
        }

        public bool UpdateTask(TaskBase updatedTask)
        {
            FOService.Instance.ServiceLogger.LogDebug($"Start: UpdateTask {updatedTask.Name} {updatedTask.Guid}");
            var updated = FOService.Instance.TestExecutionManager.UpdateTask(updatedTask);
            FOService.Instance.ServiceLogger.LogDebug($"Finish: UpdateTask {updatedTask.Name} {updatedTask.Guid}");
            return updated;
        }

        public bool UpdateTaskList(TaskList taskList)
        {
            FOService.Instance.ServiceLogger.LogDebug($"Start: UpdateTaskList {taskList.Guid}");
            var updated = FOService.Instance.TestExecutionManager.UpdateTaskList(taskList);
            FOService.Instance.ServiceLogger.LogDebug($"Finish: UpdateTaskList {taskList.Guid}");
            return updated;
        }

        public bool SetDefaultTePath(string teExePath)
        {
            FOService.Instance.ServiceLogger.LogDebug($"Start: SetDefaultTePath {teExePath}");
            bool success = false;
            try
            {
                TaskRunner.GlobalTeExePath = teExePath;
                success = true;
            }
            catch (Exception)
            {}

            FOService.Instance.ServiceLogger.LogDebug($"Finish: SetDefaultTePath {teExePath}");

            return success;
        }

        public string GetLogFolder()
        {
            FOService.Instance.ServiceLogger.LogDebug($"Start: GetLogFolder");
            var folder = FOService.Instance.TestExecutionManager.LogFolder;
            FOService.Instance.ServiceLogger.LogDebug($"Finish: GetLogFolder");
            return folder;
        }

        public bool SetLogFolder(string logFolder, bool moveExistingLogs)
        {
            FOService.Instance.ServiceLogger.LogDebug($"Start: SetLogFolder {logFolder} move existing logs = {moveExistingLogs}");
            var updated = FOService.Instance.TestExecutionManager.SetLogFolder(logFolder, moveExistingLogs);
            FOService.Instance.ServiceLogger.LogDebug($"Finish: SetLogFolder {logFolder} move existing logs = {moveExistingLogs}");
            return updated;
        }

        public void AbortAll()
        {
            FOService.Instance.ServiceLogger.LogDebug($"Start: AbortAllTaskLists");
            FOService.Instance.TestExecutionManager.AbortAll();
            FOService.Instance.ServiceLogger.LogDebug($"Finish: AbortAllTaskLists");
        }

        public void AbortTaskList(Guid taskListGuid)
        {
            FOService.Instance.ServiceLogger.LogDebug($"Start: AbortTaskList {taskListGuid}");
            FOService.Instance.TestExecutionManager.AbortTaskList(taskListGuid);
            FOService.Instance.ServiceLogger.LogDebug($"Finish: AbortTaskList {taskListGuid}");
        }
        public void AbortTaskRun(Guid taskRunGuid)
        {
            FOService.Instance.ServiceLogger.LogDebug($"Start: AbortTaskRun {taskRunGuid}");
            FOService.Instance.TestExecutionManager.AbortTaskRun(taskRunGuid);
            FOService.Instance.ServiceLogger.LogDebug($"Finish: AbortTaskRun {taskRunGuid}");
        }

        public string GetServiceVersionString()
        {
            FOService.Instance.ServiceLogger.LogDebug($"Start: GetServiceVersionString");
            var version = FOService.GetServiceVersionString();
            FOService.Instance.ServiceLogger.LogDebug($"Finish: GetServiceVersionString");
            return version;
        }

        public TaskRun QueryTaskRun(Guid taskRunGuid)
        {
            FOService.Instance.ServiceLogger.LogDebug($"Start: QueryTaskRun {taskRunGuid}");
            var run = TaskRun_Server.GetTaskRunByGuid(taskRunGuid);
            TaskRun ret = null;
            if (run != null)
            {
                ret = run.DeepCopy();
            }
            else
            {
                FOService.Instance.ServiceLogger.LogDebug($"QueryTaskRun {taskRunGuid}, run not known to server, seeing if we can load it from the log file...");

                var files = Directory.EnumerateFiles(FOService.Instance.TestExecutionManager.LogFolder, $"*{taskRunGuid.ToString()}*", SearchOption.AllDirectories);
                if (files.Count() == 1)
                {
                    ret = FOService.Instance.TestExecutionManager.LoadTaskRunFromFile(files.First());
                }
            }

            FOService.Instance.ServiceLogger.LogDebug($"Finish: QueryTaskRun {taskRunGuid}");
            return ret;
        }

        public bool UpdateTaskRun(TaskRun taskRun)
        {
            FOService.Instance.ServiceLogger.LogDebug($"Start: UpdateTaskRun {taskRun.Guid}");
            var updated = FOService.Instance.TestExecutionManager.UpdateTaskRun(taskRun);
            FOService.Instance.ServiceLogger.LogDebug($"Finish: UpdateTaskRun {taskRun.Guid}");
            return updated;
        }

        public bool RunTaskList(Guid taskListToRun, int initialTaskIndex)
        {
            FOService.Instance.ServiceLogger.LogDebug($"Start: RunTaskList {taskListToRun}, start index: {initialTaskIndex}");
            var ran = FOService.Instance.TestExecutionManager.RunTaskListFromInitial(taskListToRun, initialTaskIndex);
            FOService.Instance.ServiceLogger.LogDebug($"Finish: RunTaskList {taskListToRun}, start index: {initialTaskIndex}");
            return ran;
        }

        public TaskRun RunExecutable(string exeFilePath, string arguments, string logFilePath = null)
        {
            FOService.Instance.ServiceLogger.LogDebug($"Start: RunExecutable {exeFilePath} {arguments}");
            var run = FOService.Instance.TestExecutionManager.RunExecutableAsBackgroundTask(exeFilePath, arguments, logFilePath);
            FOService.Instance.ServiceLogger.LogDebug($"Finish: RunExecutable {exeFilePath} {arguments}");
            return run;
        }

        public TaskRun RunTask(Guid taskGuid)
        {
            FOService.Instance.ServiceLogger.LogDebug($"Start: RunTask {taskGuid}");
            var run = FOService.Instance.TestExecutionManager.RunTask(taskGuid);
            FOService.Instance.ServiceLogger.LogDebug($"Finish: RunTask {taskGuid}");
            return run;
        }

        public TaskRun RunTask(TaskBase task)
        {
            FOService.Instance.ServiceLogger.LogDebug($"Start: RunTask {task}");
            var run = FOService.Instance.TestExecutionManager.RunTask(task);
            FOService.Instance.ServiceLogger.LogDebug($"Finish: RunTask {task}");
            return run;
        }

        public TaskRun RunApp(string packageFamilyName)
        {
            FOService.Instance.ServiceLogger.LogDebug($"Start: RunApp {packageFamilyName}");
            var run = FOService.Instance.TestExecutionManager.RunApp(packageFamilyName);
            FOService.Instance.ServiceLogger.LogDebug($"Finish: RunApp {packageFamilyName}");
            return run;
        }

        public byte[] GetFile(string sourceFilename)
        {
            byte[] bytes = null;
            FOService.Instance.ServiceLogger.LogDebug($"Start: GetFile {sourceFilename}");

            if (!File.Exists(sourceFilename))
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, $"File {sourceFilename} requested by GetFile does not exist!"));
            }

            try
            {
                bytes = File.ReadAllBytes(sourceFilename);
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, $"File {sourceFilename} requested by GetFile could not be read! {e.AllExceptionsToString()} {e.HResult}"));
            }

            FOService.Instance.ServiceLogger.LogDebug($"Finish: GetFile {sourceFilename}");

            return bytes;
        }

        public bool SendFile(string targetFilename, byte[] fileData)
        {
            FOService.Instance.ServiceLogger.LogDebug($"Start: SendFile {targetFilename}");

            var result = false;
            try
            {
                // Create target folder, if needed.
                Directory.CreateDirectory(Path.GetDirectoryName(targetFilename));
                File.WriteAllBytes(targetFilename, fileData);
                result = true;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, $"File {targetFilename} for SendFile could not be saved! {e.AllExceptionsToString()} {e.HResult}"));
            }

            FOService.Instance.ServiceLogger.LogDebug($"Finish: SendFile {targetFilename}");

            return result;
        }

        public List<string> GetInstalledApps()
        {
            FOService.Instance.ServiceLogger.LogDebug($"Start: GetInstalledApps");
            var pkgManager = new PackageManager();
            // TODO: Bug: This should really check packages for the signed in user
            var packages = pkgManager.FindPackagesWithPackageTypes(PackageTypes.Main).ToList();
            var pfns = packages.Select(x => x.Id.FamilyName).ToList();
            FOService.Instance.ServiceLogger.LogDebug($"Finish: GetInstalledApps");
            return pfns;
        }

        public List<Tuple<string, string>> GetIpAddressesAndNicNames()
        {
            FOService.Instance.ServiceLogger.LogDebug($"Start: GetIpAddressesAndNicNames");
            List<Tuple<string, string>> ipAndNic = new List<Tuple<string, string>>();

            try
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces().Where(x => x.NetworkInterfaceType != NetworkInterfaceType.Loopback);
                foreach (var iface in interfaces)
                {
                    var props = iface.GetIPProperties();
                    foreach (var addr in props.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            ipAndNic.Add(new Tuple<string, string>(addr.Address.ToString(), iface.Name));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, $"GetIpAddressesAndNicNames() failed! {e.AllExceptionsToString()} {e.HResult}"));
            }

            FOService.Instance.ServiceLogger.LogDebug($"Finish: GetIpAddressesAndNicNames");
            return ipAndNic;
        }

        public List<string> GetDisabledPages()
        {
            List<string> ret = new List<string>();

            if (FOService.Instance.DisableCommandPromptPage)
            {
                // values must match "Tag" on MainPage.xaml
                ret.Add("console");
            }
            if (FOService.Instance.DisableUWPAppsPage)
            {
                ret.Add("apps");
            }
            if (FOService.Instance.DisableManageTasklistsPage)
            {
                ret.Add("save");
            }
            if (FOService.Instance.DisableFileTransferPage)
            {
                ret.Add("files");
            }

            return ret;
        }
    }

    public class FOService : IMicroService
    {
        enum RegKeyType
        {
            NonMutable,
            Mutable,
            Volatile
        }

        private static FOService _singleton = null;
        private static readonly object _constructorLock = new object();

        private TaskManager_Server _taskExecutionManager;
        private IMicroServiceController _controller;
        public ILogger<FOService> ServiceLogger;
        private System.Threading.CancellationTokenSource _ipcCancellationToken;
        private readonly string _nonMutableServiceRegKey = @"SYSTEM\CurrentControlSet\Control\FactoryOrchestrator";
        private readonly string _mutableServiceRegKey = @"OSDATA\CurrentControlSet\Control\FactoryOrchestrator";
        private readonly string _volatileServiceRegKey = @"SYSTEM\CurrentControlSet\Control\FactoryOrchestrator\EveryBootTaskStatus";

        private readonly string _loopbackValue = @"UWPLocalLoopbackEnabled";

        // OEM Customization registry values
        private readonly string _disableNetworkAccessValue = @"DisableNetworkAccess";
        private readonly string _disableCmdPromptValue = @"DisableCommandPromptPage";
        private readonly string _disableUWPAppsValue = @"DisableUWPAppsPage";
        private readonly string _disableTaskManagerValue = @"DisableManageTasklistsPage";
        private readonly string _disableFileTransferValue = @"DisableFileTransferPage";

        // Default paths in testcontent directory for user tasklists
        private readonly string _firstBootStateDefaultPath = @"U:\TestContent\InitialTaskLists.xml";
        private readonly string _firstBootDefaultPath = @"U:\TestContent\FirstBootTasks.xml";
        private readonly string _everyBootDefaultPath = @"U:\TestContent\EveryBootTasks.xml";

        // Registry fallbacks for user tasklists
        private readonly string _firstBootTasksPathValue = @"FirstBootTaskListsXML";
        private readonly string _everyBootTasksPathValue = @"EveryBootTaskListsXML";
        private readonly string _firstBootStatePathValue = @"FirstBootStateTaskListsXML";

        // user tasklists state registry values
        private readonly string _firstBootCompleteValue = @"FirstBootTaskListsComplete";
        private readonly string _everyBootCompleteValue = @"EveryBootTaskListsComplete";
        private readonly string _firstBootStateLoadedValue = @"FirstBootStateLoaded";

        public Dictionary<ulong, ServiceEvent> ServiceEvents { get; }
        public ulong LastEventIndex { get; private set; }
        public DateTime LastEventTime { get; private set; }
        public bool DisableCommandPromptPage { get; private set; }
        public bool DisableUWPAppsPage { get; private set; }
        public bool DisableManageTasklistsPage { get; private set; }
        public bool DisableFileTransferPage { get; private set; }
        public bool DisableNetworkAccess { get; private set; }

        /// <summary>
        /// FactoryOrchestratorService singleton
        /// </summary>
        public static FOService Instance
        {
            get
            {
                return _singleton;
            }
        }

        /// <summary>
        /// Returns build number of FactoryOrchestratorService.
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

        public FOService(IMicroServiceController controller, ILogger<FOService> logger)
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
                        _taskExecutionManager = new TaskManager_Server(@"U:\FactoryOrchestratorLogs");
                    }
                    else
                    {
                        // Otherwise save them next to the FactoryOrchestratorService.exe
                        _taskExecutionManager = new TaskManager_Server(Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "FactoryOrchestratorLogs")); ;
                    }
                }
                else
                {
                    throw new Exception("FactoryOrchestratorService already created! Only one instance allowed.");
                }
            }
        }

        public TaskManager_Server TestExecutionManager { get => _taskExecutionManager; }
        
        /// <summary>
        /// Service start.
        /// </summary>
        public void Start()
        {
            Start(false);
        }

        /// <summary>
        /// Service start.
        /// </summary>
        public void Start(bool forceUserTaskRerun)
        {
            // Execute "first run" tasks. They do nothing if already run, but might need to run every boot on a state separated WCOS image.
            ExecuteServerBootTasks();

            // Execute user defined tasks.
            ExecuteUserBootTasks(forceUserTaskRerun);

            // Load first boot state file, or try to load known TaskLists from the existing state file.
            if (!LoadFirstBootStateFile(forceUserTaskRerun) && File.Exists(_taskExecutionManager.TaskListStateFile))
            {
                try
                {
                    _taskExecutionManager.LoadTaskListsFromXmlFile(_taskExecutionManager.TaskListStateFile);
                }
                catch (Exception e)
                {
                    ServiceLogger.LogWarning($"Factory Orchestrator Service could not load {_taskExecutionManager.TaskListStateFile}\n {e.AllExceptionsToString()}");
                }
            }

            // Start IPC server on port 45684. Only start after all boot tasks are complete.
            if (DisableNetworkAccess)
            {
                FOServiceExe.ipcHost = new IpcServiceHostBuilder(FOServiceExe.ipcSvcProvider).AddTcpEndpoint<IFactoryOrchestratorService>("tcp", IPAddress.Loopback, 45684)
                                                                .Build();
            }
            else
            {
                FOServiceExe.ipcHost = new IpcServiceHostBuilder(FOServiceExe.ipcSvcProvider).AddTcpEndpoint<IFactoryOrchestratorService>("tcp", IPAddress.Any, 45684)
                                                                .Build();
            }

            _ipcCancellationToken = new System.Threading.CancellationTokenSource();
            _taskExecutionManager.OnTestManagerEvent += HandleTestManagerEvent;
            FOServiceExe.ipcHost.RunAsync(_ipcCancellationToken.Token);

            ServiceLogger.LogInformation("Factory Orchestrator Service is ready to communicate with client(s)\n");
        }

        private bool LoadFirstBootStateFile(bool force)
        {
            RegistryKey mutableKey = null;
            RegistryKey nonMutableKey = null;
            bool loaded = false;

            try
            {
                // OSDATA wont exist on Desktop, so try to open it on it's own
                mutableKey = OpenOrCreateRegKey(RegKeyType.Mutable);
            }
            catch (Exception)
            { }

            try
            {
                // Open non mutable reg key
                nonMutableKey = OpenOrCreateRegKey(RegKeyType.NonMutable);

                var firstBootStateLoaded = GetValueFromRegistry(mutableKey, nonMutableKey, _firstBootStateLoadedValue) as int?;

                if ((firstBootStateLoaded == null) || (firstBootStateLoaded == 0) || (force))
                {
                    ServiceLogger.LogInformation("Checking for first boot state TaskLists XML...");
                    // Find the TaskLists XML path. Check testcontent directory for wellknown name, fallback to registry
                    string firstBootStateTaskListPath = null;
                    if (File.Exists(_firstBootStateDefaultPath))
                    {
                        firstBootStateTaskListPath = _firstBootStateDefaultPath;
                    }
                    else
                    {
                        firstBootStateTaskListPath = GetValueFromRegistry(mutableKey, nonMutableKey, _firstBootStatePathValue) as string;
                    }

                    if (firstBootStateTaskListPath != null)
                    {
                        ServiceLogger.LogInformation($"First boot state TaskLists XML found, attempting to load {firstBootStateTaskListPath}...");

                        // Load the TaskLists file specified in registry
                        var firstBootTaskListGuids = _taskExecutionManager.LoadTaskListsFromXmlFile(firstBootStateTaskListPath);

                        ServiceLogger.LogInformation($"Successfully loaded first boot state TaskLists XML {firstBootStateTaskListPath}...");
                    }
                    else
                    {
                        ServiceLogger.LogInformation("No first boot state TaskLists XML found.");
                    }

                    loaded = true;
                    SetValueInRegistry(mutableKey, nonMutableKey, _firstBootStateLoadedValue, 1, RegistryValueKind.DWord);
                }
            }
            catch (Exception e)
            {
                LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, $"Unable to load first boot state TaskLists XML! ({e.AllExceptionsToString()})"));
            }

            return loaded;
        }

        /// <summary>
        /// Service stop.
        /// </summary>
        public void Stop()
        {
            // Disable IPC interface
            _ipcCancellationToken.Cancel();

            // Abort everything that's running, except persisted background tasks
            TestExecutionManager.AbortAll();

            // Update state file
            TestExecutionManager.SaveAllTaskListsToXmlFile(TestExecutionManager.TaskListStateFile);

            ServiceLogger.LogDebug("Factory Orchestrator Service Stopped\n");
        }

        private void HandleTestManagerEvent(object source, TestManagerEventArgs e)
        {
            ServiceEvent serviceEvent = null;
            switch (e.Event)
            {
                case TestManagerEventType.WaitingForExternalTaskRunResult:
                    serviceEvent = new ServiceEvent(ServiceEventType.WaitingForExternalTaskRun, e.Guid, $"TaskRun {e.Guid} is waiting on an external result.");
                    break;
                case TestManagerEventType.ExternalTaskRunFinished:
                    serviceEvent = new ServiceEvent(ServiceEventType.DoneWaitingForExternalTaskRun, e.Guid, $"External TaskRun {e.Guid} received a result and is finished.");
                    break;
                case TestManagerEventType.ExternalTaskRunAborted:
                    serviceEvent = new ServiceEvent(ServiceEventType.DoneWaitingForExternalTaskRun, e.Guid, $"External TaskRun {e.Guid} was aborted by the user.");
                    break;
                case TestManagerEventType.ExternalTaskRunTimeout:
                    serviceEvent = new ServiceEvent(ServiceEventType.DoneWaitingForExternalTaskRun, e.Guid, $"External TaskRun {e.Guid} timed-out and is failed.");
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

            switch (serviceEvent.ServiceEventType)
            {
                case ServiceEventType.ServiceError:
                    ServiceLogger.LogError($"{serviceEvent.Message}");
                    break;
                default:
                    ServiceLogger.LogInformation($"{serviceEvent.ServiceEventType} - {serviceEvent.Message}");
                    break;
            }
        }

        /// <summary>
        /// Executes server required tasks that should run on first boot (of FactoryOrchestrator) or every boot.
        /// </summary>
        /// <returns></returns>
        public bool ExecuteServerBootTasks()
        {
            // Enable local loopback every boot.
            return LoadOEMCustomizations() && EnableUWPLocalLoopback();
        }

        /// <summary>
        /// Executes user defined tasks that should run on first boot (of FactoryOrchestrator) or every boot.
        /// </summary>
        /// <returns></returns>
        public void ExecuteUserBootTasks(bool force)
        {
            RegistryKey mutableKey = null;
            RegistryKey nonMutableKey = null;
            RegistryKey volatileKey = null;
            bool firstBootTasksFailed = false;
            bool everyBootTasksFailed = false;
            bool firstBootTasksExecuted = false;
            bool everyBootTasksExecuted = false;
            bool stateFileBackedup = false;
            var logFolder = _taskExecutionManager.LogFolder;
            var stateFileBackupPath = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), "FactoryOrchestratorTempTaskListStateFile");
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

                // Backup State File
                if (File.Exists(_taskExecutionManager.TaskListStateFile))
                {
                    File.Copy(_taskExecutionManager.TaskListStateFile, stateFileBackupPath, true);
                }
                stateFileBackedup = true;

                // Check if first boot tasks were already completed
                var firstBootTasksCompleted = GetValueFromRegistry(mutableKey, nonMutableKey, _firstBootCompleteValue) as int?;

                if ((firstBootTasksCompleted == null) || (firstBootTasksCompleted == 0) || (force == true))
                {
                    ServiceLogger.LogInformation("Checking for first boot TaskLists XML...");

                    // Find the TaskLists XML path. Check testcontent directory for wellknown name, fallback to registry
                    string firstBootTaskListPath = null;
                    if (File.Exists(_firstBootDefaultPath))
                    {
                        firstBootTaskListPath = _firstBootDefaultPath;
                    }
                    else
                    {
                        firstBootTaskListPath = GetValueFromRegistry(mutableKey, nonMutableKey, _firstBootTasksPathValue) as string;
                    }

                    if (firstBootTaskListPath != null)
                    {
                        firstBootTasksExecuted = true;

                        ServiceLogger.LogInformation($"First boot TaskLists XML found, attempting to load {firstBootTaskListPath}...");
                        // Create a new directory for the first boot logs
                        _taskExecutionManager.SetLogFolder(Path.Combine(logFolder, "FirstBootTaskLists"), false);

                        // Load the TaskLists file specified in registry
                        var firstBootTaskListGuids = _taskExecutionManager.LoadTaskListsFromXmlFile(firstBootTaskListPath);

                        foreach (var listGuid in firstBootTaskListGuids)
                        {
                            if (!_taskExecutionManager.RunTaskList(listGuid))
                            {
                                LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, $"Unable to run first boot TaskList {listGuid}"));
                            }
                            else
                            {
                                ServiceLogger.LogInformation($"Running first boot TaskList {listGuid}...");
                            }
                        }
                    }
                    else
                    {
                        ServiceLogger.LogInformation("No first boot TaskLists found.");
                    }
                }
                else
                {
                    ServiceLogger.LogInformation("First boot TaskLists already complete.");
                }
            }
            catch (Exception e)
            {
                LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, $"Unable to complete first boot TaskLists! ({e.AllExceptionsToString()})"));
                firstBootTasksFailed = true;
            }

            // Wait for all first boot tasks to complete
            int sleepCount = 0;
            while (_taskExecutionManager.IsTaskListRunning)
            {
                System.Threading.Thread.Sleep(1000);
                sleepCount++;
                if (sleepCount % 15 == 0)
                {
                    ServiceLogger.LogInformation("Waiting for first boot TaskLists to complete... (Mark tests as BackgroundTasks if you do not expect them to ever exit.)");
                }
            }

            // Every boot tasks
            if ((stateFileBackedup) && (nonMutableKey != null) && (volatileKey != null))
            {
                try
                {
                    // Check if every boot tasks were already completed
                    var everyBootTasksCompleted = volatileKey.GetValue(_everyBootCompleteValue) as int?;

                    if ((everyBootTasksCompleted == null) || (everyBootTasksCompleted == 0) || (force == true))
                    {
                        ServiceLogger.LogInformation($"Checking for every boot TaskLists XML...");
                        // Find the TaskLists XML path. Check testcontent directory for wellknown name, fallback to registry
                        string everyBootTaskListPath = null;
                        if (File.Exists(_everyBootDefaultPath))
                        {
                            everyBootTaskListPath = _everyBootDefaultPath;
                        }
                        else
                        {
                            everyBootTaskListPath = GetValueFromRegistry(mutableKey, nonMutableKey, _everyBootTasksPathValue) as string;
                        }

                        if (everyBootTaskListPath != null)
                        {
                            everyBootTasksExecuted = true;
                            ServiceLogger.LogInformation($"Every boot TaskLists XML found, attempting to load {everyBootTaskListPath}...");

                            // Create a new directory for the first boot logs
                            _taskExecutionManager.SetLogFolder(Path.Combine(logFolder, "EveryBootTaskLists"), false);

                            // Load the TaskLists file specified in registry
                            var everyBootTaskListGuids = _taskExecutionManager.LoadTaskListsFromXmlFile(everyBootTaskListPath);

                            foreach (var listGuid in everyBootTaskListGuids)
                            {
                                if (!_taskExecutionManager.RunTaskList(listGuid))
                                {
                                    LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, $"Unable to run every boot TaskList {listGuid}"));
                                }
                                else
                                {
                                    ServiceLogger.LogInformation($"Running every boot TaskList {listGuid}...");
                                }
                            }
                        }
                        else
                        {
                            ServiceLogger.LogInformation("No every boot TaskLists found.");
                        }
                    }
                    else
                    {
                        ServiceLogger.LogInformation("Every boot TaskLists already complete.");
                    }
                }
                catch (Exception e)
                {
                    LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, $"Unable to complete every boot TaskLists! ({e.AllExceptionsToString()})"));
                    everyBootTasksFailed = true;
                }
            }

            // Wait for all tasks to complete
            sleepCount = 0;
            while (_taskExecutionManager.IsTaskListRunning)
            {
                System.Threading.Thread.Sleep(1000);
                sleepCount++;
                if (sleepCount % 15 == 0)
                {
                    ServiceLogger.LogInformation("Waiting for every boot TaskLists to complete... (Mark tests as BackgroundTasks if you do not expect them to ever exit.)");
                }
            }

            if (!firstBootTasksFailed)
            {
                // Mark first boot tasks as complete
                if (firstBootTasksExecuted)
                {
                    ServiceLogger.LogInformation("First boot TaskLists complete.");
                }
                
                SetValueInRegistry(mutableKey, nonMutableKey, _firstBootCompleteValue, 1, RegistryValueKind.DWord);
            }
            if (!everyBootTasksFailed)
            {
                // Mark every boot tasks as complete. Mark in volatile registry location so it is reset after reboot.
                if (everyBootTasksExecuted)
                {
                    ServiceLogger.LogInformation("Every boot TaskLists complete.");
                }

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

            if (firstBootTasksExecuted || everyBootTasksExecuted)
            {
                // Reset server state, clearing the first boot and first run tasklists, but keep the logs and tasks running.
                _taskExecutionManager.SetLogFolder(logFolder, false);
                _taskExecutionManager.Reset(true, false);

                // Restore state file, if it existed
                if (File.Exists(stateFileBackupPath))
                {
                    File.Copy(stateFileBackupPath, _taskExecutionManager.TaskListStateFile, true);
                }
            }
        }

        /// <summary>
        /// Checks the given mutable and non-mutable registry keys for a given value. Mutable is always checked first.
        /// </summary>
        /// <returns>The value if it exists.</returns>
        private object GetValueFromRegistry(RegistryKey mutableKey, RegistryKey nonMutableKey, string valueName, object defaultValue = null)
        {
            object ret = null;

            if (mutableKey != null)
            {
                if (defaultValue != null)
                {
                    ret = mutableKey.GetValue(valueName, defaultValue);
                }
                else
                {
                    ret = mutableKey.GetValue(valueName);
                }
            }
            
            if ((ret == null) && (nonMutableKey != null))
            {
                if (defaultValue != null)
                {
                    ret = nonMutableKey.GetValue(valueName, defaultValue);
                }
                else
                {
                    ret = nonMutableKey.GetValue(valueName);
                }
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

                var loopbackEnabled = (int)volatileKey.GetValue(_loopbackValue, 0);

                if (loopbackEnabled == 0)
                {
                    ServiceLogger.LogInformation($"Enabling UWP local loopback...");

                    // Run local loopback command for both "official" and "DEV" apps
                    RunProcessViaCmd("checknetisolation", "loopbackexempt -a -n=Microsoft.FactoryOrchestratorApp_8wekyb3d8bbwe", 5000);
                    RunProcessViaCmd("checknetisolation", "loopbackexempt -a -n=Microsoft.FactoryOrchestratorApp.DEV_8wekyb3d8bbwe", 5000);
                }

                success = true;
                volatileKey.SetValue(_loopbackValue, 1, RegistryValueKind.DWord);
            }
            catch (Exception e)
            {
                LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, $"Unable to enable UWP local loopback! You may not be able to use the FactoryOrchestrator UWP app locally ({e.AllExceptionsToString()})"));
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

        private bool LoadOEMCustomizations()
        {
            var nonMutableKey = OpenOrCreateRegKey(RegKeyType.NonMutable);

            DisableNetworkAccess = (bool)GetValueFromRegistry(null, nonMutableKey, _disableNetworkAccessValue, false);
            DisableCommandPromptPage = (bool)GetValueFromRegistry(null, nonMutableKey, _disableCmdPromptValue, false);
            DisableFileTransferPage = (bool)GetValueFromRegistry(null, nonMutableKey, _disableFileTransferValue, false);
            DisableUWPAppsPage = (bool)GetValueFromRegistry(null, nonMutableKey, _disableUWPAppsValue, false);
            DisableManageTasklistsPage = (bool)GetValueFromRegistry(null, nonMutableKey, _disableTaskManagerValue, false);

            return true;
        }

        private TaskRun_Server RunProcessViaCmd(string process, string args, int timeoutMS)
        {
            var run = (TaskRun_Server)TestExecutionManager.RunExecutableAsBackgroundTask(@"%systemroot%\system32\cmd.exe", $"/C \"{process} {args}\"");

            // Wait 2 seconds for process to start
            int waitCount = 0;
            const int waitMS = 100;
            const int maxWaits = waitMS * 20; // 2 seconds

            while ((run.TimeStarted == null) && (waitCount < maxWaits))
            {
                waitCount++;
                System.Threading.Thread.Sleep(100);
            }

            if (run.TimeStarted == null)
            {
                throw new Exception($"{process} never started");
            }

            var runner = run.GetOwningTaskRunner();
            if ((runner != null) && (!runner.WaitForExit(5000)))
            {
                TestExecutionManager.AbortTaskRun(run.Guid);
                throw new Exception($"{process} did not exit after 5 seconds!");
            }

            if (run.TaskStatus != TaskStatus.Passed)
            {
                throw new Exception($"{process} exited with {run.ExitCode}");
            }

            return run;
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

        // Firewall is configured in FactoryOrchestratorServiceTemplate.wm.xml Windows Manifest file
        //private bool AddFirewallRules()
        //{
        //    try
        //    {
        //        if (_foKey == null)
        //        {
        //            OpenOrCreateRegKey();
        //        }

        //        var value = (int)_foKey.GetValue(_firewallValue, 0);

        //        if (value == 0)
        //        {
        //            // Run firewall commands
        //       //     netsh advfirewall firewall add rule name = FactoryOrchestratorService_tcp_in program =< Path to FactoryOrchestratorService.exe > protocol = tcp dir =in enable = yes action = allow profile =public,private,domain

        //     //netsh advfirewall firewall add rule name=FactoryOrchestratorService_tcp_out program =< Path to FactoryOrchestratorService.exe> protocol= tcp dir=out enable= yes action= allow profile=public,private,domain

        //            _foKey.SetValue(_firewallValue, 1, RegistryValueKind.DWord);
        //            return true;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        ServiceLogger.LogError($"Unable to create FactoryOrchestratorService firewall rules! You may not be able to use the FactoryOrchestrator UWP app over the network ({e.Message})");
        //    }

        //    return false;
        //}
    }
}