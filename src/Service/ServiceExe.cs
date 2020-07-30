// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using PeterKottas.DotNetCore.WindowsService;
using JKang.IpcServiceFramework;
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
using System.Collections.Concurrent;
using PeterKottas.DotNetCore.WindowsService.Base;
using System.Security.Cryptography;

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
                        if (FOService.Instance != null)
                        {
                            _logger.LogError(e, string.Format(Resources.ServiceErrored, name));
                        }
                        else
                        {
                            FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, $"{string.Format(Resources.ServiceErrored, name)}: {e.AllExceptionsToString()}"));
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
                throw e;
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
                TaskList tl = FOService.Instance.TestExecutionManager.CreateTaskListFromDirectory(path, recursive);

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: CreateTaskListFromDirectory {path} {recursive}");
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
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: LoadTaskListsFromXmlFile {filename}");

                if (FOService.Instance.IsExecutingBootTasks)
                {
                    throw new FactoryOrchestratorException(Resources.BootTasksExecutingError);
                }

                filename = Environment.ExpandEnvironmentVariables(filename);
                List<Guid> taskLists = FOService.Instance.TestExecutionManager.LoadTaskListsFromXmlFile(filename);

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: LoadTaskListsFromXmlFile {filename}");
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
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: CreateTaskListFromTaskList {list.Guid}");

                if (FOService.Instance.IsExecutingBootTasks)
                {
                    throw new FactoryOrchestratorException(Resources.BootTasksExecutingError);
                }

                var serverList = FOService.Instance.TestExecutionManager.CreateTaskListFromTaskList(list);
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: CreateTaskListFromTaskList {list.Guid}");
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
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: SaveTaskListToXmlFile {guid} {filename}");

                if (FOService.Instance.IsExecutingBootTasks)
                {
                    throw new FactoryOrchestratorException(Resources.BootTasksExecutingError);
                }

                filename = Environment.ExpandEnvironmentVariables(filename);
                FOService.Instance.TestExecutionManager.SaveTaskListToXmlFile(guid, filename);

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: SaveTaskListToXmlFile {guid} {filename}");
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
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: SaveAllTaskListsToXmlFile {filename}");

                if (FOService.Instance.IsExecutingBootTasks)
                {
                    throw new FactoryOrchestratorException(Resources.BootTasksExecutingError);
                }

                filename = Environment.ExpandEnvironmentVariables(filename);
                if (!FOService.Instance.TestExecutionManager.SaveAllTaskListsToXmlFile(filename))
                {
                    throw new FactoryOrchestratorException(Resources.NoTaskListsException);
                }

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: SaveAllTaskListsToXmlFile {filename}");
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
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: GetTaskListGuids");
                var guids = FOService.Instance.TestExecutionManager.GetTaskListGuids();
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: GetTaskListGuids");
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
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: GetTaskListSummaries");
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

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: GetTaskListSummaries");
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
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: QueryTaskList {taskListGuid}");
                var list = FOService.Instance.TestExecutionManager.GetTaskList(taskListGuid);
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: QueryTaskList {taskListGuid}");
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
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: QueryTask {taskGuid}");
                var task = FOService.Instance.TestExecutionManager.GetTask(taskGuid);
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
                throw e;
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

                FOService.Instance.TestExecutionManager.DeleteTaskList(listToDelete);
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: DeleteTaskList {listToDelete}");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
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

                FOService.Instance.TestExecutionManager.ReorderTaskLists(newOrder);
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: ReorderTaskLists {newOrder}");
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
                if (FOService.Instance.IsExecutingBootTasks)
                {
                    throw new FactoryOrchestratorException(Resources.BootTasksExecutingError);
                }

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
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: UpdateTask {updatedTask.Name} {updatedTask.Guid}");

                if (FOService.Instance.IsExecutingBootTasks)
                {
                    throw new FactoryOrchestratorException(Resources.BootTasksExecutingError);
                }

                FOService.Instance.TestExecutionManager.UpdateTask(updatedTask);
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: UpdateTask {updatedTask.Name} {updatedTask.Guid}");
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
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: UpdateTaskList {taskList.Guid}");

                if (FOService.Instance.IsExecutingBootTasks)
                {
                    throw new FactoryOrchestratorException(Resources.BootTasksExecutingError);
                }

                FOService.Instance.TestExecutionManager.UpdateTaskList(taskList);
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: UpdateTaskList {taskList.Guid}");
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
                throw e;
            }
        }

        public string GetLogFolder()
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: GetLogFolder");
                var folder = FOService.Instance.TestExecutionManager.LogFolder;
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: GetLogFolder");
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
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: SetLogFolder {logFolder} {moveExistingLogs}");

                if (FOService.Instance.IsExecutingBootTasks)
                {
                    throw new FactoryOrchestratorException(Resources.BootTasksExecutingError);
                }

                logFolder = Environment.ExpandEnvironmentVariables(logFolder);
                FOService.Instance.TestExecutionManager.SetLogFolder(logFolder, moveExistingLogs);

                // Set new value in registry
                FOService.Instance.SetValueInRegistry(FOService.Instance._logFolderValue, logFolder, RegistryValueKind.String);

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: SetLogFolder {logFolder} {moveExistingLogs}");
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
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: AbortAllTaskLists");
                FOService.Instance.TestExecutionManager.AbortAll();
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: AbortAllTaskLists");
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
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: AbortTaskList {taskListGuid}");
                FOService.Instance.TestExecutionManager.AbortTaskList(taskListGuid);
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: AbortTaskList {taskListGuid}");
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
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: AbortTaskRun {taskRunGuid}");
                FOService.Instance.TestExecutionManager.AbortTaskRun(taskRunGuid);
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: AbortTaskRun {taskRunGuid}");
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
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: GetServiceVersionString");
                var version = FOService.GetServiceVersionString();
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: GetServiceVersionString");
                return version;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
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
                throw e;
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
                throw e;
            }
        }

        public TaskRun QueryTaskRun(Guid taskRunGuid)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: QueryTaskRun {taskRunGuid}");
                var run = FOService.Instance.TestExecutionManager.GetTaskRunByGuid(taskRunGuid).DeepCopy();
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: QueryTaskRun {taskRunGuid}");
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
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: UpdateTaskRun {taskRun.Guid}");

                if (FOService.Instance.IsExecutingBootTasks)
                {
                    throw new FactoryOrchestratorException(Resources.BootTasksExecutingError);
                }

                FOService.Instance.TestExecutionManager.UpdateTaskRun(taskRun);
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: UpdateTaskRun {taskRun.Guid}");
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
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: RunAllTaskLists");

                if (FOService.Instance.IsExecutingBootTasks)
                {
                    throw new FactoryOrchestratorException(Resources.BootTasksExecutingError);
                }

                var ran = FOService.Instance.TestExecutionManager.RunAllTaskLists();
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: RunAllTaskLists");
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
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: RunTaskList {taskListToRun} {initialTaskIndex}");

                if (FOService.Instance.IsExecutingBootTasks)
                {
                    throw new FactoryOrchestratorException(Resources.BootTasksExecutingError);
                }

                FOService.Instance.TestExecutionManager.RunTaskListFromInitial(taskListToRun, initialTaskIndex);
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: RunTaskList {taskListToRun} {initialTaskIndex}");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
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

                var run = FOService.Instance.TestExecutionManager.RunExecutableAsBackgroundTask(exeFilePath, arguments, logFilePath, runInContainer);
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: RunExecutable {exeFilePath} {arguments}");
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
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: RunTask {taskGuid}");

                if (FOService.Instance.IsExecutingBootTasks)
                {
                    throw new FactoryOrchestratorException(Resources.BootTasksExecutingError);
                }

                var run = FOService.Instance.TestExecutionManager.RunTask(taskGuid);
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: RunTask {taskGuid}");
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
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: RunTask {task}");

                if (FOService.Instance.IsExecutingBootTasks)
                {
                    throw new FactoryOrchestratorException(Resources.BootTasksExecutingError);
                }

                var run = FOService.Instance.TestExecutionManager.RunTask(task);
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: RunTask {task}");
                return run;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
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
                    throw new FactoryOrchestratorException(string.Format(Resources.WindowsOnlyError, "RunApp"));
                }

                var run = FOService.Instance.TestExecutionManager.RunApp(aumid);
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: RunApp {aumid}");
                return run;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
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
                throw e;
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
                    throw new FactoryOrchestratorException($"{string.Format(Resources.FileSaveError, targetFilename)} {e.AllExceptionsToString()} {e.HResult}", null, e);
                }

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: SendFile {targetFilename} {appending} {sendToContainer}");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
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
                        throw new FileNotFoundException(string.Format(Resources.InvalidPathError, path));
                    }
                }

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: DeleteFileOrFolder {path} {deleteInContainer}");
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
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
                        throw new FileNotFoundException(string.Format(Resources.InvalidPathError, sourcePath));
                    }
                }

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: MoveFileOrFolder {sourcePath} {destinationPath} {moveInContainer}");
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
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: EnumerateDirectories {path}");
                path = Environment.ExpandEnvironmentVariables(path);
                var dirs = Directory.EnumerateDirectories(path, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).ToList();
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: EnumerateDirectories {path}");
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
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: EnumerateFiles {path} {recursive}");
                path = Environment.ExpandEnvironmentVariables(path);
                var files = Directory.EnumerateFiles(path, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).ToList();
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: EnumerateFiles {path} {recursive}");
                return files;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw e;
            }
        }

        public void InstallApp(string appPackagePath, List<string> dependentPackages = null, string certificateFile = null)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: InstallApp {appPackagePath}");

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    throw new FactoryOrchestratorException(string.Format(Resources.WindowsOnlyError, "InstallApp"));
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
                throw e;
            }
        }

        public List<string> GetInstalledApps()
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: GetInstalledApps");

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    throw new FactoryOrchestratorException(string.Format(Resources.WindowsOnlyError, "GetInstalledApps"));
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
                throw e;
            }
        }

        public void EnableLocalLoopbackForApp(string aumid)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: EnableLocalLoopbackForApp {aumid}");
                var index = aumid.IndexOf('!');

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
                throw e;
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
                throw e;
            }
        }

        public List<string> GetContainerIpAddresses()
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: GetContainerIpAddresses");
                var ips = FOService.Instance.GetContainerIpAddresses();
                var ipStrings = new List<string>();
                foreach (var ip in ips)
                {
                    ipStrings.Add(ip.ToString());
                }

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: GetContainerIpAddresses");
                return ipStrings;
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
                throw e;
            }
        }

        public bool IsContainerRunning()
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: IsContainerRunning");
                bool ret = false;
                if (!String.IsNullOrWhiteSpace(FOService.Instance.GetContainerId()))
                {
                    ret = true;
                }
                else
                {
                    ret = false;
                }

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: IsContainerRunning");
                return ret;
            }
            catch (FactoryOrchestratorContainerException)
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: IsContainerRunning");
                return false;
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
        private static readonly object _openedFilesLock = new object();

        private TaskManager_Server _taskExecutionManager;
        private IMicroServiceController _controller;
        public ILogger<FOService> ServiceLogger;
        private System.Threading.CancellationTokenSource _ipcCancellationToken;
        private Dictionary<string, (Stream stream, System.Threading.Timer timer)> _openedFiles;

        private readonly string _nonMutableServiceRegKey = @"SYSTEM\CurrentControlSet\Control\FactoryOrchestrator";
        private readonly string _mutableServiceRegKey = @"OSDATA\CurrentControlSet\Control\FactoryOrchestrator";
        private readonly string _volatileServiceRegKey = @"SYSTEM\CurrentControlSet\Control\FactoryOrchestrator\EveryBootTaskStatus";

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

        public Dictionary<ulong, ServiceEvent> ServiceEvents { get; }
        public ulong LastEventIndex { get; private set; }
        public DateTime LastEventTime { get; private set; }
        public bool DisableCommandPromptPage { get; private set; }
        public bool DisableWindowsDevicePortalPage { get; private set; }
        public bool DisableUWPAppsPage { get; private set; }
        public bool DisableManageTasklistsPage { get; private set; }
        public bool DisableFileTransferPage { get; private set; }
        public bool DisableNetworkAccess { get; private set; }
        public bool EnableNetworkAccess { get; private set; }
        public int ServiceNetworkPort { get; private set; }

        public string TaskManagerLogFolder { get; private set; }


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
            using (var reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", false))
            {
                var version = (string)reg.GetValue("BuildLabEx");
                return version;
            }
        }

        /// <summary>
        /// Returns OEM version string, set as WCOS OEM Customization.
        /// </summary>
        /// <returns></returns>
        public static string GetOEMVersionString()
        {
            using (var reg = Registry.LocalMachine.OpenSubKey(@"OSDATA\CurrentControlSet\Control\FactoryOrchestrator", false))
            {
                var version = (string)reg.GetValue("OEMVersion");
                return version;
            }
        }

        public FOService(IMicroServiceController controller, ILogger<FOService> logger)
        {
            lock (_constructorLock)
            {
                if (_singleton != null)
                {
                    throw new FactoryOrchestratorException(Resources.ServiceAlreadyCreatedError);
                }

                _controller = controller;
                ServiceLogger = logger;
                _singleton = this;
                ServiceEvents = new Dictionary<ulong, ServiceEvent>();
                LastEventIndex = 0;
                LastEventTime = DateTime.MinValue;
                DisableCommandPromptPage = false;
                DisableWindowsDevicePortalPage = false;
                DisableUWPAppsPage = false;
                DisableManageTasklistsPage = false;
                DisableFileTransferPage = false;
                DisableNetworkAccess = false;
				EnableNetworkAccess = false;
                LocalLoopbackApps = new List<string>();
                TaskManagerLogFolder = _defaultLogFolder;
                IsExecutingBootTasks = true;
                _containerClient = null;
                ServiceNetworkPort = 45684;
                _openedFiles = new Dictionary<string, (Stream stream, System.Threading.Timer timer)>();
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
            bool networkAccessEnabled = false;
            // Execute "first run" tasks. They do nothing if already run, but might need to run every boot on a state separated WCOS image.
            ExecuteServerBootTasks();

            // Start IPC server on port 45684. Only start after all boot tasks are complete.
            if (EnableNetworkAccess && !DisableNetworkAccess) 
            {
                FOServiceExe.ipcHost = new IpcServiceHostBuilder(FOServiceExe.ipcSvcProvider).AddTcpEndpoint<IFactoryOrchestratorService>("tcp", IPAddress.Any, 45684)
                                                                .Build();
                networkAccessEnabled = true;
            }
            else
            {
                FOServiceExe.ipcHost = new IpcServiceHostBuilder(FOServiceExe.ipcSvcProvider).AddTcpEndpoint<IFactoryOrchestratorService>("tcp", IPAddress.Loopback, 45684)
                                                                .Build();
                networkAccessEnabled = false;
            }

            _ipcCancellationToken = new System.Threading.CancellationTokenSource();
            _taskExecutionManager.OnTaskManagerEvent += HandleTaskManagerEvent;
            FOServiceExe.ipcHost.RunAsync(_ipcCancellationToken.Token);

            if (networkAccessEnabled)
            {
                ServiceLogger.LogInformation($"{Resources.NetworkAccessEnabled}\n");
            }
            else 
            {
                ServiceLogger.LogInformation($"{Resources.NetworkAccessDisabled}\n");
            }

            ServiceLogger.LogInformation($"{Resources.ReadyToCommunicate}\n");
            LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceStart, null, Resources.BootTasksStarted));

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
                    ServiceLogger.LogWarning($"{string.Format(Resources.FOXMLFileLoadException, _taskExecutionManager.TaskListStateFile)}\n {e.AllExceptionsToString()}");
                }
            }

            IsExecutingBootTasks = false;
            LogServiceEvent(new ServiceEvent(ServiceEventType.BootTasksComplete, null, Resources.BootTasksFinished));
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

                    ServiceLogger.LogInformation(string.Format(Resources.CheckingForFile, _initialTasksDefaultPath));
                    // Find the TaskLists XML path. Check testcontent directory for wellknown name, fallback to registry
                    if (!File.Exists(firstBootStateTaskListPath))
                    {
                        firstBootStateTaskListPath = GetValueFromRegistry(_initialTasksPathValue, _initialTasksDefaultPath) as string;
                        if (!firstBootStateTaskListPath.Equals(_initialTasksDefaultPath, StringComparison.OrdinalIgnoreCase))
                        {
                            ServiceLogger.LogInformation(string.Format(Resources.CheckingForFile, firstBootStateTaskListPath));
                        }
                    }

                    if (File.Exists(firstBootStateTaskListPath))
                    {
                        ServiceLogger.LogInformation(string.Format(Resources.AttemptingFileLoad, firstBootStateTaskListPath));

                        // Load the TaskLists file specified in registry
                        var firstBootTaskListGuids = _taskExecutionManager.LoadTaskListsFromXmlFile(firstBootStateTaskListPath);

                        ServiceLogger.LogInformation(string.Format(Resources.FileLoadSucceeded, firstBootStateTaskListPath));
                        loaded = true;
                        SetValueInRegistry(_firstBootStateLoadedValue, 1, RegistryValueKind.DWord);
                    }
                    else
                    {
                        ServiceLogger.LogInformation(string.Format(Resources.FileNotFound, firstBootStateTaskListPath));
                    }
                }
            }
            catch (Exception e)
            {
                LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, $"{string.Format(Resources.FOXMLFileLoadException, _initialTasksDefaultPath)} ({e.AllExceptionsToString()})"));
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
                ServiceLogger.LogError(e, Resources.ServiceStopSaveError);
            }

            // Close registry
            _volatileKey.Close();
            _nonMutableKey.Close();
            if (_mutableKey != null)
            {
                _mutableKey.Close();
            }

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
                            serviceEvent = new ServiceEvent(ServiceEventType.WaitingForContainerTaskRun, e.Guid, string.Format(Resources.WaitingForContainerTaskRun, e.Guid));
                            RunTaskRunInContainer(run);
                        }
                        else
                        {
                            serviceEvent = new ServiceEvent(ServiceEventType.WaitingForExternalTaskRun, e.Guid, string.Format(Resources.WaitingForExternalTaskRun, e.Guid));
                        }
                        break;
                    }
                case TaskManagerEventType.WaitingForExternalTaskRunFinished:
                    serviceEvent = new ServiceEvent(ServiceEventType.DoneWaitingForExternalTaskRun, e.Guid, string.Format(Resources.DoneWaitingForExternalTaskRun, e.Guid, e.Status));
                    break;
                default:
                    break;
            }

            if (serviceEvent != null)
            {
                LogServiceEvent(serviceEvent);
            }
        }

        private void RunTaskRunInContainer(TaskRun_Server hostRun)
        {
            hostRun.TaskOutput.Add(Resources.AttemptingContainerTaskRun);

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

                    if (containerTask.Type == TaskType.UWP)
                    {
                        // The container doesn't have WDP, translate to a shell execute call
                        hostRun.TaskOutput.Add(Resources.RedirectingUWPToRunAs);
                        var temp = containerTask as UWPTask;
                        var uwpContainerTask = new ExecutableTask(@"%windir%\system32\RunAsExplorerUser.exe");
                        uwpContainerTask.Arguments = @"explorer.exe shell:appsFolder\" + temp.Path;
                        containerTask = uwpContainerTask;
                    }

                    await ConnectToContainer();

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
                            _taskExecutionManager.UpdateTaskRun(hostRun);
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
                    _containerClient = null;
                }
            });
        }

        public async Task ConnectToContainer()
        {
            if (_containerClient == null || !_containerClient.IsConnected)
            {
                foreach (var ip in GetContainerIpAddresses())
                {
                    _containerClient = new FactoryOrchestratorClient(ip, ServiceNetworkPort);
                    if (await _containerClient.TryConnect())
                    {
                        return;
                    }
                    else
                    {
                        _containerClient = null;
                    }
                }
            }
            else
            {
                return;
            }

            throw new FactoryOrchestratorContainerException(Resources.NoContainerIpFound);
        }

        public string GetContainerId()
        {
            try
            {
                // TODO: replace with an API call
                var cmdiag = RunProcessViaCmd("cmdiag.exe", "list", 5000);
                var containerGuidString = cmdiag.TaskOutput.Where(x => x.ToLowerInvariant().Contains("cmscontainerstaterunning".ToLowerInvariant())).DefaultIfEmpty(null).FirstOrDefault();

                if (containerGuidString != null)
                {
                    return containerGuidString.Split(',', StringSplitOptions.RemoveEmptyEntries).First().Trim();
                }
            }
            catch (Exception e)
            {
                throw new FactoryOrchestratorContainerException(Resources.NoContainerIdFound, null, e);
            }

            throw new FactoryOrchestratorContainerException(Resources.NoContainerIdFound);
        }

        public List<IPAddress> GetContainerIpAddresses()
        {
            List<IPAddress> ips = new List<IPAddress>();
            try
            {
                var containerGuid = GetContainerId();
                var cmdiag = RunProcessViaCmd("cmdiag.exe", $"exec {containerGuid} -runas administrator -command \"ipconfig.exe\"", 5000);
                var ipStrings = cmdiag.TaskOutput.Where(x => x.ToLowerInvariant().Contains("IPv4 address".ToLowerInvariant()));

                foreach (var ipString in ipStrings)
                {
                    if (ipString != null)
                    {
                        ips.Add(IPAddress.Parse(ipString.Split(':', StringSplitOptions.RemoveEmptyEntries).Last().Trim()));
                    }
                }

                return ips;
            }
            catch (Exception e)
            {
                throw new FactoryOrchestratorContainerException(Resources.NoContainerIpFound, null, e);
            }

            throw new FactoryOrchestratorContainerException(Resources.NoContainerIpFound);
        }


        public void SendFile(string targetFilename, byte[] fileData, bool appending, bool sendToContainer)
        {
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
	            if (fileData.Length != Constants.FILE_TRANSFER_CHUNK_SIZE)
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
            await ConnectToContainer();
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
            byte[] bytes = new byte[0];

            if (!getFromContainer)
            {
                sourceFilename = Environment.ExpandEnvironmentVariables(sourceFilename);
                if (!File.Exists(sourceFilename))
                {
                    throw new FileNotFoundException(string.Format(Resources.FileNotFoundException, sourceFilename), sourceFilename);
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
            await ConnectToContainer();
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
            await ConnectToContainer();
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
            await ConnectToContainer();
            try
            {
                await _containerClient.DeleteFileOrFolder(path);
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
                Stop();
            }

            try
            {
                _volatileKey = OpenOrCreateRegKey(RegKeyType.Volatile);
            }
            catch (Exception e)
            {
                ServiceLogger.LogError($"! {e.Message}");
                Stop();
            }

            LoadOEMCustomizations();

            // Now that we know the log folder, we can create the TaskManager instance
            try
            {
                Directory.CreateDirectory(FOServiceExe.ServiceLogFolder);
            }
            catch (Exception e)
            {
                ServiceLogger.LogError($"{string.Format(Resources.CreateDirectoryFailed, FOServiceExe.ServiceLogFolder)} {e.Message}");
                Stop();
            }
            try
            {
                Directory.CreateDirectory(TaskManagerLogFolder);
            }
            catch (Exception e)
            {
                ServiceLogger.LogError($"{string.Format(Resources.CreateDirectoryFailed, TaskManagerLogFolder)} {e.Message}");
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
            bool firstBootTasksFailed = false;
            bool everyBootTasksFailed = false;
            bool firstBootTasksExecuted = false;
            bool everyBootTasksExecuted = false;
            bool stateFileBackedup = false;
            var logFolder = _taskExecutionManager.LogFolder;
            var stateFileBackupPath = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), "FactoryOrchestratorTempTaskListStateFile");

            // First Boot tasks
            try
            {
                // Backup State File
                if (File.Exists(_taskExecutionManager.TaskListStateFile))
                {
                    File.Copy(_taskExecutionManager.TaskListStateFile, stateFileBackupPath, true);
                }
                stateFileBackedup = true;

                // Check if first boot tasks were already completed
                var firstBootTasksCompleted = GetValueFromRegistry(_firstBootCompleteValue) as int?;

                if ((firstBootTasksCompleted == null) || (firstBootTasksCompleted == 0) || (force == true))
                {
                    ServiceLogger.LogInformation(string.Format(Resources.CheckingForFile, _firstBootTasksDefaultPath));
                    string firstBootTaskListPath = _firstBootTasksDefaultPath;

                    // Find the TaskLists XML path. Check testcontent directory for wellknown name, fallback to registry
                    if (!File.Exists(firstBootTaskListPath))
                    {
                        firstBootTaskListPath = (string)GetValueFromRegistry(_firstBootTasksPathValue, _firstBootTasksDefaultPath);
                        if (!firstBootTaskListPath.Equals(_firstBootTasksDefaultPath, StringComparison.OrdinalIgnoreCase))
                        {
                            ServiceLogger.LogInformation(string.Format(Resources.CheckingForFile, firstBootTaskListPath));
                        }
                    }

                    if (File.Exists(firstBootTaskListPath))
                    {
                        firstBootTasksExecuted = true;
                        ServiceLogger.LogInformation(string.Format(Resources.AttemptingFileLoad, firstBootTaskListPath));
                        // Create a new directory for the first boot logs
                        _taskExecutionManager.SetLogFolder(Path.Combine(logFolder, "FirstBootTaskLists"), false);

                        // Load the TaskLists file specified in registry
                        var firstBootTaskListGuids = _taskExecutionManager.LoadTaskListsFromXmlFile(firstBootTaskListPath);

                        foreach (var listGuid in firstBootTaskListGuids)
                        {
                            _taskExecutionManager.RunTaskList(listGuid);
                            ServiceLogger.LogInformation(string.Format(Resources.FirstBootRunningTaskList, listGuid));
                        }
                    }
                    else
                    {
                        ServiceLogger.LogInformation(string.Format(Resources.FileNotFound, firstBootTaskListPath));
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
            while (_taskExecutionManager.IsTaskListRunning)
            {
                System.Threading.Thread.Sleep(1000);
                sleepCount++;
                if (sleepCount % 15 == 0)
                {
                    ServiceLogger.LogInformation(Resources.FirstBootWaiting);
                }
            }

            // Every boot tasks
            if ((stateFileBackedup) && (_nonMutableKey != null) && (_volatileKey != null))
            {
                try
                {
                    // Check if every boot tasks were already completed
                    var everyBootTasksCompleted = _volatileKey.GetValue(_everyBootCompleteValue) as int?;

                    if ((everyBootTasksCompleted == null) || (everyBootTasksCompleted == 0) || (force == true))
                    {
                        ServiceLogger.LogInformation(string.Format(Resources.CheckingForFile, _everyBootTasksDefaultPath));
                        string everyBootTaskListPath = _everyBootTasksDefaultPath;

                        // Find the TaskLists XML path. Check testcontent directory for wellknown name, fallback to registry
                        if (!File.Exists(everyBootTaskListPath))
                        {
                            everyBootTaskListPath = (string)GetValueFromRegistry(_everyBootTasksPathValue, _everyBootTasksDefaultPath);
                            if (!everyBootTaskListPath.Equals(_everyBootTasksDefaultPath, StringComparison.OrdinalIgnoreCase))
                            {
                                ServiceLogger.LogInformation(string.Format(Resources.CheckingForFile, everyBootTaskListPath));
                            }
                        }

                        if (File.Exists(everyBootTaskListPath))
                        {
                            everyBootTasksExecuted = true;
                            ServiceLogger.LogInformation(string.Format(Resources.AttemptingFileLoad, everyBootTaskListPath));

                            // Create a new directory for the first boot logs
                            _taskExecutionManager.SetLogFolder(Path.Combine(logFolder, "EveryBootTaskLists"), false);

                            // Load the TaskLists file specified in registry
                            var everyBootTaskListGuids = _taskExecutionManager.LoadTaskListsFromXmlFile(everyBootTaskListPath);

                            foreach (var listGuid in everyBootTaskListGuids)
                            {
                                _taskExecutionManager.RunTaskList(listGuid);
                                ServiceLogger.LogInformation(string.Format(Resources.EveryBootRunningTaskList, listGuid));
                            }
                        }
                        else
                        {
                            ServiceLogger.LogInformation(string.Format(Resources.FileNotFound, everyBootTaskListPath));
                        }
                    }
                    else
                    {
                        ServiceLogger.LogInformation(Resources.EveryBootAlreadyComplete);
                    }
                }
                catch (Exception e)
                {
                    LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, $"{Resources.EveryBootFailed} ({e.AllExceptionsToString()})"));
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
                    ServiceLogger.LogInformation(Resources.EveryBootWaiting);
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
            if (!everyBootTasksFailed)
            {
                // Mark every boot tasks as complete. Mark in volatile registry location so it is reset after reboot.
                if (everyBootTasksExecuted)
                {
                    ServiceLogger.LogInformation(Resources.EveryBootComplete);
                }

                _volatileKey.SetValue(_everyBootCompleteValue, 1, RegistryValueKind.DWord);
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
                ServiceLogger.LogInformation(string.Format(Resources.EnablingLoopback, _foAppPfn));

                try
                {
                    EnableLocalLoopbackForApp(_foAppPfn, false);
                }
                catch (Exception e)
                {
                    success = false;
                    LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, $"{string.Format(Resources.EnablingLoopbackFailed, _foAppPfn)} ({e.AllExceptionsToString()})"));
                }

                ServiceLogger.LogInformation(string.Format(Resources.EnablingLoopback, _foDevAppPfn));

                try
                {
                    EnableLocalLoopbackForApp(_foDevAppPfn, false);
                }
                catch (Exception e)
                {
                    success = false;
                    LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, $"{string.Format(Resources.EnablingLoopbackFailed, _foDevAppPfn)} ({e.AllExceptionsToString()})"));
                }

                // Enable all other allowed apps
                foreach (var app in LocalLoopbackApps)
                {
                    ServiceLogger.LogInformation(string.Format(Resources.EnablingLoopback, app));

                    try
                    {
                        EnableLocalLoopbackForApp(app, false);
                    }
                    catch (Exception e)
                    {
                        success = false;
                        LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, $"{string.Format(Resources.EnablingLoopbackFailed, app)} ({e.AllExceptionsToString()})"));
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
            // If a value is set improperly, it will fallback to defaults set in the CTOR.
            try
            {
                DisableNetworkAccess = Convert.ToBoolean(GetValueFromRegistry(_disableNetworkAccessValue, true));
            }
            catch (Exception)
            { }
            try
            {
                EnableNetworkAccess = Convert.ToBoolean(GetValueFromRegistry(_enableNetworkAccessValue, false));
            }
            catch (Exception)
            { }

            try
            {
                DisableCommandPromptPage = Convert.ToBoolean(GetValueFromRegistry(_disableCmdPromptValue, false));
            }
            catch (Exception)
            { }

            try
            {
                DisableFileTransferPage = Convert.ToBoolean(GetValueFromRegistry(_disableFileTransferValue, false));
            }
            catch (Exception)
            { }

            try
            {
                DisableUWPAppsPage = Convert.ToBoolean(GetValueFromRegistry(_disableUWPAppsValue, false));
            }
            catch (Exception)
            { }

            try
            {
                DisableManageTasklistsPage = Convert.ToBoolean(GetValueFromRegistry(_disableTaskManagerValue, false));
            }
            catch (Exception)
            { }

            try
            {
                DisableWindowsDevicePortalPage = Convert.ToBoolean(GetValueFromRegistry(_disableWindowsDevicePortalValue, false));
            }
            catch (Exception)
            { }

            try
            {
                TaskManagerLogFolder = (string)GetValueFromRegistry(_logFolderValue, _defaultLogFolder);
            }
            catch (Exception)
            { }

            var loopbackAppsString = "";
            try
            {
                loopbackAppsString = (string)GetValueFromRegistry(_localLoopbackAppsValue, "");
            }
            catch (Exception)
            { }

            LocalLoopbackApps = loopbackAppsString.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();

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
            if ((runner != null) && (!runner.WaitForExit(timeoutMS)))
            {
                TestExecutionManager.AbortTaskRun(run.Guid);
                throw new FactoryOrchestratorException($"{process} did not exit before {timeoutMS}ms!");
            }

            if (run.TaskStatus != TaskStatus.Passed)
            {
                throw new FactoryOrchestratorException($"{process} exited with {run.ExitCode}");
            }

            return run;
        }
    }
}