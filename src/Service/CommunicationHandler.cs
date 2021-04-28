// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.FactoryOrchestrator.Core;
using Microsoft.FactoryOrchestrator.Server;

namespace Microsoft.FactoryOrchestrator.Service
{
    // Generally, the flow is:
    // Find the FactoryOrchestratorService singleton
    // Pass call to it
    public class CommunicationHandler : IFactoryOrchestratorService
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
                    FOService.Instance.Stop(true);
                    FOService.Instance.Start(true, new CancellationToken());
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

                // Set new value in ServiceStatus file
                FOService.Instance.ServiceStatus.LogFolder = logFolder;

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

        public PlatformID GetOSPlatform()
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: GetOSPlatform");
                var platform = Environment.OSVersion.Platform;
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: GetOSPlatform");
                return platform;
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

        public TaskRun RunTask(TaskBase task, Guid? desiredTaskRunGuid = null)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: RunTask {task}");

                if (FOService.Instance.IsExecutingBootTasks)
                {
                    throw new FactoryOrchestratorException(Resources.BootTasksExecutingError);
                }

                var run = FOService.Instance.TaskExecutionManager.RunTask(task, desiredTaskRunGuid);
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

        public void TerminateApp(string aumid)
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: TerminateApp {aumid}");

                if (FOService.Instance.IsExecutingBootTasks)
                {
                    throw new FactoryOrchestratorException(Resources.BootTasksExecutingError);
                }

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    throw new FactoryOrchestratorException(string.Format(CultureInfo.CurrentCulture, Resources.WindowsOnlyError, "TerminateApp"));
                }

                var apps = WDPHelpers.GetInstalledAppPackagesAsync("localhost", GetWdpHttpPort()).Result;
                var app = apps.Packages.Where(x => x.AppId.Equals(aumid, StringComparison.OrdinalIgnoreCase)).DefaultIfEmpty(null).FirstOrDefault();

                if (app != null)
                {
                    WDPHelpers.CloseAppWithWDP(app.FullName, "localhost", GetWdpHttpPort()).Wait();
                }

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: TerminateApp {aumid}");
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

                WDPHelpers.InstallAppWithWDP(appPackagePath, dependentPackages, certificateFile, "localhost", GetWdpHttpPort()).Wait();

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
                var apps = WDPHelpers.GetInstalledAppPackagesAsync("localhost", GetWdpHttpPort()).Result;

                List<string> aumids = apps.Packages.Select(x => x.AppId).ToList();

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: GetInstalledApps");
                return aumids;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }

        public List<PackageInfo> GetInstalledAppsDetailed()
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: GetInstalledAppsDetailed");

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    throw new FactoryOrchestratorException(string.Format(CultureInfo.CurrentCulture, Resources.WindowsOnlyError, "GetInstalledAppsDetailed"));
                }

                // Get installed packages on the system
                var apps = WDPHelpers.GetInstalledAppPackagesAsync("localhost", GetWdpHttpPort()).Result;

                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: GetInstalledAppsDetailed");
                return apps.Packages;
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

        public int GetWdpHttpPort()
        {
            try
            {
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Start}: GetWdpHttpPort");
                int ret = GetWdpHttpPort();
                FOService.Instance.ServiceLogger.LogDebug($"{Resources.Finish}: GetWdpHttpPort");
                return ret;
            }
            catch (Exception e)
            {
                FOService.Instance.LogServiceEvent(new ServiceEvent(ServiceEventType.ServiceError, null, e.AllExceptionsToString()));
                throw;
            }
        }
    }
}
