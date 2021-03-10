// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.FactoryOrchestrator.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaskStatus = Microsoft.FactoryOrchestrator.Core.TaskStatus;
using static Microsoft.FactoryOrchestrator.Server.HelperMethods;
using System.Runtime.InteropServices;
using System.Net;
using System.Text.RegularExpressions;
using PEUtility;
using System.Globalization;
using System.Xml;

namespace Microsoft.FactoryOrchestrator.Server
{

    internal static class NativeMethods
    {
        [DllImport("KernelBase.dll", SetLastError = false, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments
        public static extern bool IsApiSetImplemented([MarshalAs(UnmanagedType.LPStr)] string Contract);
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
    }

    public class TaskManager : IDisposable
    {
        public TaskManager(string logFolder, string taskListStateFile)
        {
            _startNonParallelTaskRunLock = new SemaphoreSlim(1, 1);
            KnownTaskLists = new List<TaskList>();
            RunningTaskListTokens = new ConcurrentDictionary<Guid, CancellationTokenSource>();
            RunningBackgroundTasks = new ConcurrentDictionary<Guid, List<TaskRunner>>();
            RunningTaskRunTokens = new ConcurrentDictionary<Guid, CancellationTokenSource>();
            _taskRunMap = new ConcurrentDictionary<Guid, ServerTaskRun>();
            _taskMapLock = new object();
            TaskListStateFile = taskListStateFile;

            if (_isWindows)
            {
                _supportsWin32Gui = NativeMethods.IsApiSetImplemented("ext-ms-win-ntuser-window-l1-1-4");
            }
            else
            {
                _supportsWin32Gui = false;
            }

            SetLogFolder(logFolder, false);
        }

        public void SetLogFolder(string newLogFolder, bool moveFiles)
        {
            if (newLogFolder == LogFolder)
            {
                return;
            }

            try
            {
                // Don't allow the folder to move if any task is running or we are actively modifying tasklists
                lock (RunningTaskListLock)
                {
                    if (IsTaskListRunning)
                    {
                        throw new FactoryOrchestratorTaskListRunningException();
                    }

                    lock (KnownTaskListLock)
                    {
                        if (moveFiles && (LogFolder != null) && (Directory.Exists(LogFolder)))
                        {
                            // Move existing folder to temp folder
                            var tempDir = Path.Combine(Path.GetTempPath(), "FOTemp");
                            CopyDirectory(LogFolder, tempDir, true);

                            // Delete old folder
                            Directory.Delete(LogFolder, true);

                            // Move temp folder to new folder
                            CopyDirectory(tempDir, newLogFolder, true);

                            // Delete temp folder
                            Directory.Delete(tempDir, true);
                        }
                        else
                        {
                            Directory.CreateDirectory(newLogFolder);
                        }

                        // Update paths
                        LogFolder = newLogFolder;
                    }
                }
            }
            catch (Exception e)
            {
                throw new FactoryOrchestratorException(Resources.LogFolderMoveFailed, null, e);
            }
        }

        private static void CopyDirectory(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    Resources.SourceDirectoryNotFound
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    CopyDirectory(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        public TaskList CreateTaskListFromDirectory(String path, bool recursive)
        {
            // Search for all "executable" files
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var exes = Directory.EnumerateFiles(path, "*.exe", searchOption);
            var dlls = Directory.EnumerateFiles(path, "*.dll", searchOption);
            var commands = Directory.EnumerateFiles(path, "*.bat", searchOption).Concat(Directory.EnumerateFiles(path, "*.cmd", searchOption)).Concat(Directory.EnumerateFiles(path, "*.sh", searchOption));
            var ps1s = Directory.EnumerateFiles(path, "*.ps1", searchOption);
            TaskList tests = new TaskList(path, Guid.NewGuid());

            Parallel.ForEach<string>(dlls, (dll) =>
            {
                try
                {
                    var maybeTAEF = CheckForTAEFTest(dll);
                    if (maybeTAEF != null)
                    {
                        tests.Tasks.Add(maybeTAEF);
                    }
                }
                catch (Exception)
                {
                    // TODO: Logging
                }
            });

            // Assume every file found is a valid task
            foreach (var exe in exes)
            {
                var task = new ExecutableTask(exe);
                tests.Tasks.Add(task);
            }

            foreach (var command in commands)
            {
                var task = new CommandLineTask(command);
                tests.Tasks.Add(task);
            }

            foreach (var ps1 in ps1s)
            {
                var task = new PowerShellTask(ps1);
                tests.Tasks.Add(task);
            }

            lock (KnownTaskListLock)
            {
                KnownTaskLists.Add(tests);
                OnTaskManagerEvent?.Invoke(this, new TaskManagerEventArgs(TaskManagerEventType.NewTaskList, tests.Guid, null));
            }

            // Update XML for state tracking (this locks KnownTaskListLock)
            SaveAllTaskListsToXmlFile(TaskListStateFile);

            return tests;
        }

        public bool SaveAllTaskListsToXmlFile(string filename)
        {
            FactoryOrchestratorXML xml = new FactoryOrchestratorXML();

            lock (KnownTaskListLock)
            {
                if (KnownTaskLists.Count > 0)
                {
                    xml.TaskLists = KnownTaskLists;
                }
                else
                {
                    return false;
                }

                if (!xml.Save(filename))
                {
                    throw new FactoryOrchestratorException(Resources.TaskListSaveFailed);
                }
            }

            return true;
        }

        public void SaveTaskListToXmlFile(Guid guid, string filename)
        {
            lock (RunningTaskListLock)
            {
                if (IsTaskListRunning)
                {
                    throw new FactoryOrchestratorTaskListRunningException();
                }

                FactoryOrchestratorXML xml = new FactoryOrchestratorXML();

                var index = KnownTaskLists.FindIndex(x => x.Guid == guid);
                if (index > -1)
                {
                    xml.TaskLists.Add(KnownTaskLists[index]);
                }
                else
                {
                    throw new FactoryOrchestratorUnkownGuidException(guid, typeof(TaskList));
                }

                if (!xml.Save(filename))
                {
                    throw new FactoryOrchestratorException(Resources.TaskListSaveFailed);
                }
            }
        }

        public List<Guid> LoadTaskListsFromXmlFile(string filename)
        {
            lock (RunningTaskListLock)
            {
                if (IsTaskListRunning)
                {
                    throw new FactoryOrchestratorTaskListRunningException();
                }

                FactoryOrchestratorXML xml;
                xml = FactoryOrchestratorXML.Load(filename);

                // Add GUIDs to any TaskBase or TaskList objects that don't have one
                lock (KnownTaskListLock)
                {
                    // Check for duplicate GUIDs
                    var currentGuids = KnownTaskLists.Select(x => x.Guid).Concat(KnownTaskLists.SelectMany(y => y.Tasks).Select(z => z.Guid));
                    var newGuids = xml.TaskLists.Select(x => x.Guid).Concat(xml.TaskLists.SelectMany(y => y.Tasks).Select(z => z.Guid));
                    var intersect = currentGuids.Intersect(newGuids);
                    var dupGuidString = string.Empty;

                    foreach (var guid in intersect)
                    {
                        dupGuidString += $"{guid.ToString()}, ";
                    }

                    if (!string.IsNullOrEmpty(dupGuidString))
                    {
                        throw new FactoryOrchestratorXmlException(string.Format(CultureInfo.CurrentCulture, Resources.DuplicateGuidInXml, dupGuidString));
                    }

                    foreach (var list in xml.TaskLists)
                    {
                        // Mark "running" Tasks as Unknown, as their state is unknown
                        var badStateTasks = list.Tasks.Where(x => (x.IsRunningOrPending));
                        foreach (var task in badStateTasks)
                        {
                            task.LatestTaskRunStatus = TaskStatus.Unknown;
                        }
                        badStateTasks = list.BackgroundTasks.Where(x => (x.IsRunningOrPending));
                        foreach (var task in badStateTasks)
                        {
                            task.LatestTaskRunStatus = TaskStatus.Unknown;
                        }

                        // Add the list. We already checked it isn't a duplicate GUID.
                        KnownTaskLists.Add(list);
                        OnTaskManagerEvent?.Invoke(this, new TaskManagerEventArgs(TaskManagerEventType.NewTaskList, list.Guid, null));
                    }
                }

                // Update XML for state tracking
                if (filename != TaskListStateFile)
                {
                    SaveAllTaskListsToXmlFile(TaskListStateFile);
                }

                return xml.TaskLists.Select(x => x.Guid).ToList();
            }
        }

        public void DeleteTaskList(Guid listToDelete)
        {
            bool removed = false;
            lock (KnownTaskListLock)
            {
                // Delete() gracefully returns if the guid is invalid
                AbortTaskList(listToDelete);
                var index = KnownTaskLists.FindIndex(x => x.Guid == listToDelete);
                if (index > -1)
                {
                    KnownTaskLists.RemoveAt(index);
                    removed = true;
                }
            }

            if (removed)
            {
                OnTaskManagerEvent?.Invoke(this, new TaskManagerEventArgs(TaskManagerEventType.DeletedTaskList, listToDelete, null));

                // Update XML for state tracking
                SaveAllTaskListsToXmlFile(TaskListStateFile);
            }
        }

        public TaskBase GetTask(Guid taskGuid)
        {
            foreach (var list in KnownTaskLists)
            {
                var index = list.Tasks.FindIndex(y => y.Guid.Equals(taskGuid));
                if (index != -1)
                {
                    return list.Tasks[index];
                }
            }

            return null;
        }

        public TaskList GetTaskList(Guid guid)
        {
            return KnownTaskLists.Find(x => x.Guid == guid);
        }

        public List<Guid> GetTaskListGuids()
        {
            return KnownTaskLists.Select(x => x.Guid).ToList();
        }

        public void ReorderTaskLists(List<Guid> newOrder)
        {
            if (newOrder == null)
            {
                throw new ArgumentNullException(nameof(newOrder));
            }

            lock (KnownTaskListLock)
            {
                if (KnownTaskLists.Count != newOrder.Count)
                {
                    throw new FactoryOrchestratorException(string.Format(CultureInfo.CurrentCulture, Resources.TaskListCountMismatch, KnownTaskLists.Count, newOrder.Count));
                }

                lock (RunningTaskListLock)
                {
                    if (IsTaskListRunning)
                    {
                        throw new FactoryOrchestratorTaskListRunningException();
                    }

                    // Find TaskList and place in new order
                    var newKnownTaskLists = new List<TaskList>(KnownTaskLists.Count);
                    foreach (var guid in newOrder)
                    {
                        var list = GetTaskList(guid);
                        newKnownTaskLists.Add(list);
                    }

                    // Replace old list
                    KnownTaskLists = newKnownTaskLists;
                }
            }
        }

        public TaskList CreateTaskListFromTaskList(TaskList taskList)
        {
            if (taskList == null)
            {
                throw new ArgumentNullException(nameof(taskList));
            }

            lock (KnownTaskListLock)
            {
                if (KnownTaskLists.Exists(x => x.Guid == taskList.Guid))
                {
                    throw new FactoryOrchestratorException(string.Format(CultureInfo.CurrentCulture, Resources.TaskListExistsAlready, taskList.Guid));
                }
                else
                {
                    taskList.ValidateTaskList();
                    KnownTaskLists.Add(taskList);
                    OnTaskManagerEvent?.Invoke(this, new TaskManagerEventArgs(TaskManagerEventType.NewTaskList, taskList.Guid, null));
                }
            }

            return taskList;
        }

        public void Reset(bool preserveLogs = true, bool terminateBackgroundTasks = true)
        {
            lock (RunningTaskListLock)
            {
                lock (KnownTaskListLock)
                {
                    // Cancel all running TaskLists
                    foreach (var token in RunningTaskListTokens.Values)
                    {
                        token.Cancel();
                    }

                    // Cancel any started testruns
                    foreach (var token in RunningTaskRunTokens.Values)
                    {
                        token.Cancel();
                    }

                    // Kill all background tasks
                    if (terminateBackgroundTasks)
                    {
                        Parallel.ForEach(RunningBackgroundTasks.SelectMany(x => x.Value), (bgRunner) =>
                        {
                            bgRunner.StopTask();
                        });

                        RunningBackgroundTasks.Clear();
                    }

                    // Reset all tests
                    foreach (var task in KnownTaskLists.SelectMany(x => x.Tasks))
                    {
                        ResetTask(task, preserveLogs);
                    }

                    // Delete state file
                    File.Delete(TaskListStateFile);

                    // Create new dictionaries
                    RunningTaskListTokens.Clear();
                    RunningTaskRunTokens.Clear();
                    KnownTaskLists.Clear();
                }
            }
        }

        /// <summary>
        /// Checks if a DLL is a TAEF test. Returns an initialized TAEFTest instance if it is.
        /// </summary>
        /// <param name="dllToTest">Path to DLL to check</param>
        /// <returns>null if DLL is not a TAEF test. TAEFTest instance if it is.</returns>
        public TAEFTest CheckForTAEFTest(String dllToTest)
        {
            // Try to list TAEF testcases to see if it is a valid TAEF test
            TAEFTest maybeTAEF = new TAEFTest(dllToTest);
            var taskRun = CreateTaskRunForTask(maybeTAEF, LogFolder);
            bool isTaef = false;
            using (var runner = new TaskRunner(taskRun))
            {
                try
                {
                    if (!runner.CheckIfTaefTest())
                    {
                        throw new FactoryOrchestratorException(string.Format(CultureInfo.CurrentCulture, Resources.TaefCheckFailed, dllToTest));
                    }
                    // Timeout after a second
                    if (!runner.WaitForExit(1000))
                    {
                        runner.StopTask();
                        throw new FactoryOrchestratorException(string.Format(CultureInfo.CurrentCulture, Resources.TaefCheckTimeout, dllToTest));
                    }

                    // "No tests were executed." error, returned when a binary is not a valid TAEF test.
                    // https://docs.microsoft.com/en-us/windows-hardware/drivers/taef/exit-codes-for-taef
                    if ((taskRun.ExitCode == 117440512) || (taskRun.ExitCode == 0))
                    {
                        // Check if it was able to enumerate the test cases.
                        if (!taskRun.TaskOutput.Any(x => x.Contains("Summary of Errors Outside of Tests")) && !taskRun.TaskOutput.Any(x => x.Contains("Failed to load")))
                        {
                            isTaef = true;
                        }
                    }
                    else
                    {
                        throw new FactoryOrchestratorException(string.Format(CultureInfo.CurrentCulture, Resources.TaefCheckReturnedError, taskRun.ExitCode, dllToTest));
                    }
                }
                catch (Exception e)
                {
                    throw new FactoryOrchestratorException(string.Format(CultureInfo.CurrentCulture, Resources.TaefValidationFailed, dllToTest), null, e);
                }
            }

            // Cleanup task run
            ResetTask(maybeTAEF, false);

            if (isTaef)
            {
                return maybeTAEF;
            }
            else
            {
                return null;
            }
        }

        public void UpdateTaskList(TaskList taskList)
        {
            if (taskList == null)
            {
                throw new ArgumentNullException(nameof(taskList));
            }

            lock (KnownTaskListLock)
            {
                var index = KnownTaskLists.FindIndex(x => x.Guid == taskList.Guid);
                if (index > -1)
                {
                    lock (RunningTaskListLock)
                    {
                        // if it is running don't update the tasklist
                        if (RunningTaskListTokens.Keys.Contains(taskList.Guid))
                        {
                            throw new FactoryOrchestratorTaskListRunningException(taskList.Guid);
                        }
                        else
                        {
                            KnownTaskLists[index] = taskList;
                            OnTaskManagerEvent?.Invoke(this, new TaskManagerEventArgs(TaskManagerEventType.UpdatedTaskList, taskList.Guid, null));
                        }
                    }
                }
                else
                {
                    throw new FactoryOrchestratorUnkownGuidException(taskList.Guid, typeof(TaskList));
                }
            }

            // Update XML for state tracking
            SaveAllTaskListsToXmlFile(TaskListStateFile);
        }

        public void UpdateTask(TaskBase updatedTest)
        {
            if (updatedTest == null)
            {
                throw new ArgumentNullException(nameof(updatedTest));
            }

            // Changing state, lock the task lists
            lock (KnownTaskListLock)
            {
                // Find list this task in it
                int taskIndex = -1;
                var taskList = KnownTaskLists.First(x => (taskIndex = x.Tasks.FindIndex(y => y.Guid.Equals(updatedTest.Guid))) != -1);

                if (taskList == null)
                {
                    throw new FactoryOrchestratorUnkownGuidException(updatedTest.Guid, typeof(TaskBase));
                }
                else
                {
                    lock (RunningTaskListLock)
                    {
                        // if it is running don't update the task
                        if (RunningTaskListTokens.Keys.Contains(taskList.Guid))
                        {
                            throw new FactoryOrchestratorTaskListRunningException(taskList.Guid);
                        }
                        else
                        {
                            taskList.Tasks[taskIndex] = updatedTest;
                        }
                    }
                }
            }

            // Update XML for state tracking
            SaveAllTaskListsToXmlFile(TaskListStateFile);
        }

        public void UpdateTaskRun(TaskRun latestTaskRun)
        {
            if (latestTaskRun == null)
            {
                throw new ArgumentNullException(nameof(latestTaskRun));
            }

            UpdateTaskRunInternal(latestTaskRun);
        }

        public bool RunAllTaskLists()
        {
            lock (KnownTaskListLock)
            {
                foreach (var list in KnownTaskLists)
                {
                    RunTaskList(list.Guid);
                }
            }

            return true;
        }

        public void RunTaskList(Guid taskListGuidToRun, int startIndex = 0)
        {
            TaskList list = KnownTaskLists.Find(x => x.Guid == taskListGuidToRun);

            // Check if task list is valid
            if (list == null)
            {
                throw new FactoryOrchestratorUnkownGuidException(taskListGuidToRun, typeof(TaskList));
            }

            // Check if task list is already running or set to be run
            lock (RunningTaskListLock)
            {
                if (RunningTaskListTokens.ContainsKey(taskListGuidToRun))
                {
                    throw new FactoryOrchestratorTaskListRunningException(taskListGuidToRun);
                }

                // Create testrun for all background tasks in the list
                List<Guid> backgroundTaskRunGuids = new List<Guid>();
                foreach (var task in list.BackgroundTasks)
                {
                    backgroundTaskRunGuids.Add(CreateTaskRunForTask(task, LogFolder, taskListGuidToRun).Guid);
                }

                // Create taskrun for all tasks in the list
                List<Guid> taskRunGuids = new List<Guid>();
                var tasks = list.Tasks.ToList();
                for (int i = startIndex; i < tasks.Count; i++)
                {
                    tasks[i].TimesRetried = 0;
                    taskRunGuids.Add(CreateTaskRunForTask(tasks[i], LogFolder, taskListGuidToRun).Guid);
                }

                // Update XML for state tracking.
                SaveAllTaskListsToXmlFile(TaskListStateFile);

                var workItem = new TaskListWorkItem(list.Guid, backgroundTaskRunGuids, taskRunGuids, list.TerminateBackgroundTasksOnCompletion, list.AllowOtherTaskListsToRun, list.RunInParallel);
                QueueTaskListWorkItem(workItem);
            }
        }

        public void RunTaskListFromInitial(Guid taskListToRun, int initialTask)
        {
            RunTaskList(taskListToRun, initialTask);
        }

        private class TaskListWorkItem
        {
            public TaskListWorkItem(Guid taskListGuid, List<Guid> backgroundTaskRunGuids, List<Guid> taskRunGuids, bool terminateBackgroundTasksOnCompletion, bool allowOtherTaskListsToRun, bool runListInParallel)
            {
                TaskListGuid = taskListGuid;
                BackgroundTaskRunGuids = backgroundTaskRunGuids;
                AllowOtherTaskListsToRun = allowOtherTaskListsToRun;
                RunListInParallel = runListInParallel;
                TerminateBackgroundTasksOnCompletion = terminateBackgroundTasksOnCompletion;
                TaskRunGuids = taskRunGuids;
            }

            public Guid TaskListGuid;
            public bool AllowOtherTaskListsToRun;
            public bool RunListInParallel;
            public bool TerminateBackgroundTasksOnCompletion;
            public List<Guid> TaskRunGuids;
            public List<Guid> BackgroundTaskRunGuids;
        }

        private void DoTaskList(object i, CancellationToken token)
        {
            var item = (TaskListWorkItem)i;
            bool usedSem;

            if (!item.AllowOtherTaskListsToRun)
            {
                try
                {
                    _startNonParallelTaskRunLock.Wait(token);
                }
                catch (OperationCanceledException)
                {
                    // Need to mark everything as aborted below
                }

                usedSem = true;
            }
            else
            {
                usedSem = _startNonParallelTaskRunLock.Wait(0, token);
            }

            try
            {
                if (!token.IsCancellationRequested)
                {
                    OnTaskManagerEvent?.Invoke(this, new TaskManagerEventArgs(TaskManagerEventType.TaskListStarted, item.TaskListGuid, TaskStatus.Running));
                    StartTaskRuns(item.TaskListGuid, item.BackgroundTaskRunGuids, item.TaskRunGuids, token, item.TerminateBackgroundTasksOnCompletion, item.RunListInParallel);

                    // Tests are done! Update the tokens & locks
                    lock (RunningTaskListLock)
                    {
                        RunningTaskListTokens.TryRemove(item.TaskListGuid, out var removed);
                        removed?.Dispose();
                    }
                    if (usedSem)
                    {
                        _startNonParallelTaskRunLock.Release();
                        usedSem = false;
                    }

                    OnTaskManagerEvent?.Invoke(this, new TaskManagerEventArgs(TaskManagerEventType.TaskListFinished, item.TaskListGuid, GetTaskList(item.TaskListGuid).TaskListStatus));
                }
            }
            finally
            {
                if (usedSem)
                {
                    _startNonParallelTaskRunLock.Release();
                }
            }

            if (token.IsCancellationRequested)
            {
                foreach (var runGuid in item.TaskRunGuids)
                {
                    var run = GetTaskRunByGuid(runGuid);
                    if (!run.TaskRunComplete)
                    {
                        run.TaskStatus = TaskStatus.Aborted;
                        run.ExitCode = -2147467260; // E_ABORT
                        run.OwningTask.LatestTaskRunStatus = TaskStatus.Aborted;
                        run.OwningTask.LatestTaskRunExitCode = -2147467260; // E_ABORT
                    }
                }

                // Update XML for state tracking. This is only needed if a tasklist was aborted.
                SaveAllTaskListsToXmlFile(TaskListStateFile);
            }
        }

        private void StartTaskRuns(Guid taskListGuid, List<Guid> backgroundTaskRunGuids, List<Guid> taskRunGuids, CancellationToken token, bool terminateBackgroundTasksOnCompletion = true, bool runInParallel = false, TaskRunnerEventHandler taskRunEventHandler = null)
        {
            var currentBgTasks = new List<TaskRun>();
            // Start all background tasks
            Parallel.ForEach(backgroundTaskRunGuids, (bgRunGuid, state) =>
            {
                var taskRun = GetTaskRunByGuid(bgRunGuid);
                var testToken = new CancellationTokenSource();
                StartTask(taskRun, token, taskRunEventHandler, true);
                RunningBackgroundTasks[taskListGuid].Add(taskRun.GetOwningTaskRunner());
                currentBgTasks.Add(taskRun);
            });

            // Wait for all background tasks to start
            while (currentBgTasks.Any(x => x.TaskStatus == TaskStatus.RunPending))
            {
                Thread.Sleep(30);
            }

            // Run all tasks in the list
            if (!runInParallel)
            {
                foreach (var runGuid in taskRunGuids)
                {
                    var taskRun = GetTaskRunByGuid(runGuid);

                    if (token.IsCancellationRequested)
                    {
                        taskRun.TaskStatus = TaskStatus.Aborted;
                        taskRun.OwningTask.LatestTaskRunStatus = TaskStatus.Aborted;
                    }
                    else
                    {
                        var testToken = new CancellationTokenSource();
                        RunningTaskRunTokens.TryAdd(taskRun.Guid, testToken);
                        StartTask(taskRun, testToken.Token, taskRunEventHandler, false);

                        // Update saved state
                        SaveAllTaskListsToXmlFile(TaskListStateFile);
                    }
                }
            }
            else
            {
                Parallel.ForEach(taskRunGuids, (runGuid) =>
                {
                    var taskRun = GetTaskRunByGuid(runGuid);

                    if (token.IsCancellationRequested)
                    {
                        taskRun.TaskStatus = TaskStatus.Aborted;
                        taskRun.OwningTask.LatestTaskRunStatus = TaskStatus.Aborted;
                    }
                    else
                    {
                        var testToken = new CancellationTokenSource();
                        RunningTaskRunTokens.TryAdd(taskRun.Guid, testToken);
                        StartTask(taskRun, token, taskRunEventHandler);

                        // Update saved state
                        SaveAllTaskListsToXmlFile(TaskListStateFile);
                    }
                });
            }

            // Kill all background tasks
            if (terminateBackgroundTasksOnCompletion)
            {
                Parallel.ForEach(RunningBackgroundTasks[taskListGuid], (bgRunner) =>
                {
                    bgRunner.StopTask();
                });

                RunningBackgroundTasks.TryRemove(taskListGuid, out var removed);
            }
        }

        private void StartTask(ServerTaskRun taskRun, CancellationToken token, TaskRunnerEventHandler taskRunEventHandler = null, bool backgroundTask = false)
        {
            if (!token.IsCancellationRequested)
            {
                TaskRunner runner = null;
                Timer externalTimeoutTimer = null;
                bool waitingForResult = true;
                var UWPTask = taskRun.OwningTask as UWPTask;

                OnTaskManagerEvent?.Invoke(this, new TaskManagerEventArgs(TaskManagerEventType.TaskRunStarted, taskRun.Guid, TaskStatus.RunPending));

                try
                {
                    if (taskRun.RunByServer)
                    {
                        if (taskRun.TaskType == TaskType.ConsoleExe)
                        {
                            if ((_supportsWin32Gui) && (System.Security.Principal.WindowsIdentity.GetCurrent().IsSystem) && (GetPeSubsystem(taskRun.TaskPath) == Subsystem.WindowsGui))
                            {
                                // Program is Win32 GUI, if RunAsRDUser is present, redirect to it.
                                // This allows the program to run as a user account and show its GUI.
                                if (File.Exists(Path.Combine(Environment.SystemDirectory, "RunAsRDUser.exe")))
                                {
                                    taskRun.TaskOutput.Add(string.Format(CultureInfo.CurrentCulture, Resources.RedirectingToRunAsRDUser, taskRun.TaskPath));
                                    var currentPath = taskRun.TaskPath;
                                    taskRun.TaskPath = Path.Combine(Environment.SystemDirectory, "RunAsRDUser.exe");
                                    taskRun.Arguments = $"{currentPath} {taskRun.Arguments}";
                                }
                                else
                                {
                                    taskRun.TaskOutput.Add(string.Format(CultureInfo.CurrentCulture, Resources.RunningGuiAsSystemWarning, taskRun.TaskPath));
                                }
                            }
                        }

#pragma warning disable CA2000 // Dispose objects before losing scope. It is disposed by the runner object itself when it completes or is aborted.
                        runner = new TaskRunner(taskRun);
#pragma warning restore CA2000
                        waitingForResult = false;

                        if (taskRunEventHandler != null)
                        {
                            runner.OnTestEvent += taskRunEventHandler;
                        }

                        // If the process failed to start, the error is logged to the TaskRun output
                        runner.Run();
                    }
                    else
                    {
                        // Write log file. Unlike an executable Task, we must do this manually.
                        taskRun.WriteLogHeader();

                        // Attempt to start the UWP app
                        if ((taskRun.TaskType == TaskType.UWP) && (_isWindows))
                        {
                            HttpWebResponse response = null;
                            bool restFailed = false;
                            var errorCode = 0;
                            try
                            {
                                var s = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(taskRun.TaskPath));
                                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(@"http://127.0.0.1/api/taskmanager/app?appid=" + s);
                                request.Method = "POST";
                                response = (HttpWebResponse)request.GetResponse();
                            }
                            catch (Exception e)
                            {
                                restFailed = true;
                                errorCode = e.HResult;
                            }

                            if (restFailed || (response.StatusCode != HttpStatusCode.OK))
                            {
                                waitingForResult = false;
                                taskRun.TaskOutput.Add(string.Format(CultureInfo.CurrentCulture, Resources.WDPAppLaunchFailed, taskRun.TaskPath));
                                taskRun.TaskOutput.Add(Resources.WDPAppLaunchFailed2);
                                taskRun.TaskOutput.Add(Resources.WDPAppLaunchFailed3);
                                taskRun.TaskStatus = TaskStatus.Failed;
                                taskRun.ExitCode = (errorCode == 0) ? (int)response.StatusCode : errorCode;
                                taskRun.TimeFinished = DateTime.Now;
                                taskRun.WriteLogFooter();
                            }
                            else
                            {
                                taskRun.TaskOutput.Add(string.Format(CultureInfo.CurrentCulture, Resources.WDPAppLaunchSucceeded, taskRun.TaskPath));
                                if (taskRun.OwningTask == null)
                                {
                                    // App was launched via RunApp(), and is not part of a Task. Complete it.
                                    waitingForResult = false;
                                    taskRun.TaskStatus = TaskStatus.Passed;
                                    taskRun.ExitCode = 0;
                                    taskRun.TimeFinished = DateTime.Now;
                                    taskRun.WriteLogFooter();
                                }
                            }
                        }

                        if (waitingForResult)
                        {
                            // Mark the run as started.
                            taskRun.StartWaitingForExternalResult();

                            if (UWPTask != null && UWPTask.AutoPassedIfLaunched)
                            {
                                taskRun.TaskStatus = TaskStatus.Passed;
                                taskRun.ExitCode = 0;
                            }
                            else
                            {
                                // Let the service know we are waiting for a result
                                OnTaskManagerEvent?.Invoke(this, new TaskManagerEventArgs(TaskManagerEventType.WaitingForExternalTaskRunStarted, taskRun.Guid, TaskStatus.Running));

                                // Start timeout timer
                                if (taskRun.TimeoutSeconds > 0)
                                {
                                    externalTimeoutTimer = new System.Threading.Timer(new TimerCallback(ExternalTaskRunTimeout), taskRun.Guid, taskRun.TimeoutSeconds * 1000, Timeout.Infinite);
                                }
                            }
                        }
                    }

                    // Wait for task to finish, timeout, or be aborted
                    if (!taskRun.BackgroundTask)
                    {
                        while (!token.IsCancellationRequested && !taskRun.TaskRunComplete)
                        {
                            // TODO: Performance: replace with a signal mechanism
                            // TODO: Feature, Performance: Just sleep for the timeout value if possible
                            Thread.Sleep(1000);
                        }
                    }

                    if (token.IsCancellationRequested)
                    {
                        if (taskRun.RunByServer)
                        {
                            // Task was canceled, stop executing it.
                            runner.StopTask();
                        }
                        else
                        {
                            lock (taskRun.OwningTask.TaskLock)
                            {
                                if (!taskRun.TaskRunComplete)
                                {
                                    // External, disable timeout, kill process, mark as Aborted
                                    externalTimeoutTimer?.Change(Timeout.Infinite, Timeout.Infinite);

                                    taskRun.TaskStatus = TaskStatus.Aborted;

                                    if ((UWPTask != null) && (_isWindows))
                                    {
                                        KillAppProcess(taskRun);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    // Something went wrong executing the Task. Log the failure and fail the Task.
                    taskRun.TaskOutput.Add(Resources.TaskRunUnhandledExceptionError);
                    taskRun.TaskOutput.Add(e.AllExceptionsToString());
                    taskRun.ExitCode = (e.HResult == 0) ? -2147467259 : e.HResult; // E_FAIL
                    taskRun.TimeFinished = DateTime.Now;
                    taskRun.TaskStatus = TaskStatus.Failed;
                }
                finally
                {
                    // If external Task, disable timeout
                    externalTimeoutTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                    externalTimeoutTimer?.Dispose();
                }

                if (taskRun.RunByClient)
                {
                    // Mark the run as finished.
                    taskRun.EndWaitingForExternalResult(waitingForResult);

                    if (waitingForResult)
                    {
                        // Exit the app (if desired)
                        if (UWPTask != null && !UWPTask.AutoPassedIfLaunched && UWPTask.TerminateOnCompleted &&
                            _isWindows && KillAppProcess(taskRun))
                        {
                            taskRun.TaskOutput.Add(string.Format(CultureInfo.CurrentCulture, Resources.AppTerminated, taskRun.TaskPath));
                        }
                    }

                    // Write log file. Unlike an executable Task, we must do this manually.
                    taskRun.WriteLogFooter();

                    // Let the service know we got a result
                    OnTaskManagerEvent?.Invoke(this, new TaskManagerEventArgs(TaskManagerEventType.WaitingForExternalTaskRunFinished, taskRun.Guid, taskRun.TaskStatus));
                }

                OnTaskManagerEvent?.Invoke(this, new TaskManagerEventArgs(TaskManagerEventType.TaskRunFinished, taskRun.Guid, taskRun.TaskStatus));

                if ((!token.IsCancellationRequested) && (taskRun.OwningTask != null) && (taskRun.TaskStatus != TaskStatus.Passed) && (!backgroundTask))
                {
                    // Task failed, was not cancelled, and is not a background task.
                    // Check the OwningTask for special cases.
                    if (taskRun.OwningTask.TimesRetried < taskRun.OwningTask.MaxNumberOfRetries)
                    {
                        // Re-run the task under a new TaskRun
                        var newRun = CreateTaskRunForTask(taskRun.OwningTask, LogFolder);
                        taskRun.OwningTask.TimesRetried++;
                        StartTask(newRun, token, taskRunEventHandler);
                    }
                    else if (taskRun.OwningTask.AbortTaskListOnFailed)
                    {
                        AbortTaskList((Guid)taskRun.OwningTaskListGuid);
                    }
                }
            }
            else
            {
                taskRun.ExitCode = -2147467260; // E_ABORT
                taskRun.TaskStatus = TaskStatus.Aborted;
                taskRun.OwningTask.LatestTaskRunStatus = TaskStatus.Aborted;
            }
        }

        private static PEUtility.Subsystem GetPeSubsystem(string filename)
        {
            try
            {
                var pe = new PEUtility.Executable(filename);
                return pe.Subsystem;
            }
            catch (Exception)
            {
                return Subsystem.Unknown;
            }
        }

        private void ExternalTaskRunTimeout(object state)
        {
            // Kill process, mark as timeout
            ServerTaskRun run = GetTaskRunByGuid((Guid)state);

            lock (run.OwningTask.TaskLock)
            {
                if (!run.TaskRunComplete)
                {
                    KillAppProcess(run);

                    run.ExitCode = -2147467260; // E_ABORT
                    run.TaskStatus = TaskStatus.Timeout;
                }
            }
        }

        private static bool KillAppProcess(ServerTaskRun run)
        {
            // Use WDP to find the app package full name
            string appFullName = "";
            try
            {
                var response = WDPHelpers.WdpHttpClient.GetAsync(new Uri("http://localhost/api/app/packagemanager/packages")).Result;

                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                List<string> aumids = new List<string>();
                var output = response.Content.ReadAsStringAsync().Result;
                var matches = Regex.Matches(output, "\"PackageFullName\" : \"(.+?)\".+?\"PackageRelativeId\" : \"(.+?)\"");
                foreach (Match match in matches)
                {
                    if (match.Groups[2].Value.Equals(run.TaskPath, StringComparison.OrdinalIgnoreCase))
                    {
                        appFullName = match.Groups[1].Value;
                        break;
                    }
                }
            }
            catch (Exception)
            { } // Igore WDP errors

            if (String.IsNullOrWhiteSpace(appFullName))
            {
                return false;
            }

            // If the name is found, look for it in the path of all running processes
            var processes = Process.GetProcesses();
            foreach (var process in processes)
            {
                try
                {
                    if (process.MainModule.FileName.ToUpperInvariant().Contains(appFullName.ToUpperInvariant()))
                    {
                        // app package full name is in the process path, kill the process and return
                        process.Kill();
                        return true;
                    }
                }
                // We might not have permissions for every process
                catch (Exception)
                { }
            }

            return false;
        }

        public void AbortAll()
        {
            lock (RunningTaskListLock)
            {
                foreach (var token in RunningTaskListTokens.Values)
                {
                    token.Cancel();
                }
                foreach (var token in RunningTaskRunTokens.Values)
                {
                    token.Cancel();
                }

                RunningTaskListTokens.Clear();
                RunningTaskRunTokens.Clear();
            }
        }

        public void AbortTaskList(Guid taskListToCancel)
        {
            lock (RunningTaskListLock)
            {
                if (RunningTaskListTokens.TryGetValue(taskListToCancel, out var token))
                {
                    token.Cancel();
                    RunningTaskListTokens.TryRemove(taskListToCancel, out var removed);
                    removed?.Dispose();

                    var taskRunGuids = KnownTaskLists.Find(x => x.Guid == taskListToCancel).Tasks.Select(x => x.LatestTaskRunGuid);
                    if (taskRunGuids != null)
                    {
                        foreach (var guid in taskRunGuids)
                        {
                            if (RunningTaskRunTokens.TryGetValue((Guid)guid, out token))
                            {
                                token.Cancel();
                                RunningTaskRunTokens.TryRemove((Guid)guid, out removed);
                                removed?.Dispose();
                            }
                        }
                    }
                }
            }
        }

        public void AbortTaskRun(Guid taskRunToCancel)
        {
            lock (RunningTaskListLock)
            {
                if (RunningTaskRunTokens.TryGetValue(taskRunToCancel, out var token))
                {
                    token.Cancel();
                    RunningTaskRunTokens.TryRemove(taskRunToCancel, out var removed);
                    removed?.Dispose();
                }
                else if (RunningBackgroundTasks.TryGetValue(taskRunToCancel, out var backgroundTasks))
                {
                    // Task is not run by the cluent
                    var run = GetTaskRunByGuid(taskRunToCancel);

                    if (run.RunByServer)
                    {
                        backgroundTasks[0]?.StopTask();
                    }
                    else
                    {
                        run.TaskStatus = TaskStatus.Aborted;
                        run.ExitCode = -2147467260; // E_ABORT
                        UpdateTaskRun(run);
                        if (run.OwningTask != null)
                        {
                            run.OwningTask.LatestTaskRunStatus = TaskStatus.Aborted;
                            run.OwningTask.LatestTaskRunExitCode = -2147467260; // E_ABORT
                        }
                    }
                    RunningBackgroundTasks.TryRemove(taskRunToCancel, out var removed);
                }
                else
                {
                    foreach (var list in RunningBackgroundTasks.Values)
                    {
                        if (list.Select(x => x.ActiveTaskRunGuid).Contains(taskRunToCancel))
                        {
                            var runner = list.First(x => x.ActiveTaskRunGuid == taskRunToCancel);

                            if (runner != null)
                            {
                                runner.StopTask();
                                list.Remove(runner);
                            }
                        }
                    }
                }
            }
        }

        public TaskRun RunExecutableAsBackgroundTask(string exeFilePath, string arguments, string logFilePath = null, bool runInContainer = false)
        {
            var run = CreateTaskRunWithoutTask(exeFilePath, arguments, logFilePath, TaskType.ConsoleExe);
            run.BackgroundTask = true;
            run.RunInContainer = runInContainer;
            using (var token = new CancellationTokenSource())
            {
                StartTask(run, token.Token);
            }

            RunningBackgroundTasks.TryAdd(run.Guid, new List<TaskRunner>(1) { run.GetOwningTaskRunner() });

            return run;
        }

        public TaskRun RunTask(Guid taskGuid)
        {
            // Find list this task in it
            int taskIndex = -1;
            var list = KnownTaskLists.First(x => (taskIndex = x.Tasks.FindIndex(y => y.Guid.Equals(taskGuid))) != -1);

            if (list == null)
            {
                throw new FactoryOrchestratorUnkownGuidException(taskGuid, typeof(TaskBase));
            }

            // If list is running already, fail
            if (RunningTaskListTokens.TryGetValue(list.Guid, out var t))
            {
                throw new FactoryOrchestratorTaskListRunningException(list.Guid);
            }

            var task = list.Tasks[taskIndex];
            lock (task.TaskLock)
            {
                task.TimesRetried = 0;
            }

            var run = CreateTaskRunForTask(task, LogFolder, list.Guid);

            // Create a new TaskList to run, with only the specified Task but copy the settings from the existing list
            var runList = new List<Guid>
            {
                run.Guid
            };
            TaskListWorkItem workItem;

            if (run.BackgroundTask)
            {
                workItem = new TaskListWorkItem(list.Guid, runList, new List<Guid>(), list.TerminateBackgroundTasksOnCompletion, list.AllowOtherTaskListsToRun, list.RunInParallel);
            }
            else
            {
                workItem = new TaskListWorkItem(list.Guid, new List<Guid>(), runList, list.TerminateBackgroundTasksOnCompletion, list.AllowOtherTaskListsToRun, list.RunInParallel);
            }

            QueueTaskListWorkItem(workItem);

            return run;
        }

        public TaskRun RunTask(TaskBase task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            // If this task is associated with an existing TaskList, run it via the TaskList's TaskBase object, so the run is tracked properly
            if (GetTask(task.Guid) != null)
            {
                return RunTask(task.Guid);
            }

            // The given TaskBase object is not associated with an existing TaskList, but let's run it standalone.
            lock (task.TaskLock)
            {
                task.TimesRetried = 0;
            }
            var run = CreateTaskRunForTask(task, LogFolder);

            // TODO: Properly dispose of this token when the task completes.
            // The overall RunningBackgroundTasks & RunningTaskRunTokens logic could use some improvements.
#pragma warning disable CA2000 // Dispose objects before losing scope
            var token = new CancellationTokenSource();
#pragma warning restore CA2000 // Dispose objects before losing scope
            if (run.BackgroundTask)
            {
                StartTask(run, token.Token);
                RunningBackgroundTasks.TryAdd(run.Guid, new List<TaskRunner>(1) { run.GetOwningTaskRunner() });
            }
            else
            {
                RunningTaskRunTokens.TryAdd(run.Guid, token);
                Task t = new Task(() => { StartTask(run, token.Token); });
                t.Start();
            }

            return run;
        }

        private void QueueTaskListWorkItem(TaskListWorkItem workItem)
        {
            var token = new CancellationTokenSource();
            RunningTaskListTokens.TryAdd(workItem.TaskListGuid, token);
            RunningBackgroundTasks.TryAdd(workItem.TaskListGuid, new List<TaskRunner>());

            // PreferFairness ensures the TaskLists are executed in a "First In, First Out" manner.
            Task t = new Task((i) => { DoTaskList(i, token.Token); }, workItem, TaskCreationOptions.PreferFairness);
            t.Start();
        }

        public TaskRun RunApp(string packageFamilyName)
        {
            var run = CreateTaskRunWithoutTask(packageFamilyName, null, null, TaskType.UWP);
            var token = new CancellationTokenSource();
            RunningTaskRunTokens.TryAdd(run.Guid, token);
            Task t = new Task(() => { StartTask(run, token.Token); });
            t.Start();

            return run;
        }

        public ServerTaskRun CreateTaskRunForTask(TaskBase task, string logFolder)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }
            ServerTaskRun run;
            lock (task.TaskLock)
            {
                run = new ServerTaskRun(task, logFolder);

                // Add to GUID -> TaskRun map
                lock (_taskMapLock)
                {
                    _taskRunMap.TryAdd(run.Guid, run);
                }

                task.TaskRunGuids.Add(run.Guid);
                task.LatestTaskRunExitCode = null;
                task.LatestTaskRunStatus = TaskStatus.RunPending;
                task.LatestTaskRunTimeFinished = null;
                task.LatestTaskRunTimeStarted = null;
            }
            return run;
        }

        public ServerTaskRun CreateTaskRunForTask(TaskBase task, string logFolder, Guid taskListGuid)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }
            ServerTaskRun run;
            lock (task.TaskLock)
            {
                run = new ServerTaskRun(task, logFolder, taskListGuid);

                // Add to GUID -> TaskRun map
                lock (_taskMapLock)
                {
                    _taskRunMap.TryAdd(run.Guid, run);
                }

                task.TaskRunGuids.Add(run.Guid);
                task.LatestTaskRunExitCode = null;
                task.LatestTaskRunStatus = TaskStatus.RunPending;
                task.LatestTaskRunTimeFinished = null;
                task.LatestTaskRunTimeStarted = null;
            }
            return run;
        }

        public ServerTaskRun CreateTaskRunWithoutTask(string filePath, string arguments, string logFileOrFolder, TaskType type)
        {
            var run = new ServerTaskRun(filePath, arguments, logFileOrFolder, type);

            // Add to GUID -> TaskRun map
            lock (_taskMapLock)
            {
                _taskRunMap.TryAdd(run.Guid, run);
            }

            return run;
        }

        private void ResetTask(TaskBase task, bool preserveLogs = true)
        {
            lock (task.TaskLock)
            {
                task.LatestTaskRunStatus = TaskStatus.NotRun;
                task.LatestTaskRunExitCode = null;
                task.LatestTaskRunTimeFinished = null;
                task.LatestTaskRunTimeStarted = null;
                task.TaskRunGuids = new List<Guid>();
            }

            RemoveTaskRunsForTask(task.Guid, preserveLogs);
        }

        public List<Guid> GetTaskRunGuidsByTaskGuid(Guid taskGuid)
        {
            return _taskRunMap.Values.Where(x => x.OwningTaskGuid == taskGuid).Select(x => x.Guid).ToList();
        }

        public ServerTaskRun GetTaskRunByGuid(Guid taskRunGuid)
        {
            if (_taskRunMap.ContainsKey(taskRunGuid))
            {
                _taskRunMap.TryGetValue(taskRunGuid, out var run);
                return run;
            }
            else
            {
                var files = Directory.EnumerateFiles(LogFolder, $"*{taskRunGuid.ToString()}*", SearchOption.AllDirectories).ToList();
                if (files.Count == 1)
                {
                    return LoadTaskRunFromFile(files.First());
                }
                else if (files.Count == 0)
                {
                    return null;
                }
                else // Count > 1
                {
                    throw new FactoryOrchestratorException($"Multiple TaskRun log files found with GUID {taskRunGuid} in {LogFolder}!", taskRunGuid);
                }
            }
        }

        /// <summary>
        /// Loads a taskrun log file into a taskrun_server object. This has a very hard dependency on the output format, defined by WriteLogHeader() and WriteLogFooter().
        /// </summary>
        /// <param name="filePath">log file to load</param>
        /// <returns>created taskrun object</returns>
        private ServerTaskRun LoadTaskRunFromFile(string filePath)
        {
            ServerTaskRun run = null;

            try
            {
                var lines = File.ReadAllLines(filePath).ToList();

                // At a minimum, the task header must have been written to the log file, check for the start of the output section first.
                // This will throw an exception if it doesn't exist.
                var startOutput = lines.First(x => x.StartsWith("---------------", StringComparison.InvariantCulture));
                var taskRunGuid = new Guid(lines.First(x => x.StartsWith("TaskRun GUID:", StringComparison.InvariantCulture)).Split(' ').Last());

                // Get task guid if it exists
                Guid taskGuid;
                try
                {
                    taskGuid = new Guid(lines.First(x => x.StartsWith("Task GUID:", StringComparison.InvariantCulture)).Split(' ').Last());
                }
                catch (Exception)
                {
                    taskGuid = Guid.Empty;
                }

                // Create a new TaskRun object representing this TaskRun log file
                var task = GetTask(taskGuid);
                if (task != null)
                {
                    var list = KnownTaskLists.Where(x => x.Tasks.Any(y => y.Guid.Equals(taskGuid))).DefaultIfEmpty(null).First();
                    run = CreateTaskRunForTask(task, LogFolder, list.Guid);
                }
                else
                {
                    run = CreateTaskRunWithoutTask("", "", filePath, TaskType.External);
                }

                // Update run based on info in the log file. If a field cant be parsed, use a default
                if (!UpdateTaskRunGuid(run.Guid, taskRunGuid))
                {
                    // The taskrun already exists.
                    throw new FactoryOrchestratorException(string.Format(CultureInfo.CurrentCulture, Resources.DuplicateTaskRunGuid), run.Guid);
                }

                run.LogFilePath = filePath;
                run.TaskType = (TaskType)Enum.Parse(typeof(TaskType), lines.First(x => x.StartsWith("Type:", StringComparison.InvariantCulture)).Substring(6));
                run.Arguments = lines.First(x => x.StartsWith("Arguments: ", StringComparison.InvariantCulture)).Substring(11);
                run.TaskPath = lines.First(x => x.StartsWith("Path: ", StringComparison.InvariantCulture)).Substring(6);

                try
                {
                    run.TimeStarted = DateTime.Parse(lines.Where(x => x.StartsWith("Date/Time run: ", StringComparison.InvariantCulture)).First().Replace("Date/Time run: ", ""), CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {
                    run.TimeStarted = null;
                }

                try
                {
                    run.TimeFinished = run.TimeStarted + TimeSpan.Parse(lines.First(x => x.StartsWith("Time to complete:", StringComparison.InvariantCulture)).Split(' ').Last(), CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {
                    run.TimeFinished = null;
                }

                try
                {
                    run.TaskStatus = (TaskStatus)Enum.Parse(typeof(TaskStatus), lines.First(x => x.StartsWith("Result:", StringComparison.InvariantCulture)).Split(' ').Last());
                }
                catch (Exception)
                {
                    run.TaskStatus = TaskStatus.Unknown;
                }

                try
                {
                    run.ExitCode = int.Parse(lines.First(x => x.StartsWith("Exit code:", StringComparison.InvariantCulture)).Split(' ').Last(), CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {
                    run.ExitCode = null;
                }

                try
                {
                    var endOutput = lines.Last(x => x.StartsWith("---------------", StringComparison.InvariantCulture));
                    var startIndex = lines.IndexOf(startOutput);
                    var endIndex = lines.IndexOf(endOutput);
                    run.TaskOutput = new LockingList<string>();
                    run.TaskOutput.AddRange(lines.GetRange(startIndex + 1, endIndex - startIndex - 1));
                }
                catch (Exception)
                {
                    run.TaskOutput = new LockingList<string>();
                }
            }
            catch (Exception)
            {
                if (run != null)
                {
                    RemoveTaskRun(run.Guid, true);
                    run = null;
                }
            }

            if (run != null)
            {
                // Update it in the map, now that it is fully loaded
                UpdateTaskRunInternal(run);
            }

            return run;
        }

        /// <summary>
        /// Replaces the Guid for an existing TaskRun with a new Guid
        /// </summary>
        /// <param name="oldGuid"></param>
        /// <param name="newGuid"></param>
        /// <returns></returns>
        private bool UpdateTaskRunGuid(Guid oldGuid, Guid newGuid)
        {
            lock (_taskMapLock)
            {
                if (!_taskRunMap.TryRemove(oldGuid, out var run))
                {
                    return false;
                }
                run.Guid = newGuid;

                // Fix log path, as it also used the wrong guid.
                run.SetLogFile(run.LogFilePath.Replace(oldGuid.ToString(), newGuid.ToString()));

                if (!_taskRunMap.TryAdd(run.Guid, run))
                {
                    // the guid already exists!
                    return false;
                }
            }

            return true;
        }

        public void RemoveTaskRun(Guid taskRunGuid, bool preserveLogs)
        {
            ServerTaskRun taskRun = null;
            lock (_taskMapLock)
            {
                if (_taskRunMap.TryGetValue(taskRunGuid, out taskRun))
                {
                    _taskRunMap.TryRemove(taskRunGuid, out _);
                    if (!preserveLogs)
                    {
                        if (File.Exists(taskRun.LogFilePath))
                        {
                            File.Delete(taskRun.LogFilePath);
                        }
                    }
                }
            }
        }
        private void UpdateTaskRunInternal(TaskRun updatedTaskRun)
        {
            if (updatedTaskRun == null)
            {
                throw new ArgumentNullException(nameof(updatedTaskRun));
            }

            lock (_taskMapLock)
            {
                if (!_taskRunMap.ContainsKey(updatedTaskRun.Guid))
                {
                    throw new FactoryOrchestratorUnkownGuidException(updatedTaskRun.Guid, typeof(TaskRun));
                }

                if (_taskRunMap.TryGetValue(updatedTaskRun.Guid, out var run))
                {
                    run.ExitCode = updatedTaskRun.ExitCode;
                    run.TaskStatus = updatedTaskRun.TaskStatus;
                    run.TimeFinished = updatedTaskRun.TimeFinished;
                    run.TimeStarted = updatedTaskRun.TimeStarted;
                    run.TaskOutput = updatedTaskRun.TaskOutput;
                    run.UpdateOwningTaskFromTaskRun();
                }
            }
        }

        public void RemoveTaskRunsForTask(Guid taskGuid, bool preserveLogs)
        {
            Parallel.ForEach(GetTaskRunGuidsByTaskGuid(taskGuid), taskRunGuid =>
            {
                RemoveTaskRun(taskRunGuid, preserveLogs);
            });
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                _startNonParallelTaskRunLock.Dispose();
            }

            _isDisposed = true;
        }
        private List<TaskList> KnownTaskLists;
        private readonly ConcurrentDictionary<Guid, CancellationTokenSource> RunningTaskListTokens;
        private readonly ConcurrentDictionary<Guid, CancellationTokenSource> RunningTaskRunTokens;
        private readonly ConcurrentDictionary<Guid, List<TaskRunner>> RunningBackgroundTasks;
        private readonly object KnownTaskListLock = new object();
        private readonly object RunningTaskListLock = new object();
        private readonly SemaphoreSlim _startNonParallelTaskRunLock;
        private readonly bool _supportsWin32Gui;
        private bool _isDisposed;
        private bool _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        /// <summary>
        /// Tracks all the task runs that have ever occured, mapped by the task run GUID
        /// </summary>
        private readonly ConcurrentDictionary<Guid, ServerTaskRun> _taskRunMap;
        private readonly object _taskMapLock;

        public string LogFolder { get; private set; }
        public string TaskListStateFile { get; private set; }

        /// <summary>
        /// True if a TaskList is actively running. Lock RunningTaskListLock first if doing a destructive operation.
        /// </summary>
        public bool IsTaskListRunning
        {
            get
            {
                return (!RunningTaskListTokens.IsEmpty);
            }
        }

        public bool CanStartNewTaskListRun
        {
            get
            {
                if (!IsTaskListRunning)
                {
                    return true;
                }
                else
                {
                    // Check if a running TaskList doesn't allow a other TaskLists to run
                    return !KnownTaskLists.Any(x => (x.TaskListStatus == TaskStatus.Running) && (x.AllowOtherTaskListsToRun == false));
                }
            }
        }

        public event TaskManagerEventHandler OnTaskManagerEvent;
    }

    public delegate void TaskManagerEventHandler(object source, TaskManagerEventArgs e);

    public enum TaskManagerEventType
    {
        NewTaskList,
        UpdatedTaskList,
        DeletedTaskList,
        TaskListStarted,
        TaskListFinished,
        WaitingForExternalTaskRunStarted,
        WaitingForExternalTaskRunFinished,
        TaskRunStarted,
        TaskRunFinished
    }

    public class TaskManagerEventArgs : EventArgs
    {
        public TaskManagerEventArgs(TaskManagerEventType eventType, Guid? guid, TaskStatus? status)
        {
            Event = eventType;
            Guid = guid;
            Status = status;
        }

        public TaskManagerEventType Event { get; }
        public Guid? Guid { get; }
        public TaskStatus? Status { get; }
    }

    public delegate void TaskRunnerEventHandler(object source, TaskRunnerEventArgs e);

    public class TaskRunnerEventArgs : EventArgs
    {
        public TaskRunnerEventArgs(TaskStatus testStatus, int? eventStatusCode, String eventMessage)
        {
            TestStatus = testStatus;
            EventStatusCode = eventStatusCode;
            EventMessage = eventMessage;
        }

        public TaskStatus TestStatus { get; }
        public int? EventStatusCode { get; }
        public String EventMessage { get; }
    }

    public class TaskRunExceptionEvent : TaskRunnerEventArgs
    {
        public TaskRunExceptionEvent(TaskStatus eventType, int eventStatusCode, Exception eventException) :
            base(eventType, eventStatusCode, eventException?.ToString())
        {
            EventException = eventException;
        }

        public Exception EventException { get; }
    }

    public class TaskRunner : IDisposable
    {
        private static string _globalTeExePath = "";
        private const string GlobalTeArgs = "";

        public static string GlobalTeExePath
        {
            get
            {
                if (string.IsNullOrEmpty(_globalTeExePath))
                {
                    // Default TAEF (TE.exe) path, will use %PATH% var to find te.exe
                    return "te.exe";
                }
                else
                {
                    return _globalTeExePath;
                }
            }
            set
            {
                var newPath = Environment.ExpandEnvironmentVariables(value);
                if (File.Exists(newPath))
                {
                    _globalTeExePath = newPath;
                }
                else
                {
                    throw new FileNotFoundException(string.Format(CultureInfo.CurrentCulture, Resources.FileNotFoundException, newPath), newPath);
                }
            }

        }

        public static TaskRunner GetTaskRunnerForTaskRun(Guid taskRunGuid)
        {
            if (_taskRunnerMap.ContainsKey(taskRunGuid))
            {
                return _taskRunnerMap[taskRunGuid];
            }
            else
            {
                return null;
            }
        }

        public TaskRunner(ServerTaskRun taskRun)
        {
            IsRunning = false;
            ActiveTaskRun = taskRun ?? throw new ArgumentNullException(nameof(taskRun));
            _taskRunnerMap.TryAdd(taskRun.Guid, this);
        }

        public bool Run()
        {
            lock (runnerStateLock)
            {
                if (IsRunning == true)
                {
                    return true;
                }

                // Create Process object
                TaskProcess = CreateProcess();

                // Start the process
                return StartProcess();
            }
        }

        internal bool CheckIfTaefTest()
        {
            ActiveTaskRun.LogFilePath = null;
            lock (runnerStateLock)
            {
                if (IsRunning == true)
                {
                    return true;
                }

                // Create Process object
                TaskProcess = CreateProcess();

                // Override args to check if this is a valid TAEF test, not try to run it
                TaskProcess.StartInfo.Arguments = "\"" + ActiveTaskRun.TaskPath + "\"" + " /list";

                // Start the process
                return StartProcess();
            }
        }

        private Process CreateProcess()
        {
            TaskProcess = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();

            if (ActiveTaskRun.TaskType == TaskType.TAEFDll)
            {
                startInfo.FileName = GlobalTeExePath;
                startInfo.Arguments += "\"" + ActiveTaskRun.TaskPath + "\" " + GlobalTeArgs + " ";
            }
            else if (ActiveTaskRun.TaskType == TaskType.PowerShell)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (FindFileInPath("pwsh.exe") != "pwsh.exe")
                    {
                        // pwsh.exe (PowerShell Core 6, PowerShell 7+) is installed on the system, use it
                        startInfo.FileName = "pwsh.exe";
                    }
                    else
                    {
                        // Assume legacy powershell.exe is present. If it isn't, StartProcess() will fail gracefully.
                        startInfo.FileName = "powershell.exe";
                    }
                }
                else
                {
                    startInfo.FileName = "pwsh";
                }

                startInfo.Arguments += $"-NonInteractive -File \"{ActiveTaskRun.TaskPath}\" ";
            }
            else if (ActiveTaskRun.TaskType == TaskType.BatchFile)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    startInfo.FileName = "cmd.exe";
                    startInfo.Arguments += $"/C \"{ActiveTaskRun.TaskPath}\" ";
                }
                else
                {
                    startInfo.FileName = "bash";
                    startInfo.Arguments += $" \"{ActiveTaskRun.TaskPath}\" ";
                }
            }
            else
            {
                startInfo.FileName = ActiveTaskRun.TaskPath;
            }

            startInfo.Arguments += ActiveTaskRun.Arguments;

            // Configure IO redirection
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.CreateNoWindow = true;
            startInfo.Environment["PATH"] = Environment.GetEnvironmentVariable("PATH");

            if (ActiveTaskRun.TaskType == TaskType.TAEFDll)
            {
                startInfo.Environment["PATH"] += ";" + Path.GetDirectoryName(ActiveTaskRun.TaskPath);
                startInfo.WorkingDirectory = Path.GetDirectoryName(startInfo.FileName);
            }
            else
            {
                startInfo.WorkingDirectory = Path.GetDirectoryName(ActiveTaskRun.TaskPath);
            }

            // Configure event handling
            TaskProcess.EnableRaisingEvents = true;
            TaskProcess.Exited += OnExited;
            TaskProcess.OutputDataReceived += OnOutputData;
            TaskProcess.ErrorDataReceived += OnErrorData;

            TaskProcess.StartInfo = startInfo;
            return TaskProcess;
        }

        private bool StartProcess()
        {
            try
            {
                TaskProcess.Start();
                IsRunning = true;
            }
            catch (Exception e)
            {
                // Process failed to start. Log the failure.
                ActiveTaskRun.TaskStatus = TaskStatus.Failed;
                _taskRunnerMap.TryRemove(ActiveTaskRunGuid, out var removed);
                removed?.Dispose();
                ActiveTaskRun.TaskOutput.Add("ERROR: " + Resources.ProcessStartError);
                ActiveTaskRun.TaskOutput.Add(e.Message);
            }

            if (IsRunning)
            {
                // Process started successfully.
                ActiveTaskRun.TaskStatus = TaskStatus.Running;
                ActiveTaskRun.TimeStarted = DateTime.Now;

                // Start async read (OnOutputData, OnErrorData)
                TaskProcess.BeginErrorReadLine();
                TaskProcess.BeginOutputReadLine();
                // Start Task timeout timer if needed
                if ((!ActiveTaskRun.BackgroundTask) && (ActiveTaskRun.TimeoutSeconds != -1))
                {
                    Task.Run(async () =>
                    {
                        await Task.Delay(new TimeSpan(0, 0, ActiveTaskRun.TimeoutSeconds), TimeoutToken.Token);

                        // If we hit the timeout and the task didn't finish, stop it now.
                        if (!TimeoutToken.IsCancellationRequested)
                        {
                            TimeoutTask();
                        }
                    });
                }
            }

            // Update Task with result of starting the process
            ActiveTaskRun.UpdateOwningTaskFromTaskRun();

            // Write header to file
            ActiveTaskRun.WriteLogHeader();

            var LogFilePath = ActiveTaskRun.LogFilePath;
            if (LogFilePath != null)
            {
                if (!IsRunning)
                {
                    // Process.Start() failed. Finish writing the log file, as OnExited will never execute.
                    ActiveTaskRun.WriteLogFooter();
                    OnTestEvent?.Invoke(this, new TaskRunnerEventArgs(ActiveTaskRun.TaskStatus, ActiveTaskRun.ExitCode, Resources.ProcessStartError));
                    Dispose();
                }
                else
                {
                    // Update log file output every 5 seconds
                    outputTimer = new Timer(OnOutputTimer, null, 5000, 5000);
                }
            }

            return IsRunning;
        }

        private void OnOutputTimer(object state)
        {
            var logFilePath = ActiveTaskRun.LogFilePath;
            if (logFilePath != null)
            {
                lock (outputLock)
                {
                    ActiveTaskRun.WriteUpdatedLogOutput();
                }
            }
        }

        private void OnErrorData(object sender, DataReceivedEventArgs e)
        {
            // Use mutex to ensure output data is serialized
            lock (outputLock)
            {
                AddOutputLine(true, e.Data);
            }
        }

        private void OnOutputData(object sender, DataReceivedEventArgs e)
        {
            // Use mutex to ensure output data is serialized
            lock (outputLock)
            {
                AddOutputLine(false, e.Data);
            }
        }

        private void AddOutputLine(bool isStdErr, string line)
        {
            if (line != null)
            {
                if (isStdErr)
                {
                    line = "ERROR: " + line;
                }
                ActiveTaskRun.TaskOutput.Add(line);

                // TODO: Better parse TAEF files
                //if (ActiveTaskRun.TaskType == TaskType.TAEFDll)
                //{
                //    ParseTeOutput(line);
                //}
            }
        }

        //private void ParseTeOutput(string data)
        //{
        //    return;
        //}

        private void OnExited(object sender, EventArgs e)
        {
            // Stop output timer
            if (outputTimer != null)
            {
                outputTimer.Dispose();
            }

            // Cancel the task timeout
            TimeoutToken.Cancel();

            // Save the result of the task
            if (TaskAborted)
            {
                ActiveTaskRun.ExitCode = -2147467260; // E_ABORT
                ActiveTaskRun.TaskStatus = TaskTimeout ? TaskStatus.Timeout : TaskStatus.Aborted;
            }
            else
            {
                ActiveTaskRun.TimeFinished = DateTime.Now;
                ActiveTaskRun.ExitCode = TaskProcess.ExitCode;
                ActiveTaskRun.TaskStatus = (ActiveTaskRun.ExitCode == 0) ? TaskStatus.Passed : TaskStatus.Failed;
            }

            ActiveTaskRun.UpdateOwningTaskFromTaskRun();

            // Save task output to file
            // Wait for all output to complete
            TaskProcess.WaitForExit();

            lock (outputLock)
            {
                // Write remaining output to file
                ActiveTaskRun.WriteLogFooter();
            }

            IsRunning = false;
            _taskRunnerMap.TryRemove(ActiveTaskRunGuid, out var removed);
            removed?.Dispose();

            // Raise event if event handler exists
            OnTestEvent?.Invoke(this, new TaskRunnerEventArgs(ActiveTaskRun.TaskStatus, ActiveTaskRun.ExitCode, Resources.ProcessExited));

            // Dispose self, we are done executing
            this.Dispose();
        }

        private bool TimeoutTask()
        {
            TaskTimeout = true;
            return StopTask();
        }

        public bool StopTask()
        {
            lock (runnerStateLock)
            {
                TaskAborted = true;
                try
                {
                    // todo: Process.Kill() doesn't terminate child processes. This is most noticable when using the FO app command prompt as it launches cmd.exe /C"<User input>".
                    // NET Core 3.1 has a Kill(true) method to terminate child processes, but it isn't available in NET Standard, yet. Until then, this will leak processes.
                    TaskProcess.Kill();
                }
                catch (Exception)
                {
                    // Process likely exited already, just continue
                }

                // Wait ten seconds for OnExited to fire
                for (int i = 0; i < 100; i++)
                {
                    if (IsRunning)
                    {
                        Thread.Sleep(100);
                    }
                    else
                    {
                        break;
                    }
                }

                if (IsRunning)
                {
                    // OnExited never occurred, so Dispose() wasn't called. Call it ourselves.
                    Dispose();
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public bool WaitForExit()
        {
            return WaitForExit();
        }
        public bool WaitForExit(int milliseconds)
        {
            // After Process.WaitForExit returns true, we still may need to wait for the OnExited method to complete.
            // Maintain our own timer so we can still exit close to the expected ms timeout value the user supplied.
            var timer = Stopwatch.StartNew();

            if (IsRunning)
            {
                if (TaskProcess.WaitForExit(milliseconds))
                {
                    // Process exited, wait for IsRunning to be updated at the end of OnExited
                    while ((IsRunning) && (timer.ElapsedMilliseconds < milliseconds))
                    {
                        Thread.Sleep(15);
                    }

                    if (IsRunning)
                    {
                        // OnExited is still going on
                        return false;
                    }
                    else
                    {
                        // OnExited is complete
                        return true;
                    }
                }
                else
                {
                    // Process is still running
                    return false;
                }
            }
            else
            {
                // Process is not running and OnExited is complete
                return true;
            }
        }

        public bool IsRunning { get; set; }

        [JsonIgnore]
        public Guid ActiveTaskRunGuid
        {
            get
            {
                return ActiveTaskRun.Guid;
            }
        }

        private readonly ServerTaskRun ActiveTaskRun;
        public event TaskRunnerEventHandler OnTestEvent;
        private Process TaskProcess;
        private bool TaskAborted = false;
        private bool TaskTimeout = false;
        private readonly CancellationTokenSource TimeoutToken = new CancellationTokenSource();
        private Timer outputTimer = null;

        /// <summary>
        /// Lock to maintain consistent state of execution status (Run, Abort etc)
        /// </summary>
        private readonly object runnerStateLock = new object();
        /// <summary>
        /// Lock to maintain serial ordering of task stdout & stderr
        /// </summary>
        private readonly object outputLock = new object();

        private static readonly ConcurrentDictionary<Guid, TaskRunner> _taskRunnerMap = new ConcurrentDictionary<Guid, TaskRunner>();
        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    outputTimer?.Dispose();
                    TaskProcess?.Dispose();
                    TimeoutToken.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }

    /// <summary>
    /// Server side only TaskRun class.
    /// </summary>
    public class ServerTaskRun : TaskRun
    {
        // TODO: Move taskrunner into taskrun_server

        // TODO: Quality: The taskrun_server CTORS are convoluted and could be simplified.

        public ServerTaskRun(TaskBase owningTask, string logFolder, Guid TaskListGuid) : this(owningTask, logFolder)
        {
            OwningTaskListGuid = TaskListGuid;
        }

        public ServerTaskRun(TaskBase owningTask, string logFolder) : base(owningTask)
        {
            CtorCommon();
            OwningTaskListGuid = null;
            OwningTask = owningTask;

            // Setup log path
            SetLogFile(logFolder);
        }

        /// <summary>
        /// Private Ctor used for task runs not backed by a TaskBase object
        /// </summary>
        /// <param name="taskPath"></param>
        /// <param name="arguments"></param>
        /// <param name="logFileOrPath"></param>
        public ServerTaskRun(string taskPath, string arguments, string logFileOrPath, TaskType type) : base(null)
        {
            // Set values via method args. They weren't set by base Ctor since the owning task is null.
            TaskPath = taskPath;
            Arguments = arguments;
            TaskName = Path.GetFileName(taskPath);
            TaskType = type;
            OwningTaskListGuid = null;

            CtorCommon();

            if (logFileOrPath != null)
            {
                var ext = logFileOrPath.Substring(logFileOrPath.Length - 3);
                if (ext == ".log" || ext == ".txt")
                {
                    LogFilePath = logFileOrPath;
                }
                else
                {
                    LogFilePath = Path.Combine(logFileOrPath, String.Format(CultureInfo.InvariantCulture, "{0}_Run{1}.log", TaskName, Guid));
                }
            }
        }

        private void CtorCommon()
        {
            // If an executable, find the actual path to the file.
            if ((TaskType == TaskType.ConsoleExe) || (TaskType == TaskType.BatchFile))
            {
                // For container tasks, the executable search should happen inside the container
                if (!RunInContainer)
                {
                    TaskPath = FindFileInPath(TaskPath);
                }
            }
        }

        public void SetLogFile(string logFolder)
        {
            LogFilePath = Path.Combine(logFolder, TaskName, $"Run_{Guid}.log");
        }

        /// <summary>
        /// If the TaskRun is related to a Task, updates that Task.
        /// </summary>
        public void UpdateOwningTaskFromTaskRun()
        {
            if (OwningTask != null)
            {
                lock (OwningTask.TaskLock)
                {
                    OwningTask.LatestTaskRunStatus = this.TaskStatus;
                    OwningTask.LatestTaskRunTimeStarted = this.TimeStarted;
                    OwningTask.LatestTaskRunTimeFinished = this.TimeFinished;
                    OwningTask.LatestTaskRunExitCode = this.ExitCode;
                }
            }
        }

        /// <summary>
        /// Marks an external TaskRun as started.
        /// </summary>
        public void StartWaitingForExternalResult()
        {
            TimeStarted = DateTime.Now;
            TaskStatus = TaskStatus.WaitingForExternalResult;
            UpdateOwningTaskFromTaskRun();
        }

        /// <summary>
        /// Marks an external TaskRun as completed.
        /// </summary>
        public void EndWaitingForExternalResult(bool waitForResult)
        {
            TimeFinished = DateTime.Now;
            if (waitForResult && !RunInContainer)
            {
                TaskOutput.Add(string.Format(CultureInfo.CurrentCulture, Resources.EndWaitingForExternalResult, TaskStatus, ExitCode));
            }

            UpdateOwningTaskFromTaskRun();
        }

        /// <summary>
        /// Logs general info about a TaskRun to its log file, done with the TaskRun starts executing.
        /// </summary>
        public void WriteLogHeader()
        {
            // WARNING: Update LoadTaskRunFromFile() if you change the header format!
            if (LogFilePath != null)
            {
                try
                {
                    _logIndex = 0;
                    Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath));
                    List<string> header = new List<string>();

                    if (!BackgroundTask)
                    {
                        header.Add(String.Format(CultureInfo.InvariantCulture, "Task: {0}", TaskName));
                    }
                    else
                    {
                        header.Add(String.Format(CultureInfo.InvariantCulture, "Background Task: {0}", TaskName));
                    }

                    header.Add(String.Format(CultureInfo.InvariantCulture, "Task GUID: {0}", (OwningTaskGuid == null) ? "Not a known Task" : OwningTaskGuid.ToString()));
                    header.Add(String.Format(CultureInfo.InvariantCulture, "TaskRun GUID: {0}", Guid));
                    header.Add(String.Format(CultureInfo.InvariantCulture, "Type: {0}", TaskType));
                    header.Add(String.Format(CultureInfo.InvariantCulture, "Path: {0}", TaskPath));
                    header.Add(String.Format(CultureInfo.InvariantCulture, "Arguments: {0}", Arguments));
                    header.Add(String.Format(CultureInfo.InvariantCulture, "Date/Time run: {0}", (TimeStarted == null) ? "Never Started" : ((DateTime)TimeStarted).ToString(CultureInfo.InvariantCulture)));
                    header.Add(String.Format(CultureInfo.InvariantCulture, "--------------- Output --------------"));

                    File.WriteAllLines(LogFilePath, header);
                }
                catch (IOException e)
                {
                    // todo: logging
                    TaskOutput.Insert(0, $"{string.Format(CultureInfo.CurrentCulture, Resources.LogFileCreationFailed, LogFilePath)}.{Environment.NewLine}{e.AllExceptionsToString()}");
                    DeleteLogFile();
                }
            }
        }

        public void WriteUpdatedLogOutput()
        {
            if (LogFilePath != null)
            {
                if (_logIndex != TaskOutput.Count)
                {
                    try
                    {
                        File.AppendAllLines(LogFilePath, TaskOutput.GetRange(_logIndex, TaskOutput.Count - _logIndex));
                        _logIndex = TaskOutput.Count;
                    }
                    catch (IOException e)
                    {
                        // todo: logging
                        TaskOutput.Insert(0, $"{string.Format(CultureInfo.CurrentCulture, Resources.LogFileCreationFailed, LogFilePath)}.{Environment.NewLine}{e.AllExceptionsToString()}");
                        DeleteLogFile();
                    }
                }
            }
        }

        /// <summary>
        /// Logs the result of a TaskRun to its log file.
        /// </summary>
        public void WriteLogFooter()
        {
            // WARNING: Update LoadTaskRunFromFile() if you change the footer format!
            if (LogFilePath != null)
            {
                try
                {
                    WriteUpdatedLogOutput();
                    var footer = new List<string>
                    {
                        // End output section
                        "-------------------------------------",
                        $"Result: {TaskStatus}"
                    };
                    if (ExitCode != null)
                    {
                        footer.Add($"Exit code: {ExitCode}");
                    }
                    if (RunTime != null)
                    {
                        footer.Add($"Time to complete: {RunTime}");
                    }

                    File.AppendAllLines(LogFilePath, footer);
                }
                catch (IOException e)
                {
                    // todo: logging
                    TaskOutput.Insert(0, $"{string.Format(CultureInfo.CurrentCulture, Resources.LogFileCreationFailed, LogFilePath)}:{Environment.NewLine}{e.AllExceptionsToString()}");
                    DeleteLogFile();
                }
            }
        }

        private void DeleteLogFile()
        {
            if (LogFilePath != null)
            {
                try
                {
                    File.Delete(LogFilePath);
                }
                catch (IOException) { }

                LogFilePath = null;
            }
        }

        public TaskRunner GetOwningTaskRunner()
        {
            return TaskRunner.GetTaskRunnerForTaskRun(this.Guid);
        }

        [JsonIgnore]
        public TaskBase OwningTask { get; }

        [JsonIgnore]
        public Guid? OwningTaskListGuid { get; }

        /// <summary>
        /// Index of the last line written to the log file.
        /// </summary>
        private int _logIndex;
    }

    public static class HelperMethods
    {
        public static string FindFileInPath(string file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            file = Environment.ExpandEnvironmentVariables(file);

            if (!File.Exists(file))
            {
                if (String.IsNullOrEmpty(Path.GetDirectoryName(file)))
                {
                    string[] paths;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        paths = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';');
                    }
                    else
                    {
                        paths = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(':');
                    }

                    foreach (string testPath in paths)
                    {
                        string path = testPath.Trim();
                        if (!String.IsNullOrEmpty(path) && File.Exists(path = Path.Combine(path, file)))
                        {
                            return Path.GetFullPath(path);
                        }
                    }
                }

                // No match found, just return the existing path
                return file;
            }

            // file is a full path, return it
            return Path.GetFullPath(file);
        }
    }
}
