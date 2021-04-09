// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using JKang.IpcServiceFramework.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FactoryOrchestrator.Client;
using Microsoft.FactoryOrchestrator.Core;
using Microsoft.FactoryOrchestrator.Server;
using Microsoft.Win32;
using TaskStatus = Microsoft.FactoryOrchestrator.Core.TaskStatus;

namespace Microsoft.FactoryOrchestrator.Service
{
    internal static class NativeMethods
    {
        [DllImport("KernelBase.dll", SetLastError = false, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments
        public static extern bool IsApiSetImplemented([MarshalAs(UnmanagedType.LPStr)] string Contract);
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
    }

    /// <summary>
    /// The program executable. Contains Main(). Uses Microsoft.Extensions.Hosting to configure and start systemd & Windows services.
    /// </summary>
    public sealed class FOServiceExe
    {
        /// <summary>
        /// Directory where service the log and TaskList XML are saved. The directory path cannot be changed by the user, unlike the TaskRun log directory (FOServiceStatus.LogFolder). Therefore it is not necessarily the directory where TaskRun logs are saved.
        /// </summary>
        public static string ServiceExeLogFolder
        {
            get
            {
                if (Environment.GetEnvironmentVariable("ProgramData") != null)
                {
                    return Path.Combine(Environment.GetEnvironmentVariable("ProgramData"), "FactoryOrchestrator");
                }
                else if (Directory.Exists("/var/log/"))
                {
                    return Path.Combine("/var/log/", "FactoryOrchestrator");
                }
                else
                {
                    return AppContext.BaseDirectory;
                }
            }
        }

        public static IHost CreateIpcHost(bool allowNetworkAccess, int port, X509Certificate2 sslCertificate) =>
              Host.CreateDefaultBuilder(null)
                  .ConfigureServices(services =>
                  {
                      services.AddScoped<IFactoryOrchestratorService, CommunicationHandler>();
                  })
                  .ConfigureIpcHost(builder =>
                  {
                      // configure IPC endpoints
                      if (allowNetworkAccess)
                      {
                          builder.AddTcpEndpoint<IFactoryOrchestratorService>(options =>
                          {
                              options.IpEndpoint = IPAddress.Any;
                              options.Port = port;
                              options.IncludeFailureDetailsInResponse = true;
                              options.MaxConcurrentCalls = 5;
                              options.SslCertificate = sslCertificate;
                              options.EnableSsl = true;
                          });
                      }
                      else
                      {
                          builder.AddTcpEndpoint<IFactoryOrchestratorService>(options =>
                          {
                              options.IpEndpoint = IPAddress.Loopback;
                              options.Port = port;
                              options.IncludeFailureDetailsInResponse = true;
                              options.MaxConcurrentCalls = 5;
                              options.SslCertificate = sslCertificate;
                              options.EnableSsl = true;
                          });
                      }
                  })
                  .ConfigureLogging(builder =>
                  {
#if DEBUG
                      var _logLevel = LogLevel.Information;
#else
                      var _logLevel = LogLevel.Error;
#endif
                      builder.SetMinimumLevel(_logLevel).AddConsole().AddProvider(new LogFileProvider());
                  }).Build();

        public static void Main(string[] args)
        {
            Host.CreateDefaultBuilder(null).UseSystemd().UseWindowsService().ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<Worker>();
            }).ConfigureLogging(builder =>
            {
#if DEBUG
                var _logLevel = LogLevel.Debug;
#else
                var _logLevel = LogLevel.Error;
#endif
                builder.SetMinimumLevel(_logLevel).AddConsole().AddProvider(new LogFileProvider());
            }).Build().Run();
        }
    }

