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

namespace Microsoft.FactoryOrchestrator.Server
{
    public class TaskManager_Server
    {
        public TaskManager_Server(string logFolder, string taskListStateFile)
        {
            _startNonParallelTaskRunLock = new SemaphoreSlim(1, 1);
            KnownTaskLists = new List<TaskList>();
            RunningTaskListTokens = new ConcurrentDictionary<Guid, CancellationTokenSource>();
            RunningBackgroundTasks = new ConcurrentDictionary<Guid, List<TaskRunner>>();
            RunningTaskRunTokens = new ConcurrentDictionary<Guid, CancellationTokenSource>();
            _taskRunMap = new ConcurrentDictionary<Guid, TaskRun_Server>();
            _taskMapLock = new object();
            TaskListStateFile = taskListStateFile;
            SetLogFolder(logFolder, false);
        }

        public void SetLogFolder(string newLogFolder, bool moveFiles)
        {
            if (newLogFolder == _logFolder)
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
                        if (moveFiles && (_logFolder != null) && (Directory.Exists(_logFolder)))
                        {
                            // Move existing folder to temp folder
                            var tempDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), "FOTemp");
                            CopyDirectory(_logFolder, tempDir, true);

                            // Delete old folder
                            Directory.Delete(_logFolder, true);

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
                        _logFolder = newLogFolder;
                    }
                }
            }
            catch (Exception e)
            {
                throw new FactoryOrchestratorException("Could not move log folder!", null, e);
            }
        }

        private static void CopyDirectory(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
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
            var bats = Directory.EnumerateFiles(path, "*.bat", searchOption);
            var cmds = Directory.EnumerateFiles(path, "*.cmd", searchOption);
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

            foreach (var cmd in cmds)
            {
                var task = new BatchFileTask(cmd);
                tests.Tasks.Add(task);
            }

            foreach (var bat in bats)
            {
                var task = new BatchFileTask(bat);
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
                    throw new FactoryOrchestratorException($"Could not save TaskLists to {filename}!");
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
                    throw new FactoryOrchestratorException($"Could not save TaskLists to {filename}!");
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

                        var index = KnownTaskLists.FindIndex(x => x.Guid == list.Guid);
                        if (index > -1)
                        {
                            // todo: consider this an error?
                            KnownTaskLists[index] = list;
                            OnTaskManagerEvent?.Invoke(this, new TaskManagerEventArgs(TaskManagerEventType.UpdatedTaskList, list.Guid, null));
                        }
                        else
                        {
                            KnownTaskLists.Add(list);
                            OnTaskManagerEvent?.Invoke(this, new TaskManagerEventArgs(TaskManagerEventType.NewTaskList, list.Guid, null));
                        }
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

            throw new Exception($"No Task with guid {taskGuid} exists!");
        }

        public TaskList GetTaskList(Guid guid)
        {
            TaskList list = KnownTaskLists.Find(x => x.Guid == guid);
            if (list != null)
            {
                return list;
            }
            else
            {
                throw new FactoryOrchestratorUnkownGuidException(guid, typeof(TaskList));
            }
        }

        public List<Guid> GetTaskListGuids()
        {
            return KnownTaskLists.Select(x => x.Guid).ToList();
        }

        public void ReorderTaskLists(List<Guid> newOrder)
        {
            lock (KnownTaskListLock)
            {
                if (KnownTaskLists.Count != newOrder.Count)
                {
                    throw new FactoryOrchestratorException($"Server has {KnownTaskLists.Count} TaskLists but new order has ony {newOrder.Count} GUIDs!");
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
            lock (KnownTaskListLock)
            {
                if (KnownTaskLists.Exists(x => x.Guid == taskList.Guid))
                {
                    throw new FactoryOrchestratorException($"TaskList with guid {taskList.Guid} already exists!");
                }
                else
                {
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
            TaskRunner runner = new TaskRunner(taskRun);
            bool isTaef = false;
            try
            {
                if (!runner.CheckIfTaefTest())
                {
                    throw new FactoryOrchestratorException(String.Format("Unable to invoke TE.exe to validate possible TAEF test: {0}", dllToTest));
                }
                // Timeout after a second
                if (!runner.WaitForExit(1000))
                {
                    runner.StopTask();
                    throw new FactoryOrchestratorException(String.Format("TE.exe timed out trying to validate possible TAEF test: {0}", dllToTest));
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
                    throw new FactoryOrchestratorException(String.Format("TE.exe returned error {0} when trying to validate possible TAEF test: {1}", taskRun.ExitCode, dllToTest));
                }
            }
            catch (Exception e)
            {
                throw new FactoryOrchestratorException(String.Format("Unable to validate possible TAEF test: {0}", dllToTest), null, e);
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
                throw new ArgumentNullException();
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
            }

            // Update XML for state tracking
            SaveAllTaskListsToXmlFile(TaskListStateFile);
        }

        public void UpdateTask(TaskBase updatedTest)
        {
            if (updatedTest == null)
            {
                throw new ArgumentNullException();
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
                throw new ArgumentNullException();
            }

            UpdateTaskRunInternal(latestTaskRun);
        }

        public bool RunAllTaskLists()
        {
            lock(KnownTaskListLock)
            {
                foreach (var list in KnownTaskLists)
                {
                    RunTaskList(list.Guid);
                    Thread.Sleep(5);
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

        public class TaskListWorkItem
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
            bool usedSem = false;

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
                usedSem = _startNonParallelTaskRunLock.Wait(0);
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
                        CancellationTokenSource removed;
                        RunningTaskListTokens.TryRemove(item.TaskListGuid, out removed);
                    }
                    if (usedSem)
                    {
                        _startNonParallelTaskRunLock.Release();
                        usedSem = false;
                    }

                    OnTaskManagerEvent?.Invoke(this, new TaskManagerEventArgs(TaskManagerEventType.TaskListStarted, item.TaskListGuid, GetTaskList(item.TaskListGuid).TaskListStatus));
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
                    run.TaskStatus = TaskStatus.Aborted;
                    run.ExitCode = -1;
                    run.OwningTask.LatestTaskRunStatus = TaskStatus.Aborted;
                    run.OwningTask.LatestTaskRunExitCode = -1;
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

                List<TaskRunner> removed;
                RunningBackgroundTasks.TryRemove(taskListGuid, out removed);
            }
        }

        private void StartTask(TaskRun_Server taskRun, CancellationToken token, TaskRunnerEventHandler taskRunEventHandler = null, bool backgroundTask = false)
        {
            if (!token.IsCancellationRequested)
            {
                TaskRunner runner = null;
                Timer externalTimeoutTimer = null;
                bool waitingForResult = true;
                var UWPTask = taskRun.OwningTask as UWPTask;

                OnTaskManagerEvent?.Invoke(this, new TaskManagerEventArgs(TaskManagerEventType.TaskRunStarted, taskRun.Guid, TaskStatus.RunPending));

                if (taskRun.RunByServer)
                {
                    runner = new TaskRunner(taskRun);
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
                    if ((taskRun.TaskType == TaskType.UWP) && (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)))
                    {
                        HttpWebResponse response = null;
                        bool restFailed = false;
                        try
                        {
                            var s = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(taskRun.TaskPath));
                            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(@"http://127.0.0.1/api/taskmanager/app?appid=" + s);
                            request.Method = "POST";
                            response = (HttpWebResponse)request.GetResponse();
                        }
                        catch (Exception)
                        {
                            restFailed = true;
                        }

                        if (restFailed || (response.StatusCode != HttpStatusCode.OK))
                        {
                            waitingForResult = false;
                            taskRun.TaskOutput.Add($"Error: Failed to launch AUMID: {taskRun.TaskPath}");
                            taskRun.TaskOutput.Add($"Error: Device Portal is required for app launch and may not be running on the system.");
                            taskRun.TaskOutput.Add($"Error: If it is running, the AUMID may be incorrect.");
                            taskRun.TaskStatus = TaskStatus.Failed;
                            taskRun.ExitCode = -1;
                            taskRun.TimeFinished = DateTime.Now;
                            taskRun.WriteLogFooter();
                        }
                        else
                        {
                            taskRun.TaskOutput.Add($"Sucessfully launched app with AUMID: {taskRun.TaskPath}");
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

                                if ((UWPTask != null) && (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)))
                                {
                                    KillAppProcess(taskRun);
                                }
                            }
                        }
                    }
                }

                if (taskRun.RunByClient)
                {
                    // Mark the run as finished.
                    taskRun.EndWaitingForExternalResult(waitingForResult);

                    if (waitingForResult)
                    {   
                        // disable timeout
                        externalTimeoutTimer?.Change(Timeout.Infinite, Timeout.Infinite);

                        // Exit the app (if desired)
                        if (UWPTask != null && !UWPTask.AutoPassedIfLaunched && UWPTask.TerminateOnCompleted &&
                            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && KillAppProcess(taskRun))
                        {
                            taskRun.TaskOutput.Add($"Terminated app: {taskRun.TaskPath}");
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
                 taskRun.ExitCode = -1;
                 taskRun.TaskStatus = TaskStatus.Aborted;
                 taskRun.OwningTask.LatestTaskRunStatus = TaskStatus.Aborted;
            }
        }

        private void ExternalTaskRunTimeout(object state)
        {
            // Kill process, mark as timeout
            TaskRun_Server run = GetTaskRunByGuid((Guid)state);

            lock (run.OwningTask.TaskLock)
            {
                if (!run.TaskRunComplete)
                {
                    KillAppProcess(run);

                    run.ExitCode = -1;
                    run.TaskStatus = TaskStatus.Timeout;
                }
            }
        }

        private bool KillAppProcess(TaskRun_Server run)
        {
            // Use WDP to find the app package full name
            string appFullName = "";
            try
            {
                var response = Impersonation.WdpHttpClient.GetAsync(new Uri("http://127.0.0.1/api/app/packagemanager/packages")).Result;

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
                    if (process.MainModule.FileName.ToLowerInvariant().Contains(appFullName.ToLowerInvariant()))
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
                CancellationTokenSource token;
                if (RunningTaskListTokens.TryGetValue(taskListToCancel, out token))
                {
                    token.Cancel();
                    CancellationTokenSource removed;
                    RunningTaskListTokens.TryRemove(taskListToCancel, out removed);

                    var taskRunGuids = KnownTaskLists.Find(x => x.Guid == taskListToCancel).Tasks.Select(x => x.LatestTaskRunGuid);
                    if (taskRunGuids != null)
                    {
                        foreach (var guid in taskRunGuids)
                        {
                            if (RunningTaskRunTokens.TryGetValue((Guid)guid, out token))
                            {
                                token.Cancel();
                                RunningTaskRunTokens.TryRemove((Guid)guid, out removed);
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
                CancellationTokenSource token;
                List<TaskRunner> backgroundTasks;

                if (RunningTaskRunTokens.TryGetValue(taskRunToCancel, out token))
                {
                    token.Cancel();
                    CancellationTokenSource removed;
                    RunningTaskRunTokens.TryRemove(taskRunToCancel, out removed);
                }
                else if (RunningBackgroundTasks.TryGetValue(taskRunToCancel, out backgroundTasks))
                {
                    backgroundTasks[0].StopTask();
                    List<TaskRunner> removed;
                    RunningBackgroundTasks.TryRemove(taskRunToCancel, out removed);
                }
                else 
                {
                    foreach (var list in RunningBackgroundTasks.Values)
                    {
                        if (list.Select(x => x.ActiveTaskRunGuid).Contains(taskRunToCancel))
                        {
                            var runner = list.First(x => x.ActiveTaskRunGuid == taskRunToCancel);
                            runner.StopTask();
                            list.Remove(runner);
                        }
                    }
                }
            }
        }

        public TaskRun RunExecutableAsBackgroundTask(string exeFilePath, string arguments, string logFilePath = null)
        {
            var run = CreateTaskRunWithoutTask(exeFilePath, arguments, logFilePath, TaskType.ConsoleExe);
            run.BackgroundTask = true;
            var token = new CancellationTokenSource();
            StartTask(run, token.Token);

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
            CancellationTokenSource t;
            if (RunningTaskListTokens.TryGetValue(list.Guid, out t))
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
            var runList = new List<Guid>();
            runList.Add(run.Guid);
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
                throw new ArgumentNullException();
            }

            if (GetTask(task.Guid) != null)
            {
                return RunTask(task.Guid);
            }

            lock (task.TaskLock)
            {
                task.TimesRetried = 0;
            }
            var run = CreateTaskRunForTask(task, LogFolder);
            var token = new CancellationTokenSource();

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

        public TaskRun_Server CreateTaskRunForTask(TaskBase task, string logFolder)
        {
            TaskRun_Server run;
            lock (task.TaskLock)
            {
                run = new TaskRun_Server(task, logFolder);

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

        public TaskRun_Server CreateTaskRunForTask(TaskBase task, string logFolder, Guid taskListGuid)
        {
            TaskRun_Server run;
            lock (task.TaskLock)
            {
                run = new TaskRun_Server(task, logFolder, taskListGuid);

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

        private TaskRun_Server CreateTaskRunWithoutTask(string filePath, string arguments, string logFileOrFolder, TaskType type)
        {
            var run = new TaskRun_Server(filePath, arguments, logFileOrFolder, type);

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

        public TaskRun_Server GetTaskRunByGuid(Guid taskRunGuid)
        {
            if (_taskRunMap.ContainsKey(taskRunGuid))
            {
                return _taskRunMap[taskRunGuid];
            }
            else
            {
                var files = Directory.EnumerateFiles(_logFolder, $"*{taskRunGuid.ToString()}*", SearchOption.AllDirectories);
                if (files.Count() == 1)
                {
                    return LoadTaskRunFromFile(files.First());
                }
                else
                {
                    throw new FactoryOrchestratorUnkownGuidException(taskRunGuid, typeof(TaskRun));
                }
            }
        }

        /// <summary>
        /// Loads a taskrun log file into a taskrun_server object. This has a very hard dependency on the output format, defined by WriteLogHeader() and WriteLogFooter().
        /// </summary>
        /// <param name="filePath">log file to load</param>
        /// <returns>created taskrun object</returns>
        private TaskRun_Server LoadTaskRunFromFile(string filePath)
        {
            TaskRun_Server run = null;

            try
            {
                var lines = File.ReadAllLines(filePath).ToList();

                // At a minimum, the task header must have been written to the log file, check for the start of the output section first.
                // This will throw an exception if it doesn't exist.
                var startOutput = lines.First(x => x.StartsWith("---------------"));
                var taskRunGuid = new Guid(lines.First(x => x.StartsWith("TaskRun GUID:")).Split(' ').Last());

                // Get task guid if it exists
                Guid taskGuid;
                try
                {
                    taskGuid = new Guid(lines.First(x => x.StartsWith("Task GUID:")).Split(' ').Last());
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
                    throw new FactoryOrchestratorException($"TaskRun {run.Guid} could not be loaded from file as it already exists!", run.Guid);
                }

                run.LogFilePath = filePath;
                run.TaskType = (TaskType)Enum.Parse(typeof(TaskType), lines.First(x => x.StartsWith("Type:")).Substring(6));
                run.Arguments = lines.First(x => x.StartsWith("Arguments: ")).Substring(11);
                run.TaskPath = lines.First(x => x.StartsWith("Path: ")).Substring(6);

                try
                {
                    run.TimeStarted = DateTime.Parse(lines.Where(x => x.StartsWith("Date/Time run: ")).First().Replace("Date/Time run: ", ""));
                }
                catch (Exception)
                {
                    run.TimeStarted = null;
                }

                try
                {
                    run.TimeFinished = run.TimeStarted + TimeSpan.Parse(lines.First(x => x.StartsWith("Time to complete:")).Split(' ').Last());
                }
                catch (Exception)
                {
                    run.TimeFinished = null;
                }

                try
                {
                    run.TaskStatus = (TaskStatus)Enum.Parse(typeof(TaskStatus), lines.First(x => x.StartsWith("Result:")).Split(' ').Last());
                }
                catch (Exception)
                {
                    run.TaskStatus = TaskStatus.Unknown;
                }

                try
                {
                    run.ExitCode = int.Parse(lines.First(x => x.StartsWith("Exit code:")).Split(' ').Last());
                }
                catch (Exception)
                {
                    run.ExitCode = null;
                }

                try
                {
                    var endOutput = lines.Last(x => x.StartsWith("---------------"));
                    var startIndex = lines.IndexOf(startOutput);
                    var endIndex = lines.IndexOf(endOutput);
                    run.TaskOutput = lines.GetRange(startIndex + 1, endIndex - startIndex - 1);
                }
                catch
                {
                    run.TaskOutput = new List<string>();
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
                TaskRun_Server run;
                if (!_taskRunMap.TryRemove(oldGuid, out run))
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
            TaskRun_Server taskRun = null;
            lock (_taskMapLock)
            {
                if (_taskRunMap.TryGetValue(taskRunGuid, out taskRun))
                {
                    TaskRun_Server removed;
                    _taskRunMap.TryRemove(taskRunGuid, out removed);
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
                throw new ArgumentNullException();
            }

            lock (_taskMapLock)
            {
                if (!_taskRunMap.ContainsKey(updatedTaskRun.Guid))
                {
                    throw new FactoryOrchestratorUnkownGuidException(updatedTaskRun.Guid, typeof(TaskRun));
                }

                var run = _taskRunMap[updatedTaskRun.Guid];
                run.ExitCode = updatedTaskRun.ExitCode;
                run.TaskStatus = updatedTaskRun.TaskStatus;
                run.TimeFinished = updatedTaskRun.TimeFinished;
                run.TimeStarted = updatedTaskRun.TimeStarted;
                run.TaskOutput = updatedTaskRun.TaskOutput;
                run.UpdateOwningTaskFromTaskRun();
            }
        }

        public void RemoveTaskRunsForTask(Guid taskGuid, bool preserveLogs)
        {
            Parallel.ForEach(GetTaskRunGuidsByTaskGuid(taskGuid), taskRunGuid =>
            {
                RemoveTaskRun(taskRunGuid, preserveLogs);
            });
        }

        private List<TaskList> KnownTaskLists;
        private ConcurrentDictionary<Guid, CancellationTokenSource> RunningTaskListTokens;
        private ConcurrentDictionary<Guid, CancellationTokenSource> RunningTaskRunTokens;
        private ConcurrentDictionary<Guid, List<TaskRunner>> RunningBackgroundTasks;
        private readonly object KnownTaskListLock = new object();
        private readonly object RunningTaskListLock = new object();
        private readonly SemaphoreSlim _startNonParallelTaskRunLock;
        private string _logFolder;

        /// <summary>
        /// Tracks all the task runs that have ever occured, mapped by the task run GUID
        /// </summary>
        private static ConcurrentDictionary<Guid, TaskRun_Server> _taskRunMap;
        private static object _taskMapLock;


        public string LogFolder { get => _logFolder; }
        public string TaskListStateFile { get; private set; }

        /// <summary>
        /// True if a TaskList is actively running. Lock RunningTaskListLock first if doing a destructive operation.
        /// </summary>
        public bool IsTaskListRunning
        {
            get
            {
                return (RunningTaskListTokens.Count > 0);
            }
        }

        public bool CanStartNewTaskListRun
        {
            get
            {
                if (RunningTaskListTokens.Count == 0)
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
            base(eventType, eventStatusCode, (eventException != null) ? eventException.ToString() : null)
        {
            EventException = eventException;
        }

        public Exception EventException { get; }
    }

    public class TaskRunner
    {
        private static string _globalTeExePath = "";
        private readonly static string GlobalTeArgs = "";

        public static string GlobalTeExePath
        {
            get
            {
                if (string.IsNullOrEmpty(_globalTeExePath))
                {
                    // Default TAEF (TE.exe) path.
                    return Environment.ExpandEnvironmentVariables(@"%SystemDrive%\taef\te.exe");
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
                    throw new FileNotFoundException($"{value} is not a valid file path!");
                }
            }
        }

        static TaskRunner()
        {
            _taskRunnerMap = new ConcurrentDictionary<Guid, TaskRunner>();
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

        public TaskRunner(TaskRun_Server taskRun)
        {
            IsRunning = false;
            ActiveTaskRun = taskRun;
            BackgroundTask = taskRun.BackgroundTask;
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
                startInfo.FileName = "pwsh.exe";
                startInfo.Arguments += $"-NonInteractive -NoProfile -File \"{ActiveTaskRun.TaskPath}\" ";
            }
            else if (ActiveTaskRun.TaskType == TaskType.BatchFile)
            {
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments += $"/C \"{ActiveTaskRun.TaskPath}\" ";
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
            startInfo.Environment["Path"] = Environment.GetEnvironmentVariable("Path");

            if (ActiveTaskRun.TaskType == TaskType.TAEFDll)
            {
                startInfo.Environment["Path"] += ";" + Path.GetDirectoryName(ActiveTaskRun.TaskPath);
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
                TaskRunner removed;
                _taskRunnerMap.TryRemove(ActiveTaskRunGuid, out removed);
                ActiveTaskRun.TaskOutput.Add("Process failed to start!");
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
                    OnTestEvent?.Invoke(this, new TaskRunnerEventArgs(ActiveTaskRun.TaskStatus, ActiveTaskRun.ExitCode, "Process failed to start."));
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

                if (ActiveTaskRun.TaskType == TaskType.TAEFDll)
                {
                    ParseTeOutput(line);
                }
            }
        }

        private void ParseTeOutput(string data)
        {
            //throw new NotImplementedException();
        }

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
                ActiveTaskRun.ExitCode = -1;
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
            TaskRunner removed;
            _taskRunnerMap.TryRemove(ActiveTaskRunGuid, out removed);

            // Raise event if event handler exists
            OnTestEvent?.Invoke(this, new TaskRunnerEventArgs(ActiveTaskRun.TaskStatus, ActiveTaskRun.ExitCode, "Process exited."));
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
        public Guid ActiveTaskRunGuid {
            get
            {
                return ActiveTaskRun.Guid;
            }
        }

        private TaskRun_Server ActiveTaskRun;
        public event TaskRunnerEventHandler OnTestEvent;
        private Process TaskProcess;
        private bool TaskAborted = false;
        private bool TaskTimeout = false;
        private bool BackgroundTask;
        private CancellationTokenSource TimeoutToken = new CancellationTokenSource();
        private Timer outputTimer = null;

        /// <summary>
        /// Lock to maintain consistent state of execution status (Run, Abort etc)
        /// </summary>
        private object runnerStateLock = new object();
        /// <summary>
        /// Lock to maintain serial ordering of task stdout & stderr
        /// </summary>
        private object outputLock = new object();

        private static ConcurrentDictionary<Guid, TaskRunner> _taskRunnerMap;
    }

    /// <summary>
    /// Server side only TaskRun class.
    /// </summary>
    public class TaskRun_Server : TaskRun
    {
        // TODO: Move taskrunner into taskrun_server

        // TODO: Quality: The taskrun_server CTORS are convoluted and could be simplified.

        public TaskRun_Server(TaskBase owningTask, string logFolder, Guid TaskListGuid) : this(owningTask, logFolder)
        {
            OwningTaskListGuid = TaskListGuid;
        }

        public TaskRun_Server(TaskBase owningTask, string logFolder) : base(owningTask)
        {
            CtorCommon();
            OwningTaskListGuid = null;
            OwningTask = owningTask;

            // Setup log path
            SetLogFile(logFolder);
        }

        /// <summary>
        /// This is used as a copy constructor. Don't add it to the map, it should be there already.
        /// </summary>
        /// <param name="taskRun"></param>
        /// <param name="owningTask"></param>
        private TaskRun_Server(TaskRun taskRun, TaskBase owningTask) : base(owningTask)
        {
            LogFilePath = taskRun.LogFilePath;
            TaskStatus = taskRun.TaskStatus;
            TimeFinished = taskRun.TimeFinished;
            TimeStarted = taskRun.TimeStarted;
            ExitCode = taskRun.ExitCode;
            TaskOutput = taskRun.TaskOutput;
            OwningTaskListGuid = taskRun.OwningTaskGuid;
            OwningTask = owningTask;
        }

        /// <summary>
        /// Private Ctor used for task runs not backed by a TaskBase object
        /// </summary>
        /// <param name="taskPath"></param>
        /// <param name="arguments"></param>
        /// <param name="logFileOrPath"></param>
        public TaskRun_Server(string taskPath, string arguments, string logFileOrPath, TaskType type) : base(null)
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
                    LogFilePath = Path.Combine(logFileOrPath, String.Format("{0}_Run{1}.log", TaskName, Guid));
                }
            }
        }

        private void CtorCommon()
        {
            // If an executable, find the actual path to the file.
            if ((TaskType == TaskType.ConsoleExe) || (TaskType == TaskType.BatchFile))
            {
                TaskPath = FindFileInPath(TaskPath);
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
            if (waitForResult)
            {
                TaskOutput.Add($"A FactoryOrchestratorClient completed the TaskRun with Status: {TaskStatus}, Exit code: {ExitCode}");
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
                        header.Add(String.Format("Task: {0}", TaskName));
                    }
                    else
                    {
                        header.Add(String.Format("Background Task: {0}", TaskName));
                    }

                    header.Add(String.Format("Task GUID: {0}", (OwningTaskGuid == null) ? "Not a known Task" : OwningTaskGuid.ToString()));
                    header.Add(String.Format("TaskRun GUID: {0}", Guid));
                    header.Add(String.Format("Type: {0}", TaskType));
                    header.Add(String.Format("Path: {0}", TaskPath));
                    header.Add(String.Format("Arguments: {0}", Arguments));
                    header.Add(String.Format("Date/Time run: {0}", (TimeStarted == null) ? "Never Started" : TimeStarted.ToString()));
                    header.Add(String.Format("--------------- Output --------------"));

                    File.WriteAllLines(LogFilePath, header);
                }
                catch (IOException e)
                {
                    // todo: logging
                    TaskOutput.Insert(0, $"WARNING: Log File {LogFilePath} could not be created:{Environment.NewLine}{e.AllExceptionsToString()}");
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
                        TaskOutput.Insert(0, $"WARNING: Log File {LogFilePath} could not be created:{Environment.NewLine}{e.AllExceptionsToString()}");
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
                    var footer = new List<string>();
                    // End output section
                    footer.Add("-------------------------------------");
                    footer.Add($"Result: {TaskStatus}");
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
                    TaskOutput.Insert(0, $"WARNING: Log File {LogFilePath} could not be created:{Environment.NewLine}{e.AllExceptionsToString()}");
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
                throw new ArgumentNullException();
            }

            file = Environment.ExpandEnvironmentVariables(file);

            if (!File.Exists(file))
            {
                if (Path.GetDirectoryName(file) == String.Empty)
                {
                    foreach (string testPath in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'))
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