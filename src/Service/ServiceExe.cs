// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using PeterKottas.DotNetCore.WindowsService;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using PeterKottas.DotNetCore.WindowsService.Interfaces;
using System;
using Microsoft.FactoryOrchestrator.Core;
using Microsoft.FactoryOrchestrator.Server;
using System.Reflection;
using System.Linq;
using Microsoft.Win32;
using System.IO;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using TaskStatus = Microsoft.FactoryOrchestrator.Core.TaskStatus;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using Microsoft.FactoryOrchestrator.Client;
using System.Globalization;
using JKang.IpcServiceFramework.Hosting;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Reflection.Metadata.Ecma335;

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

    public sealed class FOServiceExe
    {
        public static IHost ipcHost { get; internal set; }
        private static ServiceProvider ipcSvcProvider;
        public static readonly string ServiceLogFolder = Path.Combine(Environment.GetEnvironmentVariable("ProgramData"), "FactoryOrchestrator");

        public static IHost CreateHost(string[] args, bool allowNetworkAccess) =>
              Host.CreateDefaultBuilder(args)
                  .ConfigureServices(services =>
                  {
                      services.AddScoped<IFactoryOrchestratorService, FOCommunicationHandler>();
                  })
                  .ConfigureIpcHost(builder =>
                  {
                      // configure IPC endpoints
                      if (allowNetworkAccess)
                      {
                          builder.AddTcpEndpoint<IFactoryOrchestratorService>(options =>
                          {
                              options.IpEndpoint = IPAddress.Any;
                              options.Port = 45684;
                              options.IncludeFailureDetailsInResponse = true;
                              options.MaxConcurrentCalls = 5;
                          });
                      }
                      else
                      {
                          builder.AddTcpEndpoint<IFactoryOrchestratorService>(options =>
                          {
                              options.IpEndpoint = IPAddress.Loopback;
                              options.Port = 45684;
                              options.IncludeFailureDetailsInResponse = true;
                              options.MaxConcurrentCalls = 5;
                          });
                      }
                  })
                  .ConfigureLogging(builder =>
                  {
                        // optionally configure logging
                        builder.SetMinimumLevel(LogLevel.Trace);
                  }).Build();

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


          

            // Configure service providers for logger creation and managment
            ipcSvcProvider = servicesIpc
                .AddLogging(builder =>
                {
                    // Only log IPC framework errors
                    builder
                    .SetMinimumLevel(LogLevel.Error).AddConsole().AddProvider(new LogFileProvider());

                })
                .AddOptions()
#pragma warning disable CA2000 // Dispose objects before losing scope
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
#pragma warning restore CA2000 // Dispose objects before losing scope
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
                        return new FOService(foSvcProvider.GetRequiredService<ILoggerFactory>().CreateLogger<FOService>());
                    });

                    serviceConfig.OnStart((service, extraParams) =>
                    {
                        _logger.LogInformation(Resources.ServiceStarting, name);
                        service.Start();
                        _logger.LogInformation(Resources.ServiceStarted, name);
                    });

                    serviceConfig.OnStop(service =>
                    {
                        _logger.LogInformation(Resources.ServiceStopping, name);
                        service.Stop();
                        _logger.LogInformation(Resources.ServiceStopped, name);
                    });

                    serviceConfig.OnError(e =>
                    {
                        _logger?.LogCritical(e, string.Format(CultureInfo.CurrentCulture, Resources.ServiceErrored, name));
                        throw e;
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

        public bool IsExecutingBootTasks()
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: IsExecutingBootTasks");

                var ret = FOService.Instance.IsExecutingBootTasks;

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: IsExecutingBootTasks");
                return ret;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public TaskList CreateTaskListFromDirectory(string path, bool recursive)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: CreateTaskListFromDirectory {path} {recursive}");

                if (FOService.Instance.IsExecutingBootTasks)
                {
                    throw new FactoryOrchestratorException(Resources.BootTasksExecutingError);
                }

                path = Environment.ExpandEnvironmentVariables(path);
                TaskList tl = FOService.Instance.TaskExecutionManager.CreateTaskListFromDirectory(path, recursive);

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: CreateTaskListFromDirectory {path} {recursive}");
                return tl;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public List<Guid> LoadTaskListsFromXmlFile(string filename)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: LoadTaskListsFromXmlFile {filename}");

                if (FOService.Instance.IsExecutingBootTasks)
                {
                    throw new FactoryOrchestratorException(Resources.BootTasksExecutingError);
                }

                filename = Environment.ExpandEnvironmentVariables(filename);
                List<Guid> taskLists = FOService.Instance.TaskExecutionManager.LoadTaskListsFromXmlFile(filename);

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: LoadTaskListsFromXmlFile {filename}");
                return taskLists;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public TaskList CreateTaskListFromTaskList(TaskList list)
        {
            try
            {
                if (list == null)
                {
                    throw new ArgumentNullException(nameof(list));
                }

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: CreateTaskListFromTaskList {list.Guid}");

                if (FOService.Instance.IsExecutingBootTasks)
                {
                    throw new FactoryOrchestratorException(Resources.BootTasksExecutingError);
                }

                var serverList = FOService.Instance.TaskExecutionManager.CreateTaskListFromTaskList(list);
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: CreateTaskListFromTaskList {list.Guid}");
                return serverList;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public void SaveTaskListToXmlFile(Guid guid, string filename)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: SaveTaskListToXmlFile {guid} {filename}");

                if (FOService.Instance.IsExecutingBootTasks)
                {
                    throw new FactoryOrchestratorException(Resources.BootTasksExecutingError);
                }

                filename = Environment.ExpandEnvironmentVariables(filename);
                FOService.Instance.TaskExecutionManager.SaveTaskListToXmlFile(guid, filename);

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: SaveTaskListToXmlFile {guid} {filename}");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public void SaveAllTaskListsToXmlFile(string filename)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: SaveAllTaskListsToXmlFile {filename}");

                if (FOService.Instance.IsExecutingBootTasks)
                {
                    throw new FactoryOrchestratorException(Resources.BootTasksExecutingError);
                }

                filename = Environment.ExpandEnvironmentVariables(filename);
                if (!FOService.Instance.TaskExecutionManager.SaveAllTaskListsToXmlFile(filename))
                {
                    throw new FactoryOrchestratorException(Resources.NoTaskListsException);
                }

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: SaveAllTaskListsToXmlFile {filename}");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public List<Guid> GetTaskListGuids()
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: GetTaskListGuids");
                var guids = FOService.Instance.TaskExecutionManager.GetTaskListGuids();
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: GetTaskListGuids");
                return guids;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public List<TaskListSummary> GetTaskListSummaries()
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: GetTaskListSummaries");
                var guids = FOService.Instance.TaskExecutionManager.GetTaskListGuids();
                var ret = new List<TaskListSummary>();
                foreach (var guid in guids)
                {
                    var list = FOService.Instance.TaskExecutionManager.GetTaskList(guid);
                    if (list != null)
                    {
                        ret.Add(new TaskListSummary(guid, list.Name, list.TaskListStatus, list.RunInParallel, list.AllowOtherTaskListsToRun, list.TerminateBackgroundTasksOnCompletion));
                    }
                }

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: GetTaskListSummaries");
                return ret;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public List<Guid> GetBootTaskListGuids()
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: GetBootTaskListGuids");
                var guids = FOService.Instance.BootTaskExecutionManager.GetTaskListGuids();
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: GetBootTaskListGuids");
                return guids;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public List<TaskListSummary> GetBootTaskListSummaries()
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: GetBootTaskListSummaries");
                var guids = FOService.Instance.BootTaskExecutionManager.GetTaskListGuids();
                var ret = new List<TaskListSummary>();
                foreach (var guid in guids)
                {
                    var list = FOService.Instance.BootTaskExecutionManager.GetTaskList(guid);
                    if (list != null)
                    {
                        ret.Add(new TaskListSummary(guid, list.Name, list.TaskListStatus, list.RunInParallel, list.AllowOtherTaskListsToRun, list.TerminateBackgroundTasksOnCompletion));
                    }
                }

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: GetBootTaskListSummaries");
                return ret;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public TaskList QueryTaskList(Guid taskListGuid)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: QueryTaskList {taskListGuid}");

                // Check active task manager, then boot task manager
                var list = FOService.Instance.TaskExecutionManager.GetTaskList(taskListGuid);
                if (list == null)
                {
                    list = FOService.Instance.BootTaskExecutionManager.GetTaskList(taskListGuid);
                }

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: QueryTaskList {taskListGuid}");
                return list;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public TaskBase QueryTask(Guid taskGuid)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: QueryTask {taskGuid}");

                // Check active task manager, then boot task manager
                var task = FOService.Instance.TaskExecutionManager.GetTask(taskGuid);

                if (task == null)
                {
                    task = FOService.Instance.BootTaskExecutionManager.GetTask(taskGuid);
                }
                if (task == null)
                {
                    throw new FactoryOrchestratorUnkownGuidException(taskGuid);
                }

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: QueryTask {taskGuid}");
                return task;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public TaskRun QueryTaskRun(Guid taskRunGuid)
        {
            try
            {
                TaskRun ret = null;
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: QueryTaskRun {taskRunGuid}");

                // This performs a recursive search that will include both "normal" and "boot" tasks, so BootTaskExecutionManager doesn't need to be queried.
                var run = FOService.Instance.TaskExecutionManager.GetTaskRunByGuid(taskRunGuid);

                if (run != null)
                {
                    ret = run.DeepCopy();
                }

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: QueryTaskRun {taskRunGuid}");
                return ret;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public void DeleteTaskList(Guid listToDelete)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: DeleteTaskList {listToDelete}");

                if (FOService.Instance.IsExecutingBootTasks)
                {
                    throw new FactoryOrchestratorException(Resources.BootTasksExecutingError);
                }

                FOService.Instance.TaskExecutionManager.DeleteTaskList(listToDelete);
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: DeleteTaskList {listToDelete}");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public void ReorderTaskLists(List<Guid> newOrder)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: ReorderTaskLists {newOrder}");

                if (FOService.Instance.IsExecutingBootTasks)
                {
                    throw new FactoryOrchestratorException(Resources.BootTasksExecutingError);
                }

                FOService.Instance.TaskExecutionManager.ReorderTaskLists(newOrder);
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: ReorderTaskLists {newOrder}");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public void ResetService(bool preserveLogs, bool factoryReset)
        {
            try
            {
                if (FOService.Instance.IsExecutingBootTasks)
                {
                    throw new FactoryOrchestratorException(Resources.BootTasksExecutingError);
                }

                // Kill all processes including bg tasks, delete all state except registry configuration
                FOService.Instance.TaskExecutionManager.Reset(preserveLogs, true);

                if (factoryReset)
                {
                    // Pause a bit to allow the IPC call to return before we kill it off
                    Task.Run(() => { System.Threading.Thread.Sleep(500); FOService.Instance.Stop(); FOService.Instance.Start(true); });
                }
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public void UpdateTaskList(TaskList taskList)
        {
            try
            {
                if (taskList == null)
                {
                    throw new ArgumentNullException(nameof(taskList));
                }

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: UpdateTaskList {taskList.Guid}");

                if (FOService.Instance.IsExecutingBootTasks)
                {
                    throw new FactoryOrchestratorException(Resources.BootTasksExecutingError);
                }

                FOService.Instance.TaskExecutionManager.UpdateTaskList(taskList);
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: UpdateTaskList {taskList.Guid}");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public void SetTeExePath(string teExePath)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: SetDefaultTePath {teExePath}");

                if (FOService.Instance.IsExecutingBootTasks)
                {
                    throw new FactoryOrchestratorException(Resources.BootTasksExecutingError);
                }

                teExePath = Environment.ExpandEnvironmentVariables(teExePath);
                TaskRunner.GlobalTeExePath = teExePath;
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: SetDefaultTePath {teExePath}");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public string GetLogFolder()
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: GetLogFolder");
                var folder = FOService.Instance.TaskExecutionManager.LogFolder;
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: GetLogFolder");
                return folder;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public void SetLogFolder(string logFolder, bool moveExistingLogs)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: SetLogFolder {logFolder} {moveExistingLogs}");

                if (FOService.Instance.IsExecutingBootTasks)
                {
                    throw new FactoryOrchestratorException(Resources.BootTasksExecutingError);
                }

                logFolder = Environment.ExpandEnvironmentVariables(logFolder);
                FOService.Instance.TaskExecutionManager.SetLogFolder(logFolder, moveExistingLogs);

                // Set new value in registry
                FOService.Instance.SetValueInRegistry(FOService.Instance._logFolderValue, logFolder, RegistryValueKind.String);

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: SetLogFolder {logFolder} {moveExistingLogs}");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public void AbortAll()
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: AbortAllTaskLists");
                FOService.Instance.TaskExecutionManager.AbortAll();
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: AbortAllTaskLists");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public void AbortTaskList(Guid taskListGuid)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: AbortTaskList {taskListGuid}");
                FOService.Instance.TaskExecutionManager.AbortTaskList(taskListGuid);
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: AbortTaskList {taskListGuid}");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public void AbortTaskRun(Guid taskRunGuid)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: AbortTaskRun {taskRunGuid}");
                FOService.Instance.TaskExecutionManager.AbortTaskRun(taskRunGuid);
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: AbortTaskRun {taskRunGuid}");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public string GetServiceVersionString()
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: GetServiceVersionString");
                var version = FOService.GetServiceVersionString();
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: GetServiceVersionString");
                return version;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public string GetOSVersionString()
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: GetOSVersionString");
                var version = FOService.GetOSVersionString();
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: GetOSVersionString");
                return version;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public string GetOEMVersionString()
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: GetOEMVersionString");
                var version = FOService.GetOEMVersionString();
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: GetOEMVersionString");
                return version;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public void UpdateTaskRun(TaskRun taskRun)
        {
            try
            {
                if (taskRun == null)
                {
                    throw new ArgumentNullException(nameof(taskRun));
                }

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: UpdateTaskRun {taskRun.Guid}");

                if (FOService.Instance.IsExecutingBootTasks)
                {
                    throw new FactoryOrchestratorException(Resources.BootTasksExecutingError);
                }

                FOService.Instance.TaskExecutionManager.UpdateTaskRun(taskRun);
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: UpdateTaskRun {taskRun.Guid}");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public bool RunAllTaskLists()
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: RunAllTaskLists");

                if (FOService.Instance.IsExecutingBootTasks)
                {
                    throw new FactoryOrchestratorException(Resources.BootTasksExecutingError);
                }

                var ran = FOService.Instance.TaskExecutionManager.RunAllTaskLists();
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: RunAllTaskLists");
                return ran;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public void RunTaskList(Guid taskListToRun, int initialTaskIndex)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: RunTaskList {taskListToRun} {initialTaskIndex}");

                if (FOService.Instance.IsExecutingBootTasks)
                {
                    throw new FactoryOrchestratorException(Resources.BootTasksExecutingError);
                }

                FOService.Instance.TaskExecutionManager.RunTaskListFromInitial(taskListToRun, initialTaskIndex);
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: RunTaskList {taskListToRun} {initialTaskIndex}");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public TaskRun RunExecutable(string exeFilePath, string arguments, string logFilePath = null, bool runInContainer = false)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: RunExecutable {exeFilePath} {arguments}");

                if (FOService.Instance.IsExecutingBootTasks)
                {
                    throw new FactoryOrchestratorException(Resources.BootTasksExecutingError);
                }

                var run = FOService.Instance.TaskExecutionManager.RunExecutableAsBackgroundTask(exeFilePath, arguments, logFilePath, runInContainer);
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: RunExecutable {exeFilePath} {arguments}");
                return run.DeepCopy() as TaskRun;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public TaskRun RunTask(Guid taskGuid)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: RunTask {taskGuid}");

                if (FOService.Instance.IsExecutingBootTasks)
                {
                    throw new FactoryOrchestratorException(Resources.BootTasksExecutingError);
                }

                var run = FOService.Instance.TaskExecutionManager.RunTask(taskGuid);
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: RunTask {taskGuid}");
                return run;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public TaskRun RunTask(TaskBase task)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: RunTask {task}");

                if (FOService.Instance.IsExecutingBootTasks)
                {
                    throw new FactoryOrchestratorException(Resources.BootTasksExecutingError);
                }

                var run = FOService.Instance.TaskExecutionManager.RunTask(task);
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: RunTask {task}");
                return run.DeepCopy();
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public TaskRun RunApp(string aumid)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: RunApp {aumid}");

                if (FOService.Instance.IsExecutingBootTasks)
                {
                    throw new FactoryOrchestratorException(Resources.BootTasksExecutingError);
                }

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    throw new FactoryOrchestratorException(string.Format(CultureInfo.CurrentCulture, Resources.WindowsOnlyError, "RunApp"));
                }

                var run = FOService.Instance.TaskExecutionManager.RunApp(aumid);
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: RunApp {aumid}");
                return run.DeepCopy();
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public byte[] GetFile(string sourceFilename, long offset, int count, bool getFromContainer = false)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: GetFile {sourceFilename} {getFromContainer}");
                var bytes = FOService.Instance.GetFile(sourceFilename, offset, count, getFromContainer);

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: GetFile {sourceFilename} {getFromContainer}");

                return bytes;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public void SendFile(string targetFilename, byte[] fileData, bool appending = false, bool sendToContainer = false)
        {
            FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: SendFile {targetFilename} {appending} {sendToContainer}");

            try
            {
                try
                {
                     FOService.Instance.SendFile(targetFilename, fileData, appending, sendToContainer);
                }
                catch (Exception e)
                {
                    throw new FactoryOrchestratorException($"{string.Format(CultureInfo.CurrentCulture, Resources.FileSaveError, targetFilename)} {e.AllExceptionsToString()} {e.HResult}", null, e);
                }

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: SendFile {targetFilename} {appending} {sendToContainer}");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public void DeleteFileOrFolder(string path, bool deleteInContainer = false)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: DeleteFileOrFolder {path} {deleteInContainer}");
                if (deleteInContainer)
                {
                    FOService.Instance.DeleteInContainer(path).Wait();
                }
                else
                {
                    path = Environment.ExpandEnvironmentVariables(path);
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
                        throw new FileNotFoundException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidPathError, path));
                    }
                }

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: DeleteFileOrFolder {path} {deleteInContainer}");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public void MoveFileOrFolder(string sourcePath, string destinationPath, bool moveInContainer = false)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: MoveFileOrFolder {sourcePath} {destinationPath} {moveInContainer}");

                if (moveInContainer)
                {
                    FOService.Instance.MoveInContainer(sourcePath, destinationPath).Wait();
                }
                else
                {
                    sourcePath = Environment.ExpandEnvironmentVariables(sourcePath);
                    destinationPath = Environment.ExpandEnvironmentVariables(destinationPath);
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
                        throw new FileNotFoundException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidPathError, sourcePath));
                    }
                }

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: MoveFileOrFolder {sourcePath} {destinationPath} {moveInContainer}");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public List<string> EnumerateDirectories(string path, bool recursive = false, bool inContainer = false)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: EnumerateDirectories {path}");

                List<string> dirs;

                if (inContainer)
                {
                    dirs = FOService.Instance.EnumerateDirectoriesInContainer(path, recursive).Result;
                }
                else
                {
                    path = Environment.ExpandEnvironmentVariables(path);
                    dirs = Directory.EnumerateDirectories(path, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).ToList();
                }

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: EnumerateDirectories {path}");
                return dirs;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public List<string> EnumerateFiles(string path, bool recursive = false, bool inContainer = false)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: EnumerateFiles {path} {recursive}");

                List<string> files;

                if (inContainer)
                {
                    files = FOService.Instance.EnumerateFilesInContainer(path, recursive).Result;
                }
                else
                {
                    path = Environment.ExpandEnvironmentVariables(path);
                    files = Directory.EnumerateFiles(path, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).ToList();
                }

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: EnumerateFiles {path} {recursive}");
                return files;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public void InstallApp(string appPackagePath, List<string> dependentPackages = null, string certificateFile = null)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: InstallApp {appPackagePath}");

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    throw new FactoryOrchestratorException(string.Format(CultureInfo.CurrentCulture, Resources.WindowsOnlyError, "InstallApp"));
                }

                // Expand any vars
                appPackagePath = Environment.ExpandEnvironmentVariables(appPackagePath);
                certificateFile = Environment.ExpandEnvironmentVariables(certificateFile);

                List<Uri> dependentPackagesUris = new List<Uri>();
                if (dependentPackages != null)
                {
                    for (int i = 0; i < dependentPackages.Count; i++)
                    {
                        var dep = Environment.ExpandEnvironmentVariables(dependentPackages[i]);
                        dependentPackagesUris.Add(new Uri(dep));
                    }
                }

                WDPHelpers.InstallAppWithWDP(appPackagePath, dependentPackages, certificateFile).Wait();

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: InstallApp {appPackagePath}");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public List<string> GetInstalledApps()
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: GetInstalledApps");

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    throw new FactoryOrchestratorException(string.Format(CultureInfo.CurrentCulture, Resources.WindowsOnlyError, "GetInstalledApps"));
                }

                // Get installed packages on the system
                var response = WDPHelpers.WdpHttpClient.GetAsync(new Uri("http://localhost/api/app/packagemanager/packages")).Result;

                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(Resources.WDPNotRunningError);
                }

                List<string> aumids = new List<string>();
                var output = response.Content.ReadAsStringAsync().Result;
                var matches = Regex.Matches(output, "\"PackageRelativeId\" : \"(.+?)\"");
                foreach (Match match in matches)
                {
                    aumids.Add(match.Groups[1].Value);
                }

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: GetInstalledApps");
                return aumids;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public void EnableLocalLoopbackForApp(string aumid)
        {
            try
            {
                if (aumid == null)
                {
                    throw new ArgumentNullException(nameof(aumid));
                }

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: EnableLocalLoopbackForApp {aumid}");
                var index = aumid.IndexOf('!', StringComparison.OrdinalIgnoreCase);

                if (index == -1)
                {
                    throw new InvalidDataException(Resources.AUMIDNotValidError);
                }

                var pfn = aumid.Substring(0, index);
                FOService.Instance.EnableLocalLoopbackForApp(pfn, true);
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: EnableLocalLoopbackForApp {aumid}");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public List<Tuple<string, string>> GetIpAddressesAndNicNames()
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: GetIpAddressesAndNicNames");
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

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: GetIpAddressesAndNicNames");
                return ipAndNic;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public List<string> GetContainerIpAddresses()
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: GetContainerIpAddresses");

                if (!FOService.Instance.IsContainerSupportEnabled)
                {
                    throw new FactoryOrchestratorContainerDisabledException(Resources.ContainerDisabledException);
                }

                // As of now, there is only one IP for the container but keep this as a list in case that changes
                var ip = FOService.Instance.ContainerIpAddress;
                var ipStrings = new List<string>();
                if (ip != null)
                {
                    ipStrings.Add(ip.ToString());
                }

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: GetContainerIpAddresses");
                return ipStrings;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public List<string> GetDisabledPages()
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: GetDisabledPages");
                List<string> ret = new List<string>();

                if (FOService.Instance.DisableCommandPromptPage)
                {
                    // values must match "Tag" on MainPage.xaml
                    ret.Add("console");
                }
                if (FOService.Instance.DisableWindowsDevicePortalPage)
                {
                    ret.Add("wdp");
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

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: GetDisabledPages");
                return ret;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public bool IsContainerRunning()
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: IsContainerRunning");

                if (!FOService.Instance.IsContainerSupportEnabled)
                {
                    throw new FactoryOrchestratorContainerDisabledException(Resources.ContainerDisabledException);
                }

                bool ret = FOService.Instance.IsContainerConnected;
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: IsContainerRunning");
                return ret;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public bool IsNetworkAccessEnabled()
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: IsNetworkAccessEnabled");
                bool ret = FOService.Instance.IsNetworkAccessEnabled;
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: IsNetworkAccessEnabled");
                return ret;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }
    }

    public class FOService : IMicroService, IDisposable
    {
        internal enum RegKeyType
        {
            NonMutable,
            Mutable,
            Volatile
        }

        private static FOService _singleton = null;
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
        private readonly string _loopbackEnabledValue = @"UWPLocalLoopbackEnabled";

        // OEM Customization registry values
        private readonly string _disableNetworkAccessValue = @"DisableNetworkAccess";
        private readonly string _enableNetworkAccessValue = @"EnableNetworkAccess";
        private readonly string _disableCmdPromptValue = @"DisableCommandPromptPage";
        private readonly string _disableWindowsDevicePortalValue = @"DisableWindowsDevicePortalPage";
        private readonly string _disableUWPAppsValue = @"DisableUWPAppsPage";
        private readonly string _disableTaskManagerValue = @"DisableManageTasklistsPage";
        private readonly string _disableFileTransferValue = @"DisableFileTransferPage";
        private readonly string _localLoopbackAppsValue = @"AllowedLocalLoopbackApps";
        private readonly string _runOnFirstBootValue = @"RunInitialTaskListsOnFirstBoot";
        internal readonly string _logFolderValue = @"LogFolder";

        // Default log folder path
        private readonly string _defaultLogFolder = Path.Combine(FOServiceExe.ServiceLogFolder, "Logs");

        // Default paths in testcontent directory for user tasklists
        private readonly string _initialTasksDefaultPath = Environment.ExpandEnvironmentVariables(@"%DataDrive%\TestContent\InitialTaskLists.xml");
        private readonly string _firstBootTasksDefaultPath = Environment.ExpandEnvironmentVariables(@"%DataDrive%\TestContent\FirstBootTasks.xml");
        private readonly string _everyBootTasksDefaultPath = Environment.ExpandEnvironmentVariables(@"%DataDrive%\TestContent\EveryBootTasks.xml");

        // Registry fallbacks for user tasklists
        private readonly string _firstBootTasksPathValue = @"FirstBootTaskListsXML";
        private readonly string _everyBootTasksPathValue = @"EveryBootTaskListsXML";
        private readonly string _initialTasksPathValue = @"FirstBootStateTaskListsXML";

        // user tasklists state registry values
        private readonly string _firstBootCompleteValue = @"FirstBootTaskListsComplete";
        private readonly string _everyBootCompleteValue = @"EveryBootTaskListsComplete";
        private readonly string _firstBootStateLoadedValue = @"FirstBootStateLoaded";

        // PFNs for Factory Orchestrator
        private readonly string _foAppPfn = "Microsoft.FactoryOrchestratorApp_8wekyb3d8bbwe";
        private readonly string _foDevAppPfn = "Microsoft.FactoryOrchestratorApp.DEV_8wekyb3d8bbwe";

        private RegistryKey _mutableKey;
        private RegistryKey _nonMutableKey;
        private RegistryKey _volatileKey;

        private FactoryOrchestratorClient _containerClient;

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
        public string TaskManagerLogFolder { get; private set; }

        /// <summary>
        /// The service logger for FactoryOrchestrator (FOService).
        /// </summary>
        /// <value>
        /// The service logger.
        /// </value>
        public ILogger<FOService> ServiceLogger { get; private set; }

        public Dictionary<ulong, ServiceEvent> ServiceEvents { get; private set;  }
        public ulong LastEventIndex { get; private set; }
        public DateTime LastEventTime { get; private set; }
        public bool DisableCommandPromptPage { get; private set; }
        public bool DisableWindowsDevicePortalPage { get; private set; }
        public bool DisableUWPAppsPage { get; private set; }
        public bool DisableManageTasklistsPage { get; private set; }
        public bool DisableFileTransferPage { get; private set; }
        public bool IsNetworkAccessEnabled { get => _networkAccessEnabled && !_networkAccessDisabled; }
        public int ServiceNetworkPort { get; private set; }
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
            using var reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", false);
            var version = (string)reg.GetValue("BuildLabEx");
            return version;
        }

        /// <summary>
        /// Returns OEM version string, set as WCOS OEM Customization.
        /// </summary>
        /// <returns></returns>
        public static string GetOEMVersionString()
        {
            using var reg = Registry.LocalMachine.OpenSubKey(@"OSDATA\CurrentControlSet\Control\FactoryOrchestrator", false);
            var version = (string)reg?.GetValue("OEMVersion", null);
            return version;
        }

        public FOService(ILogger<FOService> logger)
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

            // Start IPC server on port 45684. Only start after all boot tasks are complete.
            FOServiceExe.ipcHost = FOServiceExe.CreateHost(null, IsNetworkAccessEnabled);
            _ipcCancellationToken = new System.Threading.CancellationTokenSource();
            FOServiceExe.ipcHost.RunAsync(_ipcCancellationToken.Token);

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

            // Execute user defined tasks.
            ExecuteUserBootTasks(forceUserTaskRerun);

            IsExecutingBootTasks = false;
            LogServiceEvent(new ServiceEvent(ServiceEventType.BootTasksComplete, null, Resources.BootTasksFinished));

            if (RunInitialTaskListsOnFirstBoot && firstBootFileLoaded)
            {
                _taskExecutionManager.RunAllTaskLists();
            }
        }

        private bool LoadFirstBootStateFile(bool force)
        {
            bool loaded = false;

            try
            {
                var firstBootStateLoaded = GetValueFromRegistry(_firstBootStateLoadedValue) as int?;

                if ((firstBootStateLoaded == null) || (firstBootStateLoaded == 0) || (force))
                {
                    string firstBootStateTaskListPath = _initialTasksDefaultPath;

                    ServiceLogger.LogInformation(string.Format(CultureInfo.CurrentCulture, Resources.CheckingForFile, _initialTasksDefaultPath));
                    // Find the TaskLists XML path. Check testcontent directory for wellknown name, fallback to registry
                    if (!File.Exists(firstBootStateTaskListPath))
                    {
                        firstBootStateTaskListPath = GetValueFromRegistry(_initialTasksPathValue, _initialTasksDefaultPath) as string;
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
                        SetValueInRegistry(_firstBootStateLoadedValue, 1, RegistryValueKind.DWord);
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
            _containerHeartbeatToken.Cancel();
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
                    var containerRun = await _containerClient.RunTask(containerTask);
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
                            // We were connected already. Verify it is still working properly.
                            await _containerClient.GetServiceVersionString();
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
                        _containerClient = new FactoryOrchestratorClient(ContainerIpAddress, ServiceNetworkPort);
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
        public void ExecuteServerBootTasks()
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

            ServiceEvents = new Dictionary<ulong, ServiceEvent>();
            LastEventIndex = 0;
            LastEventTime = DateTime.MinValue;
            LocalLoopbackApps = new List<string>();
            IsExecutingBootTasks = true;
            ServiceNetworkPort = 45684;
            _openedFiles = new Dictionary<string, (Stream stream, System.Threading.Timer timer)>();

            ContainerGuid = Guid.Empty;
            ContainerIpAddress = null;
            _containerHeartbeatToken = null;
            _containerClient = null;

            LoadOEMCustomizations();

            // Now that we know the log folder, we can create the TaskManager instance
            try
            {
                Directory.CreateDirectory(FOServiceExe.ServiceLogFolder);
            }
            catch (Exception e)
            {
                ServiceLogger.LogError($"{string.Format(CultureInfo.CurrentCulture, Resources.CreateDirectoryFailed, FOServiceExe.ServiceLogFolder)} {e.Message}");
                throw;
            }
            try
            {
                Directory.CreateDirectory(TaskManagerLogFolder);
            }
            catch (Exception e)
            {
                ServiceLogger.LogError($"{string.Format(CultureInfo.CurrentCulture, Resources.CreateDirectoryFailed, TaskManagerLogFolder)} {e.Message}");
                throw;
            }

            _taskExecutionManager = new TaskManager(TaskManagerLogFolder, Path.Combine(FOServiceExe.ServiceLogFolder, "FactoryOrchestratorKnownTaskLists.xml"));
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

            // Enable local loopback every boot.
            EnableUWPLocalLoopback();
        }

        /// <summary>
        /// Executes user defined tasks that should run on first boot (of FactoryOrchestrator) or every boot.
        /// </summary>
        /// <returns></returns>
        public void ExecuteUserBootTasks(bool force)
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
                var firstBootTaskListPath = (string)GetValueFromRegistry(_firstBootTasksPathValue, _firstBootTasksDefaultPath);
                ServiceLogger.LogInformation(string.Format(CultureInfo.CurrentCulture, Resources.CheckingForFile, _firstBootTasksDefaultPath));

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
                var firstBootTasksCompleted = GetValueFromRegistry(_firstBootCompleteValue) as int?;
                if ((firstBootTasksCompleted == null) || (firstBootTasksCompleted == 0) || (force == true))
                {
                    firstBootTasksExecuted = true;

                    foreach (var listGuid in firstBootTaskListGuids)
                    {
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
            while (BootTaskExecutionManager.IsTaskListRunning)
            {
                System.Threading.Thread.Sleep(1000);
                sleepCount++;
                if (sleepCount % 15 == 0)
                {
                    ServiceLogger.LogInformation(Resources.FirstBootWaiting);
                }
            }

            if (!firstBootTasksFailed)
            {
                // Mark first boot tasks as complete
                if (firstBootTasksExecuted)
                {
                    ServiceLogger.LogInformation(Resources.FirstBootComplete);
                }

                SetValueInRegistry(_firstBootCompleteValue, 1, RegistryValueKind.DWord);
            }

            // Every boot tasks
            try
            {
                // Find the TaskLists XML path.
                var everyBootTaskListPath = (string)GetValueFromRegistry(_everyBootTasksPathValue, _everyBootTasksDefaultPath);
                ServiceLogger.LogInformation(string.Format(CultureInfo.CurrentCulture, Resources.CheckingForFile, _everyBootTasksDefaultPath));

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
                var everyBootTasksCompleted = GetValueFromRegistry(_everyBootCompleteValue) as int?;
                if ((everyBootTasksCompleted == null) || (everyBootTasksCompleted == 0) || (force == true))
                {
                    everyBootTasksExecuted = true;

                    foreach (var listGuid in everyBootTaskListGuids)
                    {
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
            while (BootTaskExecutionManager.IsTaskListRunning)
            {
                System.Threading.Thread.Sleep(1000);
                sleepCount++;
                if (sleepCount % 15 == 0)
                {
                    ServiceLogger.LogInformation(Resources.EveryBootWaiting);
                }
            }

            if (!everyBootTasksFailed)
            {
                // Mark every boot tasks as complete. Mark in volatile registry location so it is reset after reboot.
                if (everyBootTasksExecuted)
                {
                    ServiceLogger.LogInformation(Resources.EveryBootComplete);
                }

                _volatileKey.SetValue(_everyBootCompleteValue, 1, RegistryValueKind.DWord);
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
            object ret = null;

            if (_mutableKey != null)
            {
                ret = _mutableKey.GetValue(valueName);
            }
            
            if ((ret == null) && (_nonMutableKey != null))
            {
                ret = _nonMutableKey.GetValue(valueName);
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
            bool success = true;
            var loopbackEnabled = (int)_volatileKey.GetValue(_loopbackEnabledValue, 0);
            if (loopbackEnabled == 0)
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


                _volatileKey.SetValue(_loopbackEnabledValue, 1, RegistryValueKind.DWord);
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
            try
            {
                _networkAccessDisabled = Convert.ToBoolean(GetValueFromRegistry(_disableNetworkAccessValue) ?? throw new ArgumentNullException("GetValueFromRegistry(_disableNetworkAccessValue)"), CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                _networkAccessDisabled = true;
            }

            try
            {
                _networkAccessEnabled = Convert.ToBoolean(GetValueFromRegistry(_enableNetworkAccessValue) ?? throw new ArgumentNullException("GetValueFromRegistry(_enableNetworkAccessValue)"), CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                _networkAccessEnabled = false;
            }

            try
            {
                DisableCommandPromptPage = Convert.ToBoolean(GetValueFromRegistry(_disableCmdPromptValue) ?? throw new ArgumentNullException("GetValueFromRegistry(_disableCmdPromptValue)"), CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                DisableCommandPromptPage = false;
            }

            try
            {
                DisableFileTransferPage = Convert.ToBoolean(GetValueFromRegistry(_disableFileTransferValue) ?? throw new ArgumentNullException("GetValueFromRegistry(_disableFileTransferValue)"), CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                DisableFileTransferPage = false;
            }

            try
            {
                DisableUWPAppsPage = Convert.ToBoolean(GetValueFromRegistry(_disableUWPAppsValue) ?? throw new ArgumentNullException("GetValueFromRegistry(_disableUWPAppsValue)"), CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                DisableUWPAppsPage = false;
            }

            try
            {
                DisableManageTasklistsPage = Convert.ToBoolean(GetValueFromRegistry(_disableTaskManagerValue) ?? throw new ArgumentNullException("GetValueFromRegistry(_disableTaskManagerValue)"), CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                DisableManageTasklistsPage = false;
            }

            try
            {
                DisableWindowsDevicePortalPage = Convert.ToBoolean(GetValueFromRegistry(_disableWindowsDevicePortalValue) ?? throw new ArgumentNullException("GetValueFromRegistry(_disableWindowsDevicePortalValue)"), CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                DisableWindowsDevicePortalPage = false;
            }

            try
            {
                TaskManagerLogFolder = (string)GetValueFromRegistry(_logFolderValue) ?? throw new ArgumentNullException("GetValueFromRegistry(_logFolderValue)");
            }
            catch (Exception)
            {
                TaskManagerLogFolder = _defaultLogFolder;
            }

            try
            {
                RunInitialTaskListsOnFirstBoot = Convert.ToBoolean(GetValueFromRegistry(_runOnFirstBootValue) ?? throw new ArgumentNullException("GetValueFromRegistry(_runOnFirstBootValue)"), CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                RunInitialTaskListsOnFirstBoot = false;
            }

            try
            {
                IsContainerSupportEnabled = !Convert.ToBoolean(GetValueFromRegistry(_disableContainerValue) ?? throw new ArgumentNullException("GetValueFromRegistry(_disableContainerValue)"), CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                IsContainerSupportEnabled = true;
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || !NativeMethods.IsApiSetImplemented("api-ms-win-containers-cmclient-l1-4-0"))
            {
                // Missing required ApiSet for container support. Disable it.
                // Currently container support is restricted to Windows.
                ServiceLogger.LogInformation(Resources.ContainerSupportNotPresent);
                IsContainerSupportEnabled = false;
            }

            String loopbackAppsString;
            try
            {
                loopbackAppsString = (string)GetValueFromRegistry(_localLoopbackAppsValue) ?? throw new ArgumentNullException("GetValueFromRegistry(_localLoopbackAppsValue)");
            }
            catch (Exception)
            {
                loopbackAppsString = "";
            }

            LocalLoopbackApps = loopbackAppsString.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();

            return true;
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
