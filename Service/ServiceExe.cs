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
        public static readonly string ServiceLogFolder = Path.Combine(Environment.GetEnvironmentVariable("ProgramData"), "FactoryOrchestrator");

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

        public TaskList CreateTaskListFromDirectory(string path, bool recursive)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"Start: CreateTaskListFromDirectory {path} {recursive}");
                TaskList tl = FOService.Instance.TestExecutionManager.CreateTaskListFromDirectory(path, recursive);
                FOService.Instance.ServiceLogger.LogDebug($"Finish: CreateTaskListFromDirectory {path} {recursive}");
                return tl;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }

        public List<Guid> LoadTaskListsFromXmlFile(string filename)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"Start: LoadTaskListsFromXmlFile {filename}");
                List<Guid> taskLists = FOService.Instance.TestExecutionManager.LoadTaskListsFromXmlFile(filename);
                FOService.Instance.ServiceLogger.LogDebug($"Finish: LoadTaskListsFromXmlFile {filename}");
                return taskLists;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }

        public TaskList CreateTaskListFromTaskList(TaskList list)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"Start: CreateTaskListFromTaskList {list.Guid}");
                var serverList = FOService.Instance.TestExecutionManager.CreateTaskListFromTaskList(list);
                FOService.Instance.ServiceLogger.LogDebug($"Finish: CreateTaskListFromTaskList {list.Guid}");
                return serverList;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }

        public void SaveTaskListToXmlFile(Guid guid, string filename)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"Start: SaveTaskListToXmlFile {guid} {filename}");
                FOService.Instance.TestExecutionManager.SaveTaskListToXmlFile(guid, filename);
                FOService.Instance.ServiceLogger.LogDebug($"Finish: SaveTaskListToXmlFile {guid} {filename}");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }

        public void SaveAllTaskListsToXmlFile(string filename)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"Start: SaveAllTaskListsToXmlFile {filename}");
                if (!FOService.Instance.TestExecutionManager.SaveAllTaskListsToXmlFile(filename))
                {
                    throw new FactoryOrchestratorException("No TaskLists to save!");
                }
                FOService.Instance.ServiceLogger.LogDebug($"Finish: SaveAllTaskListsToXmlFile {filename}");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }

        public List<Guid> GetTaskListGuids()
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"Start: GetTaskListGuids");
                var guids = FOService.Instance.TestExecutionManager.GetTaskListGuids();
                FOService.Instance.ServiceLogger.LogDebug($"Finish: GetTaskListGuids");
                return guids;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }

        public List<TaskListSummary> GetTaskListSummaries()
        {
            try
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

                FOService.Instance.ServiceLogger.LogDebug($"Finish: GetTaskListSummaries");
                return ret;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }

        public TaskList QueryTaskList(Guid taskListGuid)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"Start: QueryTaskList {taskListGuid}");
                var list = FOService.Instance.TestExecutionManager.GetTaskList(taskListGuid);
                FOService.Instance.ServiceLogger.LogDebug($"Finish: QueryTaskList {taskListGuid}");
                return list;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }
        public TaskBase QueryTask(Guid taskGuid)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"Start: QueryTask {taskGuid}");
                var task = FOService.Instance.TestExecutionManager.GetTask(taskGuid);
                FOService.Instance.ServiceLogger.LogDebug($"Finish: QueryTask {taskGuid}");
                return task;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }

        public void DeleteTaskList(Guid listToDelete)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"Start: DeleteTaskList {listToDelete}");
                FOService.Instance.TestExecutionManager.DeleteTaskList(listToDelete);
                FOService.Instance.ServiceLogger.LogDebug($"Finish: DeleteTaskList {listToDelete}");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }

        public void ResetService(bool preserveLogs, bool factoryReset)
        {
            try
            {
                // Kill all processes including bg tasks, delete all state except registry configuration
                FOService.Instance.TestExecutionManager.Reset(preserveLogs, true);

                if (factoryReset)
                {
                    // Pause a bit to allow the IPC call to return before we kill it off
                    Task.Run(() => { System.Threading.Thread.Sleep(100); FOService.Instance.Stop(); FOService.Instance.Start(true); });
                }
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
            
        }

        public void UpdateTask(TaskBase updatedTask)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"Start: UpdateTask {updatedTask.Name} {updatedTask.Guid}");
                FOService.Instance.TestExecutionManager.UpdateTask(updatedTask);
                FOService.Instance.ServiceLogger.LogDebug($"Finish: UpdateTask {updatedTask.Name} {updatedTask.Guid}");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }

        public void UpdateTaskList(TaskList taskList)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"Start: UpdateTaskList {taskList.Guid}");
                FOService.Instance.TestExecutionManager.UpdateTaskList(taskList);
                FOService.Instance.ServiceLogger.LogDebug($"Finish: UpdateTaskList {taskList.Guid}");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }
        public void SetTeExePath(string teExePath)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"Start: SetDefaultTePath {teExePath}");
                TaskRunner.GlobalTeExePath = teExePath;
                FOService.Instance.ServiceLogger.LogDebug($"Finish: SetDefaultTePath {teExePath}");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }

        public string GetLogFolder()
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"Start: GetLogFolder");
                var folder = FOService.Instance.TestExecutionManager.LogFolder;
                FOService.Instance.ServiceLogger.LogDebug($"Finish: GetLogFolder");
                return folder;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }

        public void SetLogFolder(string logFolder, bool moveExistingLogs)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"Start: SetLogFolder {logFolder} move existing logs = {moveExistingLogs}");
                FOService.Instance.TestExecutionManager.SetLogFolder(logFolder, moveExistingLogs);

                // Set new value in registry
                RegistryKey mutableKey = null;
                try
                {
                    // OSDATA wont exist on Desktop, so try to open it on it's own
                    mutableKey = FOService.Instance.OpenOrCreateRegKey(FOService.RegKeyType.Mutable);
                }
                catch (Exception)
                {
                    mutableKey = null;
                }

                RegistryKey nonMutableKey = FOService.Instance.OpenOrCreateRegKey(FOService.RegKeyType.NonMutable);
                FOService.Instance.SetValueInRegistry(mutableKey, nonMutableKey, FOService.Instance._logFolderValue, logFolder, RegistryValueKind.String);

                FOService.Instance.ServiceLogger.LogDebug($"Finish: SetLogFolder {logFolder} move existing logs = {moveExistingLogs}");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }

        public void AbortAll()
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"Start: AbortAllTaskLists");
                FOService.Instance.TestExecutionManager.AbortAll();
                FOService.Instance.ServiceLogger.LogDebug($"Finish: AbortAllTaskLists");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }

        public void AbortTaskList(Guid taskListGuid)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"Start: AbortTaskList {taskListGuid}");
                FOService.Instance.TestExecutionManager.AbortTaskList(taskListGuid);
                FOService.Instance.ServiceLogger.LogDebug($"Finish: AbortTaskList {taskListGuid}");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }

        public void AbortTaskRun(Guid taskRunGuid)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"Start: AbortTaskRun {taskRunGuid}");
                FOService.Instance.TestExecutionManager.AbortTaskRun(taskRunGuid);
                FOService.Instance.ServiceLogger.LogDebug($"Finish: AbortTaskRun {taskRunGuid}");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }

        public string GetServiceVersionString()
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"Start: GetServiceVersionString");
                var version = FOService.GetServiceVersionString();
                FOService.Instance.ServiceLogger.LogDebug($"Finish: GetServiceVersionString");
                return version;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }

        public TaskRun QueryTaskRun(Guid taskRunGuid)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"Start: QueryTaskRun {taskRunGuid}");
                var run = FOService.Instance.TestExecutionManager.GetTaskRunByGuid(taskRunGuid).DeepCopy();
                FOService.Instance.ServiceLogger.LogDebug($"Finish: QueryTaskRun {taskRunGuid}");
                return run;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }

        public void UpdateTaskRun(TaskRun taskRun)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"Start: UpdateTaskRun {taskRun.Guid}");
                FOService.Instance.TestExecutionManager.UpdateTaskRun(taskRun);
                FOService.Instance.ServiceLogger.LogDebug($"Finish: UpdateTaskRun {taskRun.Guid}");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }

        public bool RunAllTaskLists()
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"Start: RunAllTaskLists");
                var ran = FOService.Instance.TestExecutionManager.RunAllTaskLists();
                FOService.Instance.ServiceLogger.LogDebug($"Finish: RunAllTaskLists");
                return ran;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }

        public void RunTaskList(Guid taskListToRun, int initialTaskIndex)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"Start: RunTaskList {taskListToRun}, start index: {initialTaskIndex}");
                FOService.Instance.TestExecutionManager.RunTaskListFromInitial(taskListToRun, initialTaskIndex);
                FOService.Instance.ServiceLogger.LogDebug($"Finish: RunTaskList {taskListToRun}, start index: {initialTaskIndex}");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }

        public TaskRun RunExecutable(string exeFilePath, string arguments, string logFilePath = null)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"Start: RunExecutable {exeFilePath} {arguments}");
                var run = FOService.Instance.TestExecutionManager.RunExecutableAsBackgroundTask(exeFilePath, arguments, logFilePath);
                FOService.Instance.ServiceLogger.LogDebug($"Finish: RunExecutable {exeFilePath} {arguments}");
                return run;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }

        public TaskRun RunTask(Guid taskGuid)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"Start: RunTask {taskGuid}");
                var run = FOService.Instance.TestExecutionManager.RunTask(taskGuid);
                FOService.Instance.ServiceLogger.LogDebug($"Finish: RunTask {taskGuid}");
                return run;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }

        public TaskRun RunTask(TaskBase task)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"Start: RunTask {task}");
                var run = FOService.Instance.TestExecutionManager.RunTask(task);
                FOService.Instance.ServiceLogger.LogDebug($"Finish: RunTask {task}");
                return run;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }

        public TaskRun RunApp(string packageFamilyName)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"Start: RunApp {packageFamilyName}");
                var run = FOService.Instance.TestExecutionManager.RunApp(packageFamilyName);
                FOService.Instance.ServiceLogger.LogDebug($"Finish: RunApp {packageFamilyName}");
                return run;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }

        public byte[] GetFile(string sourceFilename)
        {
            try
            {
                byte[] bytes = null;
                FOService.Instance.ServiceLogger.LogDebug($"Start: GetFile {sourceFilename}");

                if (!File.Exists(sourceFilename))
                {
                    throw new FileNotFoundException($"File {sourceFilename} requested by GetFile does not exist!");
                }

                bytes = File.ReadAllBytes(sourceFilename);

                FOService.Instance.ServiceLogger.LogDebug($"Finish: GetFile {sourceFilename}");

                return bytes;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }

        public void SendFile(string targetFilename, byte[] fileData)
        {
            FOService.Instance.ServiceLogger.LogDebug($"Start: SendFile {targetFilename}");

            try
            {
                try
                {
                    // Create target folder, if needed.
                    Directory.CreateDirectory(Path.GetDirectoryName(targetFilename));
                    File.WriteAllBytes(targetFilename, fileData);
                }
                catch (Exception e)
                {
                    throw new FactoryOrchestratorException($"Target file {targetFilename} could not be saved! {e.AllExceptionsToString()} {e.HResult}", null, e);
                }

                FOService.Instance.ServiceLogger.LogDebug($"Finish: SendFile {targetFilename}");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }

        public void DeleteFileOrFolder(string path)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"Start: DeleteFileOrFolder {path}");
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                else if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
                else
                {
                    throw new ArgumentException($"{path} is not a valid file or folder!");
                }
                FOService.Instance.ServiceLogger.LogDebug($"Finish: DeleteFileOrFolder {path}");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }

        public void MoveFileOrFolder(string sourcePath, string destinationPath)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"Start: MoveFileOrFolder {sourcePath} {destinationPath}");

                if (File.Exists(sourcePath))
                {
                    File.Move(sourcePath, destinationPath);
                }
                else if (Directory.Exists(sourcePath))
                {
                    Directory.Move(sourcePath, destinationPath);
                }
                else
                {
                    throw new ArgumentException($"{sourcePath} is not a valid file or folder!");
                }

                FOService.Instance.ServiceLogger.LogDebug($"Finish: MoveFileOrFolder {sourcePath} {destinationPath}");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }

        public List<string> EnumerateDirectories(string path, bool recursive = false)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"Start: EnumerateDirectories {path}");
                var dirs = Directory.EnumerateDirectories(path, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).ToList();
                FOService.Instance.ServiceLogger.LogDebug($"Finish: EnumerateDirectories {path}");
                return dirs;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }

        public List<string> EnumerateFiles(string path, bool recursive = false)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"Start: EnumerateFiles {path} {recursive}");
                var files = Directory.EnumerateFiles(path, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).ToList();
                FOService.Instance.ServiceLogger.LogDebug($"Finish: EnumerateFiles {path} {recursive}");
                return files;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }

        public List<string> GetInstalledApps()
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"Start: GetInstalledApps");
                var pkgManager = new PackageManager();
                // TODO: Bug: This should really check packages for the signed in user
                var packages = pkgManager.FindPackagesWithPackageTypes(PackageTypes.Main).ToList();
                var pfns = packages.Select(x => x.Id.FamilyName).ToList();
                FOService.Instance.ServiceLogger.LogDebug($"Finish: GetInstalledApps");
                return pfns;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }

        public List<Tuple<string, string>> GetIpAddressesAndNicNames()
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"Start: GetIpAddressesAndNicNames");
                List<Tuple<string, string>> ipAndNic = new List<Tuple<string, string>>();

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

                FOService.Instance.ServiceLogger.LogDebug($"Finish: GetIpAddressesAndNicNames");
                return ipAndNic;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }

        public List<string> GetDisabledPages()
        {
            try
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
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }
    }

    public class FOService : IMicroService
    {
        internal enum RegKeyType
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
        internal readonly string _logFolderValue = @"LogFolder";

        // Default log folder path
        private readonly string _defaultLogFolder = Path.Combine(FOServiceExe.ServiceLogFolder, "Logs");

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
        public string TaskManagerLogFolder { get; private set; }

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
                }
                else
                {
                    throw new FactoryOrchestratorException("FactoryOrchestratorService already created! Only one instance allowed.");
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
            // Disable inter process communication interface
            _ipcCancellationToken.Cancel();

            // Abort everything that's running, except persisted background tasks
            TestExecutionManager.AbortAll();

            // Update state file
            try
            {
                TestExecutionManager.SaveAllTaskListsToXmlFile(TestExecutionManager.TaskListStateFile);
            }
            catch (FactoryOrchestratorException e)
            {
                ServiceLogger.LogError(e, $"Unable to save TaskLists on service stop!");
            }

            ServiceLogger.LogInformation("Factory Orchestrator Service Stopped.\n");
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
        public void ExecuteServerBootTasks()
        {
            LoadOEMCustomizations();

            // Now that we know the log folder, we can create the TaskManager instance
            try
            {
                Directory.CreateDirectory(FOServiceExe.ServiceLogFolder);
            }
            catch (Exception e)
            {
                ServiceLogger.LogError($"Could not create {FOServiceExe.ServiceLogFolder} directory! {e.Message}");
                Stop();
            }
            try
            {
                Directory.CreateDirectory(TaskManagerLogFolder);
            }
            catch (Exception e)
            {
                ServiceLogger.LogError($"Could not create {TaskManagerLogFolder} directory! {e.Message}");
                Stop();
            }


            _taskExecutionManager = new TaskManager_Server(TaskManagerLogFolder, Path.Combine(FOServiceExe.ServiceLogFolder, "FactoryOrchestratorKnownTaskLists.xml"));

            // Enable local loopback every boot.
            EnableUWPLocalLoopback();
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
                            _taskExecutionManager.RunTaskList(listGuid);
                            ServiceLogger.LogInformation($"Running first boot TaskList {listGuid}...");
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
                                _taskExecutionManager.RunTaskList(listGuid);
                                ServiceLogger.LogInformation($"Running every boot TaskList {listGuid}...");
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
        internal object GetValueFromRegistry(RegistryKey mutableKey, RegistryKey nonMutableKey, string valueName, object defaultValue = null)
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
        internal void SetValueInRegistry(RegistryKey mutableKey, RegistryKey nonMutableKey, string valueName, object value, RegistryValueKind valueKind)
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

        /// <summary>
        /// Loads OEM customizations. Still check in HKLM\System for desktop though.
        /// </summary>
        /// <returns></returns>
        private bool LoadOEMCustomizations()
        {
            RegistryKey mutableKey = null;
            var nonMutableKey = OpenOrCreateRegKey(RegKeyType.NonMutable);
            try
            {
                // OSDATA wont exist on Desktop, so try to open it on it's own
                mutableKey = OpenOrCreateRegKey(RegKeyType.Mutable);
            }
            catch (Exception)
            { }

            DisableNetworkAccess = (bool)GetValueFromRegistry(mutableKey, nonMutableKey, _disableNetworkAccessValue, false);
            DisableCommandPromptPage = (bool)GetValueFromRegistry(mutableKey, nonMutableKey, _disableCmdPromptValue, false);
            DisableFileTransferPage = (bool)GetValueFromRegistry(mutableKey, nonMutableKey, _disableFileTransferValue, false);
            DisableUWPAppsPage = (bool)GetValueFromRegistry(mutableKey, nonMutableKey, _disableUWPAppsValue, false);
            DisableManageTasklistsPage = (bool)GetValueFromRegistry(mutableKey, nonMutableKey, _disableTaskManagerValue, false);
            TaskManagerLogFolder = (string)GetValueFromRegistry(mutableKey, nonMutableKey, _logFolderValue, _defaultLogFolder);

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
                throw new FactoryOrchestratorException($"{process} never started");
            }

            var runner = run.GetOwningTaskRunner();
            if ((runner != null) && (!runner.WaitForExit(5000)))
            {
                TestExecutionManager.AbortTaskRun(run.Guid);
                throw new FactoryOrchestratorException($"{process} did not exit after 5 seconds!");
            }

            if (run.TaskStatus != TaskStatus.Passed)
            {
                throw new FactoryOrchestratorException($"{process} exited with {run.ExitCode}");
            }

            return run;
        }
    }
}