    public sealed class Worker : IHostedService, IDisposable
    {
        private readonly ILogger<Worker> _logger;
        private FOService _svc;
        private bool disposedValue;
        private const string _name = "Microsoft.FactoryOrchestrator.Service";

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            _svc = new FOService(_logger);
        }


#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(Resources.ServiceStarting, _name);
            _svc.Start(cancellationToken);
            _logger.LogInformation(Resources.ServiceStarted, _name);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(Resources.ServiceStopping, _name);
            _svc.Stop();
            _logger.LogInformation(Resources.ServiceStopped, _name);
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        #region IDisposable Support
        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _svc.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }

    public class FOService : IDisposable
    {
        internal enum RegKeyType
        {
            NonMutable,
            Mutable,
            Volatile
        }

        private static FOService _singleton = null;
        private static readonly bool _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        private static readonly object _constructorLock = new object();
        private static readonly object _openedFilesLock = new object();
        private static readonly SemaphoreSlim _containerConnectionSem = new SemaphoreSlim(1, 1);
        private System.Threading.CancellationTokenSource _ipcCancellationToken;
        private Dictionary<string, (Stream stream, System.Threading.Timer timer)> _openedFiles;

        private readonly string _nonMutableServiceRegKey = @"SYSTEM\CurrentControlSet\Control\FactoryOrchestrator";
        private readonly string _mutableServiceRegKey = @"OSDATA\CurrentControlSet\Control\FactoryOrchestrator";
        private readonly string _volatileServiceRegKey = @"SYSTEM\CurrentControlSet\Control\FactoryOrchestrator\EveryBootTaskStatus";

        // FactoryOS specific registry
        private readonly string _volatileFactoryOSContainerRegKey = @"SYSTEM\CurrentControlSet\Control\FactoryUserManager";
        private readonly string _factoryOSContainerGuidValue = @"ContainerGuid";
        private readonly string _factoryOSContainerIpv4AddressValue = @"ContainerIPv4Address";

        /// <summary>
        /// Prevents service from polling container status.
        /// </summary>
        private readonly string _disableContainerValue = @"DisableContainerSupport";
        private readonly string _sslCertificateFile = @"SSLCertificateFile";

        // OEM Customization registry values
        private readonly string _disableNetworkAccessValue = @"DisableNetworkAccess";
        private readonly string _enableNetworkAccessValue = @"EnableNetworkAccess";
        private readonly string _disableCmdPromptValue = @"DisableCommandPromptPage";
        private readonly string _disableWindowsDevicePortalValue = @"DisableWindowsDevicePortalPage";
        private readonly string _disableUWPAppsValue = @"DisableUWPAppsPage";
        private readonly string _disableTaskManagerValue = @"DisableManageTaskListsPage";
        private readonly string _disableFileTransferValue = @"DisableFileTransferPage";
        private readonly string _localLoopbackAppsValue = @"AllowedLocalLoopbackApps";
        private readonly string _runOnFirstBootValue = @"RunInitialTaskListsOnFirstBoot";
        internal readonly string _logFolderValue = @"TaskRunLogFolder";
        private readonly string _servicePortValue = "NetworkPort";

        /// <summary>
        /// Default TaskRun log folder path
        /// </summary>
        private readonly string _defaultTaskManagerLogFolder = Path.Combine(FOServiceExe.ServiceExeLogFolder, "Logs");

        // Default paths in testcontent directory for user tasklists
        private readonly string _initialTasksDefaultPath = Environment.ExpandEnvironmentVariables(@"%DataDrive%\TestContent\InitialTaskLists.xml");
        private readonly string _firstBootTasksDefaultPath = Environment.ExpandEnvironmentVariables(@"%DataDrive%\TestContent\FirstBootTasks.xml");
        private readonly string _everyBootTasksDefaultPath = Environment.ExpandEnvironmentVariables(@"%DataDrive%\TestContent\EveryBootTasks.xml");

        // Appsettings.json / registry fallbacks for user tasklists
        private readonly string _firstBootTasksPathValue = @"FirstBootTasks";
        private readonly string _everyBootTasksPathValue = @"EveryBootTasks";
        private readonly string _initialTasksPathValue = @"InitialTaskLists";


        // PFNs for Factory Orchestrator
        private readonly string _foAppPfn = "Microsoft.FactoryOrchestratorApp_8wekyb3d8bbwe";
        private readonly string _foDevAppPfn = "Microsoft.FactoryOrchestratorApp.DEV_8wekyb3d8bbwe";

        private RegistryKey _mutableKey;
        private RegistryKey _nonMutableKey;
        private RegistryKey _volatileKey;

        private readonly string _serviceStatusFilename = Path.Combine(FOServiceExe.ServiceExeLogFolder, "FactoryOrchestratorServiceStatus.xml");
        // Unix only. Windows uses volatile registry.
        private readonly string _volatileServiceStatusFilename = @"/var/log/FactoryOrchestrator/FactoryOrchestratorVolatileServiceStatus.xml";
        private FactoryOrchestratorClient _containerClient;
        private ulong _lastContainerEventIndex;
        private HashSet<Guid> _containerGUITaskRuns;

        // TaskManager_Server instances
        private TaskManager _taskExecutionManager;
        /// <summary>
        /// Gets the "active" task execution manager. If boot tasks are executing it returns the BootTaskExecutionManager, otherwise the _taskExecutionManager.
        /// </summary>
        /// <value>
        /// The task execution manager.
        /// </value>
        public TaskManager TaskExecutionManager
        {
            get
            {
                if (IsExecutingBootTasks)
                {
                    return BootTaskExecutionManager;
                }
                else
                {
                    return _taskExecutionManager;
                }
            }
        }
        /// <summary>
        /// Gets the boot task execution manager.
        /// </summary>
        /// <value>
        /// The boot task execution manager.
        /// </value>
        public TaskManager BootTaskExecutionManager { get; private set; }

        /// <summary>
        /// The service logger for FactoryOrchestrator (FOService).
        /// </summary>
        /// <value>
        /// The service logger.
        /// </value>
        public ILogger<Worker> ServiceLogger { get; private set; }

        /// <summary>
        /// A configuration (possibly empty) of service settings that are in a well-known appsettings.json file.
        /// Used to configure service defaults.
        /// </summary>
        public IConfiguration Appsettings { get; private set; }
        /// <summary>
        /// A file-backed representation of key service state that persists through reboots.
        /// </summary>
        public FOServiceStatus ServiceStatus { get; private set; }
        /// <summary>
        /// A file or registry backed representation of key service state that does not persist through reboots.
        /// </summary>
        public FOVolatileServiceStatus VolatileServiceStatus { get; private set; }
        public Dictionary<ulong, ServiceEvent> ServiceEvents { get; private set;  }
        public ulong LastEventIndex { get; private set; }
        public DateTime LastEventTime { get; private set; }
        public bool DisableCommandPromptPage { get; private set; }
        public bool DisableWindowsDevicePortalPage { get; private set; }
        public bool DisableUWPAppsPage { get; private set; }
        public bool DisableManageTasklistsPage { get; private set; }
        public bool DisableFileTransferPage { get; private set; }
        public bool IsNetworkAccessEnabled { get => _networkAccessEnabled && !_networkAccessDisabled; }
        public int NetworkPort { get; private set; }
        public X509Certificate2 SSLCertificate { get; private set; }
        public bool RunInitialTaskListsOnFirstBoot { get; private set; }
        public bool IsContainerSupportEnabled { get; private set; }

        public bool IsContainerConnected => _containerClient?.IsConnected ?? false;
        public Guid ContainerGuid { get; private set; }
        public IPAddress ContainerIpAddress { get; private set; }
        private System.Threading.CancellationTokenSource _containerHeartbeatToken;
        private bool _networkAccessEnabled;
        private bool _networkAccessDisabled;

        /// <summary>
        /// List of apps to enable local loopback on.
        /// </summary>
        public List<string> LocalLoopbackApps { get; private set; }
        public bool IsExecutingBootTasks { get; private set; }

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

        /// <summary>
        /// Returns OS version string.
        /// </summary>
        /// <returns></returns>
        public static string GetOSVersionString()
        {
            return Environment.OSVersion.VersionString;
        }

        /// <summary>
        /// Returns OEM version string, set as WCOS OEM Customization.
        /// </summary>
        /// <returns></returns>
        public static string GetOEMVersionString()
        {
            if (!_isWindows)
            {
                throw new PlatformNotSupportedException(string.Format(CultureInfo.CurrentCulture, Resources.WindowsOnlyError, "GetOEMVersionString"));
            }

            using var reg = Registry.LocalMachine.OpenSubKey(@"OSDATA\CurrentControlSet\Control\FactoryOrchestrator", false);
            var version = (string)reg?.GetValue("OEMVersion", null);
            return version;
        }

        public FOService(ILogger<Worker> logger)
        {
            lock (_constructorLock)
            {
                if (_singleton != null)
                {
                    throw new FactoryOrchestratorException(Resources.ServiceAlreadyCreatedError);
                }

                // Only initialize the bare minimum required fields & properties here. 
                // Most initialization should be done in ExecuteServerBootTasks() as part of Service Start().
                ServiceLogger = logger;
                _singleton = this;
            }
        }

        /// <summary>
        /// Service start.
        /// </summary>
        public void Start(CancellationToken cancellationToken)
        {
            Start(cancellationToken, false);
        }

        /// <summary>
        /// Service start.
        /// </summary>
        public void Start(CancellationToken cancellationToken, bool forceUserTaskRerun)
        {
            // Execute "first run" tasks. They do nothing if already run, but might need to run every boot on a state separated WCOS image.
            ExecuteServerBootTasks(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            // Load first boot state file, or try to load known TaskLists from the existing state file.
            bool firstBootFileLoaded = LoadFirstBootStateFile(forceUserTaskRerun);
            if (!firstBootFileLoaded && File.Exists(_taskExecutionManager.TaskListStateFile))
            {
                try
                {
                    _taskExecutionManager.LoadTaskListsFromXmlFile(_taskExecutionManager.TaskListStateFile);
                }
                catch (Exception e)
                {
                    ServiceLogger.LogWarning($"{string.Format(CultureInfo.CurrentCulture, Resources.FOXMLFileLoadException, _taskExecutionManager.TaskListStateFile)}\n {e.AllExceptionsToString()}");
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            // Start IPC server on desired port. Only start after all boot tasks are complete.
            var ipcHost = FOServiceExe.CreateIpcHost(IsNetworkAccessEnabled, NetworkPort, SSLCertificate);
            _ipcCancellationToken = new System.Threading.CancellationTokenSource();
            var ipcHost.RunAsync(_ipcCancellationToken.Token);

            if (IsNetworkAccessEnabled)
            {
                ServiceLogger.LogInformation($"{Resources.NetworkAccessEnabled}\n");
            }
            else 
            {
                LogServiceEvent(new ServiceEvent(ServiceEventType.NetworkAccessDisabled, null, Resources.NetworkAccessDisabled));
            }

            ServiceLogger.LogInformation($"{Resources.ReadyToCommunicate}\n");
            LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceStart, null, Resources.BootTasksStarted));

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            // Execute user defined tasks.
            ExecuteUserBootTasks(cancellationToken, forceUserTaskRerun);

            IsExecutingBootTasks = false;
            LogServiceEvent(new ServiceEvent(ServiceEventType.BootTasksComplete, null, Resources.BootTasksFinished));

            if (RunInitialTaskListsOnFirstBoot && firstBootFileLoaded)
            {
                // Use a new thread so service Start isn't blocked by these TaskLists executing.
                Task.Run(() => _taskExecutionManager.RunAllTaskLists());
            }
        }

        private bool LoadFirstBootStateFile(bool force)
        {
            bool loaded = false;

            try
            {
                if (!ServiceStatus.FirstBootStateLoaded || force)
                {
                    string firstBootStateTaskListPath = _initialTasksDefaultPath;

                    ServiceLogger.LogInformation(string.Format(CultureInfo.CurrentCulture, Resources.CheckingForFile, _initialTasksDefaultPath));
                    // Find the TaskLists XML path. Check testcontent directory for wellknown name, fallback to registry
                    if (!File.Exists(firstBootStateTaskListPath))
                    {
                        firstBootStateTaskListPath = (string)(GetAppSetting(_initialTasksPathValue) ?? _initialTasksDefaultPath);
                        if (!firstBootStateTaskListPath.Equals(_initialTasksDefaultPath, StringComparison.OrdinalIgnoreCase))
                        {
                            ServiceLogger.LogInformation(string.Format(CultureInfo.CurrentCulture, Resources.CheckingForFile, firstBootStateTaskListPath));
                        }
                    }

                    if (File.Exists(firstBootStateTaskListPath))
                    {
                        ServiceLogger.LogInformation(string.Format(CultureInfo.CurrentCulture, Resources.AttemptingFileLoad, firstBootStateTaskListPath));

                        // Load the TaskLists file specified in registry
                        _taskExecutionManager.LoadTaskListsFromXmlFile(firstBootStateTaskListPath);

                        ServiceLogger.LogInformation(string.Format(CultureInfo.CurrentCulture, Resources.FileLoadSucceeded, firstBootStateTaskListPath));
                        loaded = true;
                        ServiceStatus.FirstBootStateLoaded = true;
                    }
                    else
                    {
                        ServiceLogger.LogInformation(string.Format(CultureInfo.CurrentCulture, Resources.FileNotFound, firstBootStateTaskListPath));
                    }
                }
            }
            catch (Exception e)
            {
                LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, $"{string.Format(CultureInfo.CurrentCulture, Resources.FOXMLFileLoadException, _initialTasksDefaultPath)} ({e.AllExceptionsToString()})"));
            }

            return loaded;
        }

        /// <summary>
        /// Service stop.
        /// </summary>
        public void Stop()
        {
            // Disable inter process communication interfaces
            _ipcCancellationToken?.Cancel();
            _containerHeartbeatToken?.Cancel();
            _containerClient = null;
            ContainerGuid = Guid.Empty;
            ContainerIpAddress = null;

            // Abort everything that's running, except persisted background tasks
            _taskExecutionManager?.AbortAll();
            BootTaskExecutionManager?.AbortAll();

            // Update state file
            try
            {
                TaskExecutionManager?.SaveAllTaskListsToXmlFile(TaskExecutionManager.TaskListStateFile);
            }
            catch (FactoryOrchestratorException e)
            {
                ServiceLogger.LogError(e, Resources.ServiceStopSaveError);
            }

            // Close registry
            _volatileKey?.Close();
            _nonMutableKey?.Close();
            _mutableKey?.Close();

            ServiceLogger.LogInformation(Resources.ServiceStoppedWithName);
        }

        private void HandleTaskManagerEvent(object source, TaskManagerEventArgs e)
        {
            ServiceEvent serviceEvent = null;
            switch (e.Event)
            {
                case TaskManagerEventType.WaitingForExternalTaskRunStarted:
                    {
                        var run = _taskExecutionManager.GetTaskRunByGuid((Guid)e.Guid);

                        if (run.RunInContainer)
                        {
                            serviceEvent = new ServiceEvent(ServiceEventType.WaitingForContainerTaskRun, e.Guid, string.Format(CultureInfo.CurrentCulture, Resources.WaitingForContainerTaskRun, e.Guid));
                            RunTaskRunInContainer(run);
                        }
                        else
                        {
                            serviceEvent = new ServiceEvent(ServiceEventType.WaitingForExternalTaskRun, e.Guid, string.Format(CultureInfo.CurrentCulture, Resources.WaitingForExternalTaskRun, e.Guid));
                        }
                        break;
                    }
                case TaskManagerEventType.TaskRunRedirectedToRunAsRDUser:
                    {
                        serviceEvent = new ServiceEvent(ServiceEventType.TaskRunRedirectedToRunAsRDUser, e.Guid, string.Format(CultureInfo.CurrentCulture, Resources.WaitingForExternalTaskRun, e.Guid));
                        break;
                    }
                case TaskManagerEventType.WaitingForExternalTaskRunFinished:
                    serviceEvent = new ServiceEvent(ServiceEventType.DoneWaitingForExternalTaskRun, e.Guid, string.Format(CultureInfo.CurrentCulture, Resources.DoneWaitingForExternalTaskRun, e.Guid, e.Status));
                    break;
                default:
                    break;
            }

            if (serviceEvent != null)
            {
                LogServiceEvent(serviceEvent);
            }
        }

        private void RunTaskRunInContainer(ServerTaskRun hostRun)
        {
            hostRun.TaskOutput.Add(Resources.AttemptingContainerTaskRun);
            hostRun.TaskStatus = TaskStatus.Running;

            Task.Run(async () =>
            {
                try
                {
                    // Create a copy of the Task
                    TaskBase containerTask;
                    if (hostRun.OwningTask != null)
                    {
                        containerTask = hostRun.OwningTask.DeepCopy();
                    }
                    else
                    {
                        containerTask = TaskBase.CreateTaskFromTaskRun(hostRun);
                    }

                    // Set RunInContainer to false so it doesn't try to find another container :)
                    containerTask.RunInContainer = false;

                    // TODO: UWPs are not currently supported in the container, due to the limited shell UI support
                    //if (containerTask.Type == TaskType.UWP)
                    //{
                    //    // The container doesn't have WDP, translate to a shell execute call
                    //    hostRun.TaskOutput.Add(Resources.RedirectingUWPToRunAs);
                    //    var temp = containerTask as UWPTask;
                    //    var uwpContainerTask = new ExecutableTask(@"%windir%\system32\RunAsRDUser.exe")
                    //    {
                    //        Arguments = @"explorer.exe shell:appsFolder\" + temp.Path
                    //    };
                    //    containerTask = uwpContainerTask;
                    //}

                    // Wait forever for the container to be ready.
                    // User can abort or add timeout if desired.
                    while (!await TryVerifyContainerConnection(30) && !hostRun.TaskRunComplete)
                    {
                        if (!hostRun.TaskRunComplete)
                        {
                            hostRun.TaskOutput.Add(Resources.WaitingForContainerStart);
                        }
                    }

                    if (hostRun.TaskRunComplete)
                    {
                        // Host task was completed (likely aborted/timeout) while we were waiting for the container to connect.
                        // No action is needed.
                        return;
                    }

                    hostRun.TaskOutput.Add($"----------- {Resources.StartContainerOutput} (stdout, stderr) -----------");
                    var containerRun = await _containerClient.RunTask(containerTask, hostRun.Guid);
                    containerRun = await _containerClient.QueryTaskRun(containerRun.Guid);
                    int latestIndex = 0;
                    while (!containerRun.TaskRunComplete)
                    {
                        // TODO: signaling
                        System.Threading.Thread.Sleep(1000);

                        // While the task is running inside the container, query it every second and update the output.
                        containerRun = await _containerClient.QueryTaskRun(containerRun.Guid);
                        if (latestIndex != containerRun.TaskOutput.Count)
                        {
                            var newOutput = containerRun.TaskOutput.GetRange(latestIndex, containerRun.TaskOutput.Count - latestIndex);
                            latestIndex = containerRun.TaskOutput.Count;
                            hostRun.TaskOutput.AddRange(newOutput);
                        }

                        if (hostRun.TaskRunComplete)
                        {
                            // Host task was completed.
                            break;
                        }
                        else
                        {
                            hostRun.TaskStatus = containerRun.TaskStatus;
                            TaskExecutionManager.UpdateTaskRun(hostRun);
                        }
                    }

                    if (hostRun.TaskRunComplete && !containerRun.TaskRunComplete)
                    {
                        // Host task was completed by another means, likely aborted.
                        // Abort the container task too. Don't do any further updates to the host task.
                        try
                        {
                            await _containerClient.AbortTaskRun(containerRun.Guid);
                        }
                        catch (Exception)
                        { }
                    }
                    else
                    {
                        // Container task completed.
                        // Perform final update of host TaskRun
                        if (latestIndex != containerRun.TaskOutput.Count)
                        {
                            var newOutput = containerRun.TaskOutput.GetRange(latestIndex, containerRun.TaskOutput.Count - latestIndex);
                            latestIndex = containerRun.TaskOutput.Count;
                            hostRun.TaskOutput.AddRange(newOutput);
                        }
                        hostRun.TimeFinished = DateTime.Now;
                        hostRun.ExitCode = containerRun.ExitCode;
                        hostRun.TaskStatus = containerRun.TaskStatus;
                        hostRun.TaskOutput.Add($"----------- {Resources.EndContainerOutput} -----------");
                    }

                    if (_containerGUITaskRuns.Contains(containerRun.Guid))
                    {
                        _containerGUITaskRuns.Remove(containerRun.Guid);
                        if (_containerGUITaskRuns.Count == 0)
                        {
                            try
                            {
                                // Exit URDC if we launched it.
                                var rdApp = (await WDPHelpers.GetInstalledAppPackagesAsync()).Packages.Where(x => x.FullName.StartsWith("Microsoft.RemoteDesktop", StringComparison.OrdinalIgnoreCase)).DefaultIfEmpty(null).FirstOrDefault();

                                if (rdApp != null)
                                {
                                    await WDPHelpers.CloseAppWithWDP(rdApp.FullName);
                                }
                            }
                            catch (Exception)
                            {
                                // Ignore failure to exit RD
                            }
                        }
                    }
                        
                }
                catch (Exception e)
                {
                    LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, $"{Resources.ContainerTaskRunFailed}! {e.AllExceptionsToString()}"));
                    hostRun.TaskOutput.Add(Resources.ContainerTaskRunFailed);
                    hostRun.TaskOutput.Add(e.AllExceptionsToString());
                    hostRun.TaskStatus = TaskStatus.Failed;
                    hostRun.ExitCode = e.HResult;
                }
            });
        }

        public async Task<bool> TryVerifyContainerConnection(int retrySeconds = 0)
        {
            try
            {
                await VerifyContainerConnection(retrySeconds);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to connect to the container running FactoryOrchestratorService.
        /// </summary>
        /// <param name="retrySeconds">Number of seconds to retry before failing, default is 0 (no retries).</param>
        /// <returns></returns>
        public async Task VerifyContainerConnection(int retrySeconds = 0)
        {
            // Use semaphore to ensure ServiceEvents don't fire multiple times
            await _containerConnectionSem.WaitAsync();
            try
            {
                System.Diagnostics.Stopwatch w = new System.Diagnostics.Stopwatch();
                w.Start();

                while (true)
                {
                    var previousContainerStatus = IsContainerConnected;
                    var previousContainerGuid = ContainerGuid;

                    try
                    {
                        // Throws FactoryOrchestratorContainerException if unable to connect
                        await ConnectToContainer();

                        if (IsContainerConnected && previousContainerStatus)
                        {
                            // We were connected already. Check for any new container service events and log new host server events if any are found.
                            var containerEvents = await _containerClient.GetServiceEvents(_lastContainerEventIndex);
                            foreach (var containerEvent in containerEvents)
                            {
                                _lastContainerEventIndex = containerEvent.EventIndex;

                                switch (containerEvent.ServiceEventType)
                                {
                                    case ServiceEventType.ServiceError:
                                        LogServiceEvent(new ServiceEvent(ServiceEventType.ContainerServiceError, containerEvent.Guid, containerEvent.Message));
                                        break;
                                    case ServiceEventType.TaskRunRedirectedToRunAsRDUser:
                                        LogServiceEvent(new ServiceEvent(ServiceEventType.ContainerTaskRunRedirectedToRunAsRDUser, containerEvent.Guid, containerEvent.Message));
                                        _containerGUITaskRuns.Add((Guid)containerEvent.Guid);
                                        break;
                                    default:
                                        // ignore other events
                                        break;
                                }
                            }
                        }
                        else
                        {
                            // Initial connection made or reconnected
                            LogServiceEvent(new ServiceEvent(ServiceEventType.ContainerConnected, ContainerGuid, Resources.ContainerConnected));
                        }

                        // Connection is working, exit loop
                        break;
                    }
                    catch (Exception e)
                    {
                        if (previousContainerStatus && !IsContainerConnected)
                        {
                            // Connection lost
                            LogServiceEvent(new ServiceEvent(ServiceEventType.ContainerDisconnected, previousContainerGuid, Resources.ContainerDisconnected));
                        }

                        if (w.ElapsedMilliseconds / 1000 > retrySeconds)
                        {
                            if (!(e is FactoryOrchestratorContainerException))
                            {
                                e = new FactoryOrchestratorContainerException(null, e);
                            }

                            throw e;
                        }
                    }
                }
            }
            finally
            {
                _containerConnectionSem.Release();
            }
        }

        public async Task ConnectToContainer()
        {
            if (!IsContainerSupportEnabled)
            {
                throw new FactoryOrchestratorContainerException(Resources.ContainerDisabledException);
            }

            if (ContainerGuid != null)
            {
                if (ContainerIpAddress != null)
                {
                    if ((_containerClient == null || !_containerClient.IsConnected))
                    {
                        _containerClient = new FactoryOrchestratorClient(ContainerIpAddress, NetworkPort);
                        try
                        {
                            await _containerClient.Connect();
                            return;
                        }
                        catch (Exception e)
                        {
                            _containerClient = null;
                            throw new FactoryOrchestratorContainerException(Resources.ContainerConnectionFailed, e);
                        }
                    }
                    else
                    {
                        return;
                    }
                }

                _containerClient = null;
                throw new FactoryOrchestratorContainerException(Resources.NoContainerIpFound);
            }

            _containerClient = null;
            throw new FactoryOrchestratorContainerException(Resources.NoContainerIdFound);
        }

        public async Task<bool> TryConnectToContainer()
        {
            try
            {
                await ConnectToContainer();
                return true;
            }
            catch (FactoryOrchestratorContainerException)
            {
                return false;
            }
        }

        public bool UpdateContainerInfo()
        {
            // FactoryOS puts the container guid in SYSTEM\CurrentControlSet\Control\FactoryUserManager
            using var reg = Registry.LocalMachine.OpenSubKey(_volatileFactoryOSContainerRegKey, false);
            if (reg != null)
            {
                var guidStr = (string)reg.GetValue(_factoryOSContainerGuidValue, String.Empty);
                Guid guid;
                if (Guid.TryParse(guidStr, out guid))
                {
                    ContainerGuid = guid;
                }
                else
                {
                    ContainerGuid = Guid.Empty;
                }

                var ipStr = (string)reg.GetValue(_factoryOSContainerIpv4AddressValue, String.Empty);
                IPAddress ip;
                if (IPAddress.TryParse(ipStr, out ip))
                {
                    ContainerIpAddress = ip;
                }
                else
                {
                    ContainerIpAddress = null;
                }

                if ((ContainerGuid != Guid.Empty) && (ContainerIpAddress != null))
                {
                    return true;
                }
            }
            else
            {
                // Values not set in registry
                ContainerGuid = Guid.Empty;
                ContainerIpAddress = null;
                _containerClient = null;
            }

            return false;
        }

        public void SendFile(string targetFilename, byte[] fileData, bool appending, bool sendToContainer)
        {
            if (fileData == null)
            {
                throw new ArgumentNullException(nameof(fileData));
            }

            if (!sendToContainer)
            {
	            targetFilename = Environment.ExpandEnvironmentVariables(targetFilename);
	            Directory.CreateDirectory(Path.GetDirectoryName(targetFilename));

	            // Lock file access lock so the stream can't be disposed mid-write
	            lock (_openedFilesLock)
	            {
	                if (!appending && File.Exists(targetFilename))
	                {
	                    CloseFile(targetFilename);
	                    File.Delete(targetFilename);
	                }

	                var tuple = OpenFile(targetFilename);
	                var stream = tuple.stream;
	                stream.Seek(0, SeekOrigin.End);
	                stream.Write(fileData);
	            }

	            // If data is less than the standard length, assume the transfer is complete and close the file.
	            if (fileData.Length != Constants.FileTransferChunkSize)
	            {
	                CloseFile(targetFilename);
	            }
		    }
            else
            {
                SendFileToContainer(targetFilename, fileData, appending).Wait();
            }
        }
		
        public async Task SendFileToContainer(string containerFilePath, byte[] fileData, bool appending)
        {
            await VerifyContainerConnection(30);
            try
            {
                await _containerClient.SendFile(containerFilePath, fileData, appending);
            }
            catch (Exception e)
            {
                throw new FactoryOrchestratorContainerException(Resources.ContainerFileSendFailed, null, e);
            }
        }

        public byte[] GetFile(string sourceFilename, long offset, int count, bool getFromContainer)
        {
            byte[] bytes = Array.Empty<byte>();

            if (!getFromContainer)
            {
                sourceFilename = Environment.ExpandEnvironmentVariables(sourceFilename);
                if (!File.Exists(sourceFilename))
                {
                    throw new FileNotFoundException(string.Format(CultureInfo.CurrentCulture, Resources.FileNotFoundException, sourceFilename), sourceFilename);
                }

                if (offset < 0)
                {
                    bytes = File.ReadAllBytes(sourceFilename);
                }
                else
                {
                    lock (_openedFilesLock)
                    {
                        var tuple = OpenFile(sourceFilename);

                        var stream = tuple.stream;

                        if (offset < stream.Length)
                        {
                            stream.Seek(offset, SeekOrigin.Begin);
                            byte[] temp = new byte[count];
                            var bytesRead = stream.Read(temp, 0, count);
                            if (bytesRead == count)
                            {
                                bytes = temp;
                            }
                            else
                            {
                                bytes = temp.ToList().GetRange(0, bytesRead).ToArray();
                            }
                        }
                    }

                    // Check if we hit the end of the file
                    if (bytes.Length != count)
                    {
                        // We hit the end of the file
                        CloseFile(sourceFilename);
                    }
                }

                return bytes;
            }
            else
            {
                return GetFileFromContainer(sourceFilename, offset, count).Result;
            }
        }

        public async Task<byte[]> GetFileFromContainer(string containerFilePath, long offset, int count)
        {
            await VerifyContainerConnection(30);
            try
            {
                return await _containerClient.GetFile(containerFilePath, offset, count);
            }
            catch (Exception e)
            {
                throw new FactoryOrchestratorContainerException(Resources.ContainerFileGetFailed, null, e);
            }
        }

        public async Task MoveInContainer(string sourcePath, string destinationPath)
        {
            await VerifyContainerConnection(30);
            try
            {
                await _containerClient.MoveFileOrFolder(sourcePath, destinationPath);
            }
            catch (Exception e)
            {
                throw new FactoryOrchestratorContainerException($"", null, e);
            }
        }

        public async Task DeleteInContainer(string path)
        {
            await VerifyContainerConnection(30);
            try
            {
                await _containerClient.DeleteFileOrFolder(path);
            }
            catch (Exception e)
            {
                throw new FactoryOrchestratorContainerException($"", null, e);
            }
        }

        public async Task<List<string>> EnumerateFilesInContainer(string path, bool recursive)
        {
            await VerifyContainerConnection(30);
            try
            {
                return await _containerClient.EnumerateFiles(path, recursive);
            }
            catch (Exception e)
            {
                throw new FactoryOrchestratorContainerException($"", null, e);
            }
        }

        public async Task<List<string>> EnumerateDirectoriesInContainer(string path, bool recursive)
        {
            await VerifyContainerConnection(30);
            try
            {
                return await _containerClient.EnumerateDirectories(path, recursive);
            }
            catch (Exception e)
            {
                throw new FactoryOrchestratorContainerException($"", null, e);
            }
        }

        /// <summary>
        /// Opens the file for reading or writing. It is created if it does not exist. After 1 second the file is closed unless the same file is attempted to be reopened before then.
        /// </summary>
        /// <param name="sourceFilename">The file to open.</param>
        private (Stream stream, System.Threading.Timer timer) OpenFile(string sourceFilename)
        {
            lock(_openedFilesLock)
            {
                if (_openedFiles.ContainsKey(sourceFilename))
                {
                    _openedFiles[sourceFilename].timer.Change(10000, System.Threading.Timeout.Infinite);
                    return _openedFiles[sourceFilename];
                }
                else
                {
                    var stream = File.Open(sourceFilename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                    System.Threading.Timer timer = new System.Threading.Timer(OnFileTimeOut, sourceFilename, 10000, System.Threading.Timeout.Infinite);
                    var tuple = (stream, timer);
                    _openedFiles.Add(sourceFilename, tuple);
                    return tuple;
                }
            }
        }

        private void CloseFile(string sourceFilename)
        {
            lock (_openedFilesLock)
            {
                if (!_openedFiles.ContainsKey(sourceFilename))
                {
                    return;
                }

                var timer = _openedFiles[sourceFilename].timer;
                timer.Dispose();
                var stream = _openedFiles[sourceFilename].stream;
                stream.Flush();
                stream.Dispose();
                _openedFiles.Remove(sourceFilename);
            }
        }

        private void OnFileTimeOut(object state)
        {
            CloseFile((string)state);
        }


        public void LogServiceEvent(ServiceEvent serviceEvent)
        {
            if (serviceEvent == null)
            {
                throw new ArgumentNullException(nameof(serviceEvent));
            }

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
        public void ExecuteServerBootTasks(CancellationToken cancellationToken)
        {
            if (_isWindows)
            {
                // Open Registry Keys
                try
                {
                    _mutableKey = OpenOrCreateRegKey(RegKeyType.Mutable);
                }
                catch (Exception)
                {
                    // OSDATA wont exist on Desktop, just set to NULL if it fails to open
                    _mutableKey = null;
                }

                try
                {
                    _nonMutableKey = OpenOrCreateRegKey(RegKeyType.NonMutable);
                }
                catch (Exception e)
                {
                    ServiceLogger.LogError($" {e.Message}");
                    throw;
                }

                try
                {
                    _volatileKey = OpenOrCreateRegKey(RegKeyType.Volatile);
                }
                catch (Exception e)
                {
                    ServiceLogger.LogError($"! {e.Message}");
                    throw;
                }
            }

            // Load ServiceStatus files or registry.
            ServiceStatus = FOServiceStatus.CreateOrLoad(_serviceStatusFilename, ServiceLogger);
            VolatileServiceStatus = FOVolatileServiceStatus.CreateOrLoad(_volatileServiceStatusFilename, _volatileKey, ServiceLogger);

            if(cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (_isWindows)
            {
                // Check if this is the first time running the service after updating the service to use FOServiceStatus instead of the registry for state tracking.
                // Move the state tracking registry values into FOServiceStatus if so.
                RegistryKey source = null;
                var firstBootTaskListsComplete = GetValueFromRegistry("FirstBootTaskListsComplete", null, out source) as int?;
                if (firstBootTaskListsComplete.HasValue)
                {
                    ServiceStatus.FirstBootTaskListsComplete = (firstBootTaskListsComplete.Value == 0) ? false : true;
                    source.DeleteValue("FirstBootTaskListsComplete");
                }

                var firstBootStateLoaded = GetValueFromRegistry("FirstBootStateLoaded", null, out source) as int?;
                if (firstBootStateLoaded.HasValue)
                {
                    ServiceStatus.FirstBootStateLoaded = (firstBootStateLoaded.Value == 0) ? false : true;
                    source.DeleteValue("FirstBootStateLoaded");
                }

                var logFolder = GetValueFromRegistry("LogFolder", null, out source) as string;
                if (!string.IsNullOrWhiteSpace(logFolder))
                {
                    ServiceStatus.LogFolder = logFolder;
                    source.DeleteValue("LogFolder");
                }
            }

            ServiceEvents = new Dictionary<ulong, ServiceEvent>();
            LastEventIndex = 0;
            LastEventTime = DateTime.MinValue;
            LocalLoopbackApps = new List<string>();
            IsExecutingBootTasks = true;
            _openedFiles = new Dictionary<string, (Stream stream, System.Threading.Timer timer)>();

            ContainerGuid = Guid.Empty;
            ContainerIpAddress = null;
            _containerHeartbeatToken = null;
            _containerClient = null;
            _lastContainerEventIndex = 0;
            _containerGUITaskRuns = new HashSet<Guid>();

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            LoadOEMCustomizations();

            // Now that we know the log folders, we can create the TaskManager instance
            try
            {
                Directory.CreateDirectory(FOServiceExe.ServiceExeLogFolder);
            }
            catch (Exception e)
            {
                ServiceLogger.LogError($"{string.Format(CultureInfo.CurrentCulture, Resources.CreateDirectoryFailed, FOServiceExe.ServiceExeLogFolder)} {e.Message}");
                throw;
            }
            try
            {
                Directory.CreateDirectory(ServiceStatus.LogFolder);
            }
            catch (Exception e)
            {
                ServiceLogger.LogError($"{string.Format(CultureInfo.CurrentCulture, Resources.CreateDirectoryFailed, ServiceStatus.LogFolder)} {e.Message}");
                throw;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            _taskExecutionManager = new TaskManager(ServiceStatus.LogFolder, Path.Combine(FOServiceExe.ServiceExeLogFolder, "FactoryOrchestratorKnownTaskLists.xml"));
            _taskExecutionManager.OnTaskManagerEvent += HandleTaskManagerEvent;

            if (IsContainerSupportEnabled)
            {
                // Start container heartbeat thread
                _containerHeartbeatToken = new System.Threading.CancellationTokenSource();
                Task.Run(async () =>
                {
                    while (!_containerHeartbeatToken.Token.IsCancellationRequested)
                    {
                        // Poll every 5s for container running Factory Orchestrator Service.
                        UpdateContainerInfo();
                        await TryVerifyContainerConnection();
                        await Task.Delay(5000);
                    }
                });
            }
            else
            {
                LogServiceEvent(new ServiceEvent(ServiceEventType.ContainerDisabled, null, Resources.ContainerDisabledException));
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            // Enable local loopback every boot.
            EnableUWPLocalLoopback();
        }

        /// <summary>
        /// Executes user defined tasks that should run on first boot (of FactoryOrchestrator) or every boot.
        /// </summary>
        /// <returns></returns>
        public void ExecuteUserBootTasks(CancellationToken cancellationToken, bool force)
        {
            bool firstBootTasksFailed = false;
            bool everyBootTasksFailed = false;
            bool firstBootTasksExecuted = false;
            bool everyBootTasksExecuted = false;

            // First Boot tasks
            try
            {
                // Create unconditionally so it is guaranteed to not be null
                BootTaskExecutionManager = new TaskManager(Path.Combine(_taskExecutionManager.LogFolder, "FirstBootTaskLists"), _firstBootTasksDefaultPath);

                // Find the TaskLists XML path.
                var firstBootTaskListPath = (string)(GetAppSetting(_firstBootTasksPathValue) ?? _firstBootTasksDefaultPath);
                ServiceLogger.LogInformation(string.Format(CultureInfo.CurrentCulture, Resources.CheckingForFile, firstBootTaskListPath));

                // Load first boot XML, even if we don't end up executing it, so it can be queried via FO APIs
                List<Guid> firstBootTaskListGuids = new List<Guid>();
                if (File.Exists(firstBootTaskListPath))
                {
                    BootTaskExecutionManager = new TaskManager(Path.Combine(_taskExecutionManager.LogFolder, "FirstBootTaskLists"), firstBootTaskListPath);
                    ServiceLogger.LogInformation(string.Format(CultureInfo.CurrentCulture, Resources.AttemptingFileLoad, firstBootTaskListPath));
                    // Load the first boot TaskLists file
                    firstBootTaskListGuids = BootTaskExecutionManager.LoadTaskListsFromXmlFile(firstBootTaskListPath);
                }
                else
                {
                    ServiceLogger.LogInformation(string.Format(CultureInfo.CurrentCulture, Resources.FileNotFound, firstBootTaskListPath));
                }

                // Check if first boot tasks were already completed
                if (!ServiceStatus.FirstBootTaskListsComplete || force)
                {
                    firstBootTasksExecuted = true;

                    foreach (var listGuid in firstBootTaskListGuids)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        BootTaskExecutionManager.RunTaskList(listGuid);
                        ServiceLogger.LogInformation(string.Format(CultureInfo.CurrentCulture, Resources.FirstBootRunningTaskList, listGuid));
                    }
                }
                else
                {
                    ServiceLogger.LogInformation(Resources.FirstBootAlreadyComplete);
                }
            }
            catch (Exception e)
            {
                LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, $"{Resources.FirstBootFailed}! ({e.AllExceptionsToString()})"));
                firstBootTasksFailed = true;
            }

            // Wait for all first boot tasks to complete
            int sleepCount = 0;
            while (BootTaskExecutionManager.IsTaskListRunning && !cancellationToken.IsCancellationRequested)
            {
                System.Threading.Thread.Sleep(1000);
                sleepCount++;
                if (sleepCount % 15 == 0)
                {
                    ServiceLogger.LogInformation(Resources.FirstBootWaiting);
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (!firstBootTasksFailed)
            {
                // Mark first boot tasks as complete
                if (firstBootTasksExecuted)
                {
                    ServiceLogger.LogInformation(Resources.FirstBootComplete);
                }

                ServiceStatus.FirstBootTaskListsComplete = true;
            }

            // Every boot tasks
            try
            {
                // Find the TaskLists XML path.
                var everyBootTaskListPath = (string)(GetAppSetting(_everyBootTasksPathValue) ?? _everyBootTasksDefaultPath);
                ServiceLogger.LogInformation(string.Format(CultureInfo.CurrentCulture, Resources.CheckingForFile, everyBootTaskListPath));

                // Load every boot XML, even if we don't end up executing it, so it can be queried via FO APIs
                List<Guid> everyBootTaskListGuids = new List<Guid>();
                if (File.Exists(everyBootTaskListPath))
                {
                    BootTaskExecutionManager.SetLogFolder(Path.Combine(_taskExecutionManager.LogFolder, "EveryBootTaskLists"), false);
                    ServiceLogger.LogInformation(string.Format(CultureInfo.CurrentCulture, Resources.AttemptingFileLoad, everyBootTaskListPath));
                    // Load the first boot TaskLists file
                    everyBootTaskListGuids = BootTaskExecutionManager.LoadTaskListsFromXmlFile(everyBootTaskListPath);
                }
                else
                {
                    ServiceLogger.LogInformation(string.Format(CultureInfo.CurrentCulture, Resources.FileNotFound, everyBootTaskListPath));
                }

                // Check if every boot tasks were already completed
                bool everyBootTasksCompleted = VolatileServiceStatus.EveryBootTaskListsComplete;

                if (!everyBootTasksCompleted || force)
                {
                    everyBootTasksExecuted = true;

                    foreach (var listGuid in everyBootTaskListGuids)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        BootTaskExecutionManager.RunTaskList(listGuid);
                        ServiceLogger.LogInformation(string.Format(CultureInfo.CurrentCulture, Resources.EveryBootRunningTaskList, listGuid));
                    }
                }
                else
                {
                    ServiceLogger.LogInformation(Resources.EveryBootAlreadyComplete);
                }
            }
            catch (Exception e)
            {
                LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, $"{Resources.EveryBootFailed}! ({e.AllExceptionsToString()})"));
                everyBootTasksFailed = true;
            }

            // Wait for all tasks to complete
            sleepCount = 0;
            while (BootTaskExecutionManager.IsTaskListRunning && !cancellationToken.IsCancellationRequested)
            {
                System.Threading.Thread.Sleep(1000);
                sleepCount++;
                if (sleepCount % 15 == 0)
                {
                    ServiceLogger.LogInformation(Resources.EveryBootWaiting);
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (!everyBootTasksFailed)
            {
                // Mark every boot tasks as complete. Mark in volatile registry location so it is reset after reboot.
                if (everyBootTasksExecuted)
                {
                    ServiceLogger.LogInformation(Resources.EveryBootComplete);
                }

                VolatileServiceStatus.EveryBootTaskListsComplete = true;
            }
        }

        /// <summary>
        /// Opens a registry key.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal RegistryKey OpenOrCreateRegKey(RegKeyType type)
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

        /// <summary>
        /// Checks the given mutable and non-mutable registry keys for a given value. Mutable is always checked first.
        /// </summary>
        /// <returns>The value if it exists.</returns>
        internal object GetValueFromRegistry(string valueName, object defaultValue = null)
        {
            return GetValueFromRegistry(valueName, defaultValue, out _);
        }

        /// <summary>
        /// Checks the given mutable and non-mutable registry keys for a given value. Mutable is always checked first.
        /// </summary>
        /// <param name="valueSource">The key the value was found in.</param>
        /// <returns>The value if it exists.</returns>
        internal object GetValueFromRegistry(string valueName, object defaultValue, out RegistryKey valueSource)
        {
            object ret = null;
            valueSource = null;

            if (_mutableKey != null)
            {
                ret = _mutableKey.GetValue(valueName);
                valueSource = _mutableKey;
            }

            if ((ret == null) && (_nonMutableKey != null))
            {
                ret = _nonMutableKey.GetValue(valueName);
                valueSource = _nonMutableKey;
            }

            if (ret == null)
            {
                ret = defaultValue;
            }

            return ret;
        }

        /// <summary>
        /// Sets a given value in the registry. The mutable location is used if it exists.
        /// </summary>
        internal void SetValueInRegistry(string valueName, object value, RegistryValueKind valueKind)
        {
            if (_mutableKey != null)
            {
                _mutableKey.SetValue(valueName, value, valueKind);
            }
            else if (_nonMutableKey != null)
            {
                _nonMutableKey.SetValue(valueName, value, valueKind);
            }
        }

        /// <summary>
        /// Check if UWP local loopback needs to be enabled. Turn it on if so. Runs every boot to ensure it persists.
        /// </summary>
        /// <returns></returns>
        private bool EnableUWPLocalLoopback()
        {
            if (!_isWindows)
            {
                return true;
            }

            bool success = true;
            if (!VolatileServiceStatus.LocalLoopbackEnabled)
            {
                // Always make sure the Factory Orchestrator apps are allowed
                ServiceLogger.LogInformation(string.Format(CultureInfo.CurrentCulture, Resources.EnablingLoopback, _foAppPfn));

                try
                {
                    EnableLocalLoopbackForApp(_foAppPfn, false);
                }
                catch (Exception e)
                {
                    success = false;
                    LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, $"{string.Format(CultureInfo.CurrentCulture, Resources.EnablingLoopbackFailed, _foAppPfn)} ({e.AllExceptionsToString()})"));
                }

                ServiceLogger.LogInformation(string.Format(CultureInfo.CurrentCulture, Resources.EnablingLoopback, _foDevAppPfn));

                try
                {
                    EnableLocalLoopbackForApp(_foDevAppPfn, false);
                }
                catch (Exception e)
                {
                    success = false;
                    LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, $"{string.Format(CultureInfo.CurrentCulture, Resources.EnablingLoopbackFailed, _foDevAppPfn)} ({e.AllExceptionsToString()})"));
                }

                // Enable all other allowed apps
                foreach (var app in LocalLoopbackApps)
                {
                    ServiceLogger.LogInformation(string.Format(CultureInfo.CurrentCulture, Resources.EnablingLoopback, app));

                    try
                    {
                        EnableLocalLoopbackForApp(app, false);
                    }
                    catch (Exception e)
                    {
                        success = false;
                        LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, $"{string.Format(CultureInfo.CurrentCulture, Resources.EnablingLoopbackFailed, app)} ({e.AllExceptionsToString()})"));
                    }
                }

                VolatileServiceStatus.LocalLoopbackEnabled = true;
            }

            return success;
        }

        /// <summary>
        /// Enables local loopback for the given UWP app.
        /// </summary>
        /// <param name="pfn">The App's PFN.</param>
        internal void EnableLocalLoopbackForApp(string pfn, bool updateRegistry)
        {
            RunProcessViaCmd("checknetisolation", $"loopbackexempt -a -n={pfn}", 5000);

            if (updateRegistry)
            {
                if (!LocalLoopbackApps.Contains(pfn))
                {
                    LocalLoopbackApps.Add(pfn);

                    var loopbackAppsString = "";
                    for (int i = 0; i < LocalLoopbackApps.Count; i++)
                    {
                        var app = i == LocalLoopbackApps.Count - 1 ? LocalLoopbackApps[i] : $"{LocalLoopbackApps[i]};";
                        loopbackAppsString += app;
                    }
                    SetValueInRegistry(_localLoopbackAppsValue, loopbackAppsString + $"{pfn}", RegistryValueKind.String);
                }
            }
        }

        /// <summary>
        /// Loads OEM customizations. Still check in HKLM\System for desktop though.
        /// </summary>
        /// <returns></returns>
        private bool LoadOEMCustomizations()
        {
            // Look for appsettings.json
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(Path.Combine(FOServiceExe.ServiceExeLogFolder, "appsettings.json"), optional: true)
                .AddJsonFile("appsettings.json", optional: true);
            Appsettings = builder.Build();

            // Look for each setting in the registry (Windows only, OEM Customizations) or in the IConfiguration
            // IConfiguration takes precedence
            // If not set in either, use the default set in code below
            try
            {
                // This is exposed only as an OEM customization, not a appsetting.json value. Unlike normal uses, FactoryOS enables network access by default, and this OEM customization disables it.
                _networkAccessDisabled = Convert.ToBoolean(GetValueFromRegistry(_disableNetworkAccessValue) ?? new ArgumentNullException(), CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                _networkAccessDisabled = false;
            }

            try
            {
                _networkAccessEnabled = Convert.ToBoolean(GetAppSetting(_enableNetworkAccessValue) ?? new ArgumentNullException(), CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                _networkAccessEnabled = false;
            }

            try
            {
                DisableCommandPromptPage = Convert.ToBoolean(GetAppSetting(_disableCmdPromptValue) ?? new ArgumentNullException(), CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                DisableCommandPromptPage = false;
            }

            try
            {
                DisableFileTransferPage = Convert.ToBoolean(GetAppSetting(_disableFileTransferValue) ?? new ArgumentNullException(), CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                DisableFileTransferPage = false;
            }

            try
            {
                if (!_isWindows)
                {
                    DisableUWPAppsPage = true;
                }
                else
                {
                    DisableUWPAppsPage = Convert.ToBoolean(GetAppSetting(_disableUWPAppsValue) ?? new ArgumentNullException(), CultureInfo.InvariantCulture);
                }
            }
            catch (Exception)
            {
                DisableUWPAppsPage = false;
            }

            try
            {
                DisableManageTasklistsPage = Convert.ToBoolean(GetAppSetting(_disableTaskManagerValue) ?? new ArgumentNullException(), CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                DisableManageTasklistsPage = false;
            }

            try
            {
                if (!_isWindows)
                {
                    DisableWindowsDevicePortalPage = true;
                }
                else
                {
                    DisableWindowsDevicePortalPage = Convert.ToBoolean(GetAppSetting(_disableWindowsDevicePortalValue) ?? new ArgumentNullException(), CultureInfo.InvariantCulture);
                }
            }
            catch (Exception)
            {
                DisableWindowsDevicePortalPage = false;
            }

            // If it is set in FOServiceStatus, that means it was changed via the SetLogFolder() API, use that value.
            // Else, check if it was set in registry or JSON file.
            // Else, use the default folder.
            if (string.IsNullOrEmpty(ServiceStatus.LogFolder))
            {
                try
                {
                    ServiceStatus.LogFolder = (string)(GetAppSetting(_logFolderValue) ?? new ArgumentNullException());
                }
                catch (Exception)
                {
                    ServiceStatus.LogFolder = _defaultTaskManagerLogFolder;
                }
            }
            
            try
            {
                RunInitialTaskListsOnFirstBoot = Convert.ToBoolean(GetAppSetting(_runOnFirstBootValue) ?? new ArgumentNullException(), CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                RunInitialTaskListsOnFirstBoot = false;
            }

            if (!_isWindows || !NativeMethods.IsApiSetImplemented("api-ms-win-containers-cmclient-l1-4-0"))
            {
                // Missing required ApiSet for container support. Disable it.
                // Currently container support is restricted to Windows.
                ServiceLogger.LogInformation(Resources.ContainerSupportNotPresent);
                IsContainerSupportEnabled = false;
            }
            else
            {
                try
                {
                    IsContainerSupportEnabled = Convert.ToBoolean(GetAppSetting(_disableContainerValue) ?? new ArgumentNullException(), CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {
                    IsContainerSupportEnabled = true;
                }
            }

            string loopbackAppsString;
            try
            {
                loopbackAppsString = (string)(GetAppSetting(_localLoopbackAppsValue) ?? new ArgumentNullException());
            }
            catch (Exception)
            {
                loopbackAppsString = "";
            }

            LocalLoopbackApps = loopbackAppsString.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();

            try
            {
                NetworkPort = Convert.ToInt32(GetAppSetting(_servicePortValue) ?? new ArgumentNullException(), CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                NetworkPort = 45684;
            }

            string sslCertificateFile;
            try
            {
                sslCertificateFile = (string)(GetAppSetting(_sslCertificateFile) ?? new ArgumentNullException());
            }
            catch (Exception)
            {
                sslCertificateFile = "";
            }

            if(string.IsNullOrEmpty(sslCertificateFile))
            {
                var assm = Assembly.GetExecutingAssembly();
                string defaultCertName = "FactoryServer.pfx";
                using (Stream cs = assm.GetManifestResourceStream(assm.GetName().Name + "." + defaultCertName))
                {
                    Byte[] raw = new Byte[cs.Length];

                    for (Int32 i = 0; i < cs.Length; ++i)
                        raw[i] = (Byte)cs.ReadByte();

                    SSLCertificate = new X509Certificate2(raw);
                }
            }
            else
            {
                SSLCertificate = new X509Certificate2(sslCertificateFile);
            }

            return true;
        }

        private object GetAppSetting(string name)
        {
            object regValue = null;
            object appSettingsValue;

            if (_isWindows)
            {
                // Check registry
                try
                {
                    regValue = GetValueFromRegistry(name);
                }
                catch (Exception)
                {
                    regValue = null;
                }
            }

            // Check IConfiguration (appsettings.json)
            try
            {
                appSettingsValue = Appsettings[name];
            }
            catch (Exception)
            {
                appSettingsValue = null;
            }

            // IConfiguration takes precedence over registry
            return appSettingsValue ?? regValue;
        }

        private ServerTaskRun RunProcessViaCmd(string process, string args, int timeoutMS)
        {
            var run = (ServerTaskRun)_taskExecutionManager.RunExecutableAsBackgroundTask(@"%systemroot%\system32\cmd.exe", $"/C \"{process} {args}\"");

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
                throw new FactoryOrchestratorException(string.Format(CultureInfo.CurrentCulture, Resources.ServiceProcessStartFailed, process));
            }

            var runner = run.GetOwningTaskRunner();
            if ((runner != null) && (!runner.WaitForExit(timeoutMS)))
            {
                _taskExecutionManager.AbortTaskRun(run.Guid);
                throw new FactoryOrchestratorException(string.Format(CultureInfo.CurrentCulture, Resources.ServiceProcessTimedOut, timeoutMS));
            }

            if (run.TaskStatus != TaskStatus.Passed)
            {
                throw new FactoryOrchestratorException(string.Format(CultureInfo.CurrentCulture, Resources.ServiceProcessExitedWithError, process, run.ExitCode));
            }

            return run;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _ipcCancellationToken?.Dispose();
                    _containerHeartbeatToken?.Dispose();
                    _mutableKey?.Dispose();
                    _nonMutableKey?.Dispose();
                    _volatileKey?.Dispose();
                    _taskExecutionManager?.Dispose();
                    BootTaskExecutionManager?.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
