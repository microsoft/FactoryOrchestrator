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
using System.Xml;
using System.Xml.Serialization;
using TaskStatus = Microsoft.FactoryOrchestrator.Core.TaskStatus;

namespace Microsoft.FactoryOrchestrator.Server
{

    public static class Exception_ServerExtensions
    {
        public static string AllExceptionsToString(this Exception ex)
        {
            string ret = "";
            var exc = ex;
            while (exc != null)
            {
                if (ret.Length > 0)
                {
                    ret += " -> ";
                }
                if (exc.Message != null)
                {
                    ret += $"{exc.GetType().ToString()}:{exc.Message}";
                }
                else
                {
                    ret += $"{exc.GetType().ToString()}";
                }

                exc = exc.InnerException;
            }

             return ret;
        }
    }

    public static class TaskBase_ServerExtensions
    {
        public static Guid CreateTaskRun(this TaskBase task, string defaultLogFolder)
        {
            TaskRun_Server run;
            lock (task.TaskLock)
            {
                run = new TaskRun_Server(task, defaultLogFolder);
                task.TaskRunGuids.Add(run.Guid);
                task.LatestTaskRunExitCode = null;
                task.LatestTaskRunStatus = TaskStatus.RunPending;
                task.LatestTaskRunTimeFinished = null;
                task.LatestTaskRunTimeStarted = null;
            }
            return run.Guid;
        }

        public static Guid CreateTaskRun(this TaskBase task, string defaultLogFolder, Guid taskListGuid)
        {
            TaskRun_Server run;
            lock (task.TaskLock)
            {
                run = new TaskRun_Server(task, defaultLogFolder, taskListGuid);
                task.TaskRunGuids.Add(run.Guid);
                task.LatestTaskRunExitCode = null;
                task.LatestTaskRunStatus = TaskStatus.RunPending;
                task.LatestTaskRunTimeFinished = null;
                task.LatestTaskRunTimeStarted = null;
            }
            return run.Guid;
        }


        public static void GetLatestTaskRun(this TaskBase task)
        {
            TaskRun_Server.GetTaskRunByGuid(task.TaskRunGuids.Last());
        }

        public static void Reset(this TaskBase task, bool preserveLogs = true)
        {
            lock (task.TaskLock)
            {
                task.LatestTaskRunStatus = TaskStatus.NotRun;
                task.LatestTaskRunExitCode = null;
                task.LatestTaskRunTimeFinished = null;
                task.LatestTaskRunTimeStarted = null;
                task.TaskRunGuids = new List<Guid>();
            }

            TaskRun_Server.RemoveTaskRunsForTask(task.Guid, preserveLogs);
        }
    }

    public class TaskManager_Server
    {
        public TaskManager_Server(string defaultLogFolder)
        {
            _startNonParallelTaskRunLock = new SemaphoreSlim(1, 1);
            KnownTaskLists = new ConcurrentDictionary<Guid, TaskList>();
            RunningTaskListTokens = new ConcurrentDictionary<Guid, CancellationTokenSource>();
            RunningBackgroundTasks = new ConcurrentDictionary<Guid, List<TaskRunner>>();
            RunningTaskRunTokens = new ConcurrentDictionary<Guid, CancellationTokenSource>();
            TaskListStateFile = Path.Combine(defaultLogFolder, "FactoryOrchestratorKnownTaskLists.xml");
            SetDefaultLogFolder(defaultLogFolder, false);
        }

        public bool SetDefaultLogFolder(string newLogFolder, bool moveFiles)
        {
            if (newLogFolder == _defaultLogFolder)
            {
                return true;
            }

            try
            {
                // Don't allow the folder to move if any task is running or we are actively modifying tasklists
                lock (RunningTaskListLock)
                {
                    if (IsTaskListRunning)
                    {
                        throw new TestManagerTaskListRunningException();
                    }

                    lock (KnownTaskListLock)
                    {
                        if (moveFiles && (_defaultLogFolder != null) && (Directory.Exists(_defaultLogFolder)))
                        {
                            // Move existing folder to temp folder
                            var tempDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), "FOTemp");
                            CopyDirectory(_defaultLogFolder, tempDir, true);

                            // Delete old folder
                            Directory.Delete(_defaultLogFolder, true);

                            // Move temp folder to new folder
                            CopyDirectory(tempDir, newLogFolder, true);

                            // Delete temp folder
                            Directory.Delete(tempDir, true);
                        }
                        else
                        {
                            Directory.CreateDirectory(newLogFolder);
                        }

                        var newStateFilePath = Path.Combine(newLogFolder, "FactoryOrchestratorKnownTaskLists.xml");

                        if (!moveFiles)
                        {
                            // Move state file if needed
                            if (File.Exists(TaskListStateFile))
                            {
                                File.Move(TaskListStateFile, newStateFilePath);
                            }
                        }

                        // Update paths
                        _defaultLogFolder = newLogFolder;
                        TaskListStateFile = newStateFilePath;
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                throw new TestManagerException("Could not move log folder!", null, e);
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
        
        /// <summary>
        /// Loads a taskrun log file into a taskrun_server object. This has a very hard dependency on the output format.
        /// </summary>
        /// <param name="filePath">log file to load</param>
        /// <returns>created taskrun object</returns>
        public TaskRun_Server LoadTaskRunFromFile(string filePath)
        {
            TaskRun_Server run = null;

            try
            {
                Guid? taskGuid = null;
                Guid? taskRunGuid = null;
                var lines = File.ReadAllLines(filePath).ToList();
                taskGuid = new Guid(lines.First(x => x.StartsWith("GUID:")).Split(' ').Last());
                taskRunGuid = new Guid(lines.First(x => x.StartsWith("TaskRun GUID:")).Split(' ').Last());


                if ((taskGuid != null) && (taskRunGuid != null))
                {
                    var task = GetTask((Guid)taskGuid);
                    var list = KnownTaskLists.Values.Where(x => x.Tasks.ContainsKey((Guid)taskGuid)).First();
                    run = new TaskRun_Server(task, DefaultLogFolder, list.Guid, (Guid)taskRunGuid);
                    run.TimeStarted = DateTime.Parse(lines.Where(x => x.StartsWith("Date/Time run:")).First().Replace("Date/Time run:", ""));
                    run.TimeFinished = run.TimeStarted + TimeSpan.Parse(lines.First(x => x.StartsWith("Time to complete:")).Split(' ').Last());
                    var status = TaskStatus.Unknown;
                    Enum.TryParse(lines.First(x => x.StartsWith("Result:")).Split(' ').Last(), out status);
                    run.TaskStatus = status;
                    run.ExitCode = int.Parse(lines.First(x => x.StartsWith("Exit code:")).Split(' ').Last());
                    var startOutput = lines.First(x => x.StartsWith("---------------"));
                    var endOutput = lines.Last(x => x.StartsWith("---------------"));
                    var startIndex = lines.IndexOf(startOutput);
                    var endIndex = lines.IndexOf(endOutput);
                    run.TaskOutput = lines.GetRange(startIndex + 1, endIndex - startIndex - 1);
                }
            }
            catch (Exception)
            {
                if (run != null)
                {
                    TaskRun_Server.RemoveTaskRun(run.Guid, true);
                    run = null;
                }
            }

            return run;
        }

        public TaskList CreateTaskListFromDirectory(String path, bool onlyTAEF)
        {
            // Recursive search for all executable files
            var exes = Directory.EnumerateFiles(path, "*.exe", SearchOption.AllDirectories);
            var dlls = Directory.EnumerateFiles(path, "*.dll", SearchOption.AllDirectories);
            var bats = Directory.EnumerateFiles(path, "*.bat", SearchOption.AllDirectories);
            var cmds = Directory.EnumerateFiles(path, "*.cmd", SearchOption.AllDirectories);
            var ps1s = Directory.EnumerateFiles(path, "*.ps1", SearchOption.AllDirectories);
            TaskList tests = new TaskList(path, Guid.NewGuid());

            Parallel.ForEach<string>(dlls, (dll) =>
            {
                try
                {
                    var maybeTAEF = CheckForTAEFTest(dll);
                    if (maybeTAEF != null)
                    {
                        tests.Tasks.Add(maybeTAEF.Guid, maybeTAEF);
                    }
                }
                catch (Exception)
                {
                    // TODO: Logging
                }
            });

            // Assume every .exe is a valid console task
            if (!onlyTAEF)
            {
                foreach (var exe in exes)
                {
                    var task = new ExecutableTask(exe);
                    tests.Tasks.Add(task.Guid, task);
                }

                foreach (var cmd in cmds)
                {
                    var task = new BatchFileTask(cmd);
                    tests.Tasks.Add(task.Guid, task);
                }

                foreach (var bat in bats)
                {
                    var task = new BatchFileTask(bat);
                    tests.Tasks.Add(task.Guid, task);
                }

                foreach (var ps1 in ps1s)
                {
                    var task = new PowerShellTask(ps1);
                    tests.Tasks.Add(task.Guid, task);
                }
            }

            lock (KnownTaskListLock)
            {
                KnownTaskLists.TryAdd(tests.Guid, tests);
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
                    xml.TaskLists = KnownTaskLists.Values.ToList();
                }
                else
                {
                    return false;
                }

                if (!xml.Save(filename))
                {
                    throw new TestManagerException($"Could not save TaskLists to {filename}!");
                }
            }

            return true;
        }

        public bool SaveTaskListToXmlFile(Guid guid, string filename)
        {
            if (IsTaskListRunning)
            {
                throw new TestManagerTaskListRunningException();
            }
            
            FactoryOrchestratorXML xml = new FactoryOrchestratorXML();

            if (KnownTaskLists.ContainsKey(guid))
            {
                xml.TaskLists.Add(KnownTaskLists[guid]);
            }
            else
            {
                // TODO: Logging
                return false;
            }

            return xml.Save(filename);
        }

        public List<Guid> LoadTaskListsFromXmlFile(string filename)
        {
            if (IsTaskListRunning)
            {
                throw new TestManagerTaskListRunningException();
            }

            FactoryOrchestratorXML xml;
            xml = FactoryOrchestratorXML.Load(filename);

            // Add GUIDs to any TaskBase or TaskList objects that don't have one
            lock (KnownTaskListLock)
            {
                foreach (var list in xml.TaskLists)
                {
                    // Update "running" tests, as their state is unknown
                    list.Tasks.Values.Where(x => x.LatestTaskRunStatus == TaskStatus.Running).Select(x => x.LatestTaskRunStatus = TaskStatus.Unknown);
                    list.Tasks.Values.Where(x => x.LatestTaskRunStatus == TaskStatus.WaitingForExternalResult).Select(x => x.LatestTaskRunStatus = TaskStatus.Unknown);

                    if (KnownTaskLists.ContainsKey(list.Guid))
                    {
                        // Overwrite existing tasklist
                        KnownTaskLists[list.Guid] = list;

                        // todo: logging
                    }
                    else
                    {
                        KnownTaskLists.TryAdd(list.Guid, list);
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

        public bool DeleteTaskList(Guid listToDelete)
        {
            bool removed = false;
            lock (KnownTaskListLock)
            {
                // Abort() gracefully returns if the guid is invalid
                AbortTaskList(listToDelete);
                TaskList removedList;
                removed = KnownTaskLists.TryRemove(listToDelete, out removedList);   
            }

            // Update XML for state tracking
            SaveAllTaskListsToXmlFile(TaskListStateFile);

            return removed;
        }

        public TaskBase GetTask(Guid testGuid)
        {
            var list = KnownTaskLists.Values.Where(x => x.Tasks.ContainsKey(testGuid)).First();
            if (list == null)
            {
                return null;
            }

            TaskBase task;
            if (list.Tasks.TryGetValue(testGuid, out task))
            {
                return task;
            }
            else
            {
                return null;
            }
        }

        public TaskList GetTaskList(Guid guid)
        {
            TaskList list = null;
            if (KnownTaskLists.TryGetValue(guid, out list))
            {
                return list;
            }
            else
            {
                return null;
            }
        }

        public List<Guid> GetTaskListGuids()
        {
            return KnownTaskLists.Keys.ToList();
        }

        public TaskList CreateTaskListFromTaskList(TaskList taskList)
        {
            lock (KnownTaskListLock)
            {
                try
                {
                    KnownTaskLists.TryAdd(taskList.Guid, taskList);
                }
                catch (ArgumentException)
                {
                    // list already exists
                    return KnownTaskLists[taskList.Guid];
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
                        Parallel.ForEach(RunningBackgroundTasks.Values.SelectMany(x => x), (bgRunner) =>
                        {
                            bgRunner.StopTask();
                        });

                        RunningBackgroundTasks.Clear();
                    }

                    // Reset all tests
                    foreach (var task in KnownTaskLists.Values.SelectMany(x => x.Tasks.Values))
                    {
                        task.Reset(preserveLogs);
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
            TaskRun_Server taskRun = new TaskRun_Server(maybeTAEF, DefaultLogFolder);
            TaskRunner runner = new TaskRunner(taskRun);
            bool isTaef = false;
            try
            {
                if (!runner.CheckIfTaefTest())
                {
                    throw new Exception(String.Format("Unable to invoke TE.exe to validate possible TAEF test: {0}", dllToTest));
                }
                // Timeout after a second
                if (!runner.WaitForExit(1000))
                {
                    runner.StopTask();
                    throw new Exception(String.Format("TE.exe timed out trying to validate possible TAEF test: {0}", dllToTest));
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
                    throw new Exception(String.Format("TE.exe returned error {0} when trying to validate possible TAEF test: {1}", taskRun.ExitCode, dllToTest));
                }
            }
            catch (Exception e)
            {
                maybeTAEF.Reset(false);
                throw new Exception(String.Format("Unable to validate possible TAEF test: {0}", dllToTest), e);
            }

            // Cleanup task run
            maybeTAEF.Reset(false);

            if (isTaef)
            {
                return maybeTAEF;
            }
            else
            {
                return null;
            }
        }

        public bool UpdateTaskList(TaskList taskList)
        {
            if (taskList == null)
            {
                return false;
            }

            lock (KnownTaskListLock)
            {
                if (KnownTaskLists.ContainsKey(taskList.Guid))
                {
                    lock (RunningTaskListLock)
                    {
                        // if it is running don't update the tasklist
                        if (RunningTaskListTokens.Keys.Contains(taskList.Guid))
                        {
                            return false;
                        }
                        else
                        {
                            KnownTaskLists[taskList.Guid] = taskList;
                            return true;
                        }
                    }
                }
            }

            // Update XML for state tracking
            SaveAllTaskListsToXmlFile(TaskListStateFile);

            return false;
        }

        public bool UpdateTask(TaskBase updatedTest)
        {
            if (updatedTest == null)
            {
                return false;
            }

            // Changing state, lock the task lists
            lock (KnownTaskListLock)
            {
                // Find list this task in it
                var taskList = KnownTaskLists.Values.Where(x => x.Tasks.ContainsKey(updatedTest.Guid)).DefaultIfEmpty(null).First();

                if (taskList == null)
                {
                    return false;
                }
                else
                {
                    lock (RunningTaskListLock)
                    {
                        // if it is running don't update the task
                        if (RunningTaskListTokens.Keys.Contains(taskList.Guid))
                        {
                            return false;
                        }
                        else
                        {
                            taskList.Tasks[updatedTest.Guid] = updatedTest;
                        }
                    }
                }
            }

            // Update XML for state tracking
            SaveAllTaskListsToXmlFile(TaskListStateFile);

            return true;
        }

        public bool UpdateTaskRun(TaskRun latestTaskRun)
        {
            if (latestTaskRun == null)
            {
                return false;
            }

            return TaskRun_Server.UpdateTaskRun(latestTaskRun);
        }

        public bool RunTaskList(Guid TaskListGuidToRun, int startIndex = 0)
        {
            TaskList list = null;

            // Check if task list is valid
            if (!KnownTaskLists.TryGetValue(TaskListGuidToRun, out list))
            {
                return false;
            }

            // Check if task list is already running or set to be run
            lock (RunningTaskListLock)
            {
                if (RunningTaskListTokens.ContainsKey(TaskListGuidToRun))
                {
                    return false;
                }

                // Create testrun for all background tasks in the list
                List<Guid> backgroundTaskRunGuids = new List<Guid>();
                foreach (var task in list.BackgroundTasks.Values)
                {
                    backgroundTaskRunGuids.Add(task.CreateTaskRun(DefaultLogFolder, TaskListGuidToRun));
                }

                // Create taskrun for all tasks in the list
                List<Guid> taskRunGuids = new List<Guid>();
                var tasks = list.Tasks.ToList();
                for (int i = startIndex; i < tasks.Count; i++)
                {
                    tasks[i].Value.TimesRetried = 0;
                    taskRunGuids.Add(tasks[i].Value.CreateTaskRun(DefaultLogFolder, TaskListGuidToRun));
                }

                // Update XML for state tracking.
                SaveAllTaskListsToXmlFile(TaskListStateFile);

                var workItem = new TaskListWorkItem(list.Guid, backgroundTaskRunGuids, taskRunGuids, list.TerminateBackgroundTasksOnCompletion, list.AllowOtherTaskListsToRun, list.RunInParallel);
                QueueTaskListWorkItem(workItem);
            }

            return true;
        }

        public bool RunTaskListFromInitial(Guid taskListToRun, int initialTask)
        {
            return RunTaskList(taskListToRun, initialTask);
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

        private void TaskListWorker(object i, CancellationToken token)
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
                    return;
                }

                usedSem = true;
            }
            else
            {
                usedSem = _startNonParallelTaskRunLock.Wait(0);
            }

            if (!token.IsCancellationRequested)
            {
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
                }
            }

            if (token.IsCancellationRequested)
            {
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
                var taskRun = TaskRun_Server.GetTaskRunByGuid(bgRunGuid);
                var testToken = new CancellationTokenSource();
                StartTest(taskRun, token, taskRunEventHandler, true);
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
                    var taskRun = TaskRun_Server.GetTaskRunByGuid(runGuid);

                    if (token.IsCancellationRequested)
                    {
                        taskRun.TaskStatus = TaskStatus.Aborted;
                        taskRun.OwningTask.LatestTaskRunStatus = TaskStatus.Aborted;
                    }
                    else
                    {
                        var testToken = new CancellationTokenSource();
                        RunningTaskRunTokens.TryAdd(taskRun.Guid, testToken);
                        StartTest(taskRun, testToken.Token, taskRunEventHandler, false);

                        // Update saved state
                        SaveAllTaskListsToXmlFile(TaskListStateFile);
                    }
                }
            }
            else
            {
                Parallel.ForEach(taskRunGuids, (runGuid) =>
                {
                    var taskRun = TaskRun_Server.GetTaskRunByGuid(runGuid);

                    if (token.IsCancellationRequested)
                    {
                        taskRun.TaskStatus = TaskStatus.Aborted;
                        taskRun.OwningTask.LatestTaskRunStatus = TaskStatus.Aborted;
                    }
                    else
                    {
                        var testToken = new CancellationTokenSource();
                        RunningTaskRunTokens.TryAdd(taskRun.Guid, testToken);
                        StartTest(taskRun, token, taskRunEventHandler);

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

        private void StartTest(TaskRun_Server taskRun, CancellationToken token, TaskRunnerEventHandler taskRunEventHandler = null, bool backgroundTask = false)
        {
            if (!token.IsCancellationRequested)
            {
                TaskRunner runner = null;

                if (taskRun.RunByServer)
                {
                    runner = new TaskRunner(taskRun);

                    if (taskRunEventHandler != null)
                    {
                        runner.OnTestEvent += taskRunEventHandler;
                    }

                    if (!runner.RunTask())
                    {
                        throw new Exception(String.Format("Unable to start TaskRun {0} for task {1}", taskRun.Guid, taskRun.TaskName));
                    }
                }
                else
                {
                    // TODO: Logging : Log External tests to file
                    taskRun.StartWaitingForExternalResult();
                    OnTestManagerEvent?.Invoke(this, new TestManagerEventArgs(TestManagerEventType.WaitingForExternalTaskRunResult, taskRun.Guid));
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
                        runner.StopTask();
                    }
                    else
                    {
                        taskRun.TaskStatus = TaskStatus.Aborted;
                        taskRun.ExitCode = -1;
                    }
                }

                if (taskRun.RunByClient)
                {
                    taskRun.EndWaitingForExternalResult();
                    if (token.IsCancellationRequested)
                    {
                        OnTestManagerEvent?.Invoke(this, new TestManagerEventArgs(TestManagerEventType.ExternalTaskRunAborted, taskRun.Guid));
                    }
                    else
                    {
                        OnTestManagerEvent?.Invoke(this, new TestManagerEventArgs(TestManagerEventType.ExternalTaskRunFinished, taskRun.Guid));
                    }
                }

                if ((!token.IsCancellationRequested) && (taskRun.OwningTask != null) && (taskRun.TaskStatus != TaskStatus.Passed) && (!backgroundTask))
                {
                    if (taskRun.OwningTask.TimesRetried < taskRun.OwningTask.MaxNumberOfRetries)
                    {
                        var newRunGuid = taskRun.OwningTask.CreateTaskRun(DefaultLogFolder);
                        var newRun = TaskRun_Server.GetTaskRunByGuid(newRunGuid);
                        taskRun.OwningTask.TimesRetried++;
                        StartTest(newRun, token, taskRunEventHandler);
                    }
                    else if (taskRun.OwningTask.AbortTaskListOnFailed)
                    {
                        AbortTaskList(taskRun.OwningTaskListGuid);
                    }
                }

            }
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

                    var taskRunGuids = KnownTaskLists[taskListToCancel].Tasks.Values.Select(x => x.LatestTaskRunGuid);
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
            var run = TaskRun_Server.CreateTaskRunWithoutTask(exeFilePath, arguments, logFilePath, TaskType.ConsoleExe);
            run.BackgroundTask = true;
            var token = new CancellationTokenSource();
            StartTest(run, token.Token);

            RunningBackgroundTasks.TryAdd(run.Guid, new List<TaskRunner>(1) { run.GetOwningTaskRunner() });

            return run;
        }

        public TaskRun RetryTask(Guid taskGuid)
        {
            var list = KnownTaskLists.Values.Where(x => x.Tasks.ContainsKey(taskGuid)).DefaultIfEmpty(null).First();

            if (list == null)
            {
                // TODO: Logging: log error
                return null;
            }

            // If list is running already, fail
            CancellationTokenSource t;
            if (RunningTaskListTokens.TryGetValue(list.Guid, out t))
            {
                return null;
            }

            var task = list.Tasks[taskGuid];
            lock (task.TaskLock)
            {
                task.TimesRetried++;
            }

            var runGuid = task.CreateTaskRun(DefaultLogFolder, list.Guid);
            var run = TaskRun_Server.GetTaskRunByGuid(runGuid);
            var runList = new List<Guid>();
            runList.Add(runGuid);
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

        private void QueueTaskListWorkItem(TaskListWorkItem workItem)
        {
            var token = new CancellationTokenSource();
            RunningTaskListTokens.TryAdd(workItem.TaskListGuid, token);
            RunningBackgroundTasks.TryAdd(workItem.TaskListGuid, new List<TaskRunner>());
            Task t = new Task((i) => { TaskListWorker(i, token.Token); }, workItem, token.Token);
            t.Start();
        }

        public TaskRun RunApp(string packageFamilyName)
        {
            var run = TaskRun_Server.CreateTaskRunWithoutTask(packageFamilyName, null, null, TaskType.UWP);
            var token = new CancellationTokenSource();
            RunningTaskRunTokens.TryAdd(run.Guid, token);
            Task t = new Task(() => { StartTest(run, token.Token); });
            t.Start();

            return run;
        }

        private ConcurrentDictionary<Guid, TaskList> KnownTaskLists;
        private ConcurrentDictionary<Guid, CancellationTokenSource> RunningTaskListTokens;
        private ConcurrentDictionary<Guid, CancellationTokenSource> RunningTaskRunTokens;
        private ConcurrentDictionary<Guid, List<TaskRunner>> RunningBackgroundTasks;
        private readonly object KnownTaskListLock = new object();
        private readonly object RunningTaskListLock = new object();
        private readonly SemaphoreSlim _startNonParallelTaskRunLock;
        private string _defaultLogFolder;

        public string DefaultLogFolder { get => _defaultLogFolder; }
        public string TaskListStateFile { get; private set; }
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
                    return !KnownTaskLists.Values.Any(x => (x.TaskListStatus == TaskStatus.Running) && (x.AllowOtherTaskListsToRun == false));
                }
            }
        }

        public event TestManagerEventHandler OnTestManagerEvent;
    }

    public class TestManagerException : Exception
    {
        public TestManagerException(string message = null, Guid? guid = null, Exception innerException = null) : base(message, innerException)
        {
            Guid = guid;
        }

        public Guid? Guid { get; }
    }

    public class TestManagerTaskListRunningException : TestManagerException
    {
        public TestManagerTaskListRunningException() : base("Cannot perform operation because one or more TaskLists are actively running!")
        { }

        public TestManagerTaskListRunningException(Guid guid) : base($"Cannot perform operation because TaskList {guid} is actively running!", guid)
        { }
    }

    public delegate void TestManagerEventHandler(object source, TestManagerEventArgs e);

    public enum TestManagerEventType
    {
        NewTaskList,
        UpdatedTaskList,
        DeletedTaskList,
        TaskListRunStarted,
        TaskListRunEnded,
        WaitingForExternalTaskRunResult,
        ExternalTaskRunFinished,
        ExternalTaskRunAborted,
        ExternalTaskRunTimeout,
        StandaloneTestStarted,
        StandaloneTestFinished
    }

    public class TestManagerEventArgs : EventArgs
    {
        public TestManagerEventArgs(TestManagerEventType eventType, Guid? guid)
        {
            Event = eventType;
            Guid = guid;
        }

        public TestManagerEventType Event { get; }
        public Guid? Guid { get; }
    }

    public delegate void TaskRunnerEventHandler(object source, TaskRunnerEventArgs e);

    public class TaskRunnerEventArgs : EventArgs
    {
        public TaskRunnerEventArgs(TaskStatus testStatus, int eventStatusCode, String eventMessage)
        {
            TestStatus = testStatus;
            EventStatusCode = eventStatusCode;
            EventMessage = eventMessage;
        }

        public TaskStatus TestStatus { get; }
        public int EventStatusCode { get; }
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
        private static string _globalTeExePath = "c:\\taef\\te.exe";
        private readonly static string GlobalTeArgs = "";

        public static string GlobalTeExePath
        {
            get
            {
                return _globalTeExePath;
            }
            set
            {
                if (File.Exists(value))
                {
                    _globalTeExePath = value;
                }
                else
                {
                    throw new Exception($"{value} is not a valid file path!");
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

        public bool RunTask()
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
                startInfo.Arguments += $"-f \"{ActiveTaskRun.TaskPath}\" ";
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
            if (TaskProcess.Start())
            {
                IsRunning = true;
                ActiveTaskRun.TaskStatus = TaskStatus.Running;
                ActiveTaskRun.TimeStarted = DateTime.Now;

                // Start async read (OnOutputData, OnErrorData)
                TaskProcess.BeginErrorReadLine();
                TaskProcess.BeginOutputReadLine();
                // Start timeout timer if needed
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
            else
            {
                ActiveTaskRun.TaskStatus = TaskStatus.Failed;
                TaskRunner removed;
                _taskRunnerMap.TryRemove(ActiveTaskRunGuid, out removed);
                // TODO: log error
            }

            ActiveTaskRun.UpdateOwningTaskFromTaskRun();

            // Write header to file
            // WARNING: Update LoadTaskRunFromFile() if you change the header format!
            var LogFilePath = ActiveTaskRun.LogFilePath;
            if (LogFilePath != null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath));
                List<string> header = new List<string>();

                if (!BackgroundTask)
                {
                    header.Add(String.Format("Task: {0}", ActiveTaskRun.TaskName));
                }
                else
                {
                    header.Add(String.Format("Background Task: {0}", ActiveTaskRun.TaskName));
                }

                header.Add(String.Format("GUID: {0}", (ActiveTaskRun.OwningTaskGuid == null) ? "Not a known Task" : ActiveTaskRun.OwningTaskGuid.ToString()));
                header.Add(String.Format("TaskRun GUID: {0}", ActiveTaskRun.Guid));
                header.Add(String.Format("Type: {0}", ActiveTaskRun.TaskType));
                header.Add(String.Format("Path: {0}", ActiveTaskRun.TaskPath));
                header.Add(String.Format("Arguments: {0}", ActiveTaskRun.Arguments));
                header.Add(String.Format("Date/Time run: {0}", (ActiveTaskRun.TimeStarted == null) ? "Never Started" : ActiveTaskRun.TimeStarted.ToString()));
                if (IsRunning)
                {
                    header.Add(String.Format("--------------- Console Output --------------"));
                }
                else
                {
                    header.Add("---------------------------------------------");
                    header.Add("Process failed to start!");
                    header.Add("---------------------------------------------");
                    header.Add(String.Format("Result: {0}", ActiveTaskRun.TaskStatus));
                    header.Add(String.Format("Exit code: {0}", ActiveTaskRun.ExitCode));
                }
                File.WriteAllLines(LogFilePath, header);

                nextIndexToSave = 0;
                outputTimer = new Timer(OnOutputTimer, null, 5000, 5000);
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
                    if (nextIndexToSave != ActiveTaskRun.TaskOutput.Count)
                    {
                        File.AppendAllLines(logFilePath, ActiveTaskRun.TaskOutput.GetRange(nextIndexToSave, ActiveTaskRun.TaskOutput.Count - nextIndexToSave));
                        nextIndexToSave = ActiveTaskRun.TaskOutput.Count;
                    }
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
            if (!TaskAborted)
            {
                ActiveTaskRun.TimeFinished = DateTime.Now;
                ActiveTaskRun.ExitCode = TaskProcess.ExitCode;
                ActiveTaskRun.TaskStatus = (ActiveTaskRun.ExitCode == 0) ? TaskStatus.Passed : TaskStatus.Failed;
            }
            else
            {
                ActiveTaskRun.ExitCode = -1;
                ActiveTaskRun.TaskStatus = TaskTimeout ? TaskStatus.Timeout : TaskStatus.Aborted;
            }
            ActiveTaskRun.UpdateOwningTaskFromTaskRun();

            // Save task output to file
            // Wait for all output to complete
            TaskProcess.WaitForExit();

            lock (outputLock)
            {
                var LogFilePath = ActiveTaskRun.LogFilePath;
                if (LogFilePath != null)
                {
                    if (nextIndexToSave != ActiveTaskRun.TaskOutput.Count)
                    {
                        File.AppendAllLines(ActiveTaskRun.LogFilePath, ActiveTaskRun.TaskOutput.GetRange(nextIndexToSave, ActiveTaskRun.TaskOutput.Count - nextIndexToSave));
                        nextIndexToSave = ActiveTaskRun.TaskOutput.Count;
                    }
                    // WARNING: Update LoadTaskRunFromFile() if you change the footer format!
                    File.AppendAllLines(LogFilePath,
                                        new String[] {
                                    "---------------------------------------------",
                                    String.Format("Result: {0}", ActiveTaskRun.TaskStatus),
                                    String.Format("Exit code: {0}", ActiveTaskRun.ExitCode),
                                    String.Format("Time to complete: {0}", (ActiveTaskRun.RunTime == null) ? "" : ActiveTaskRun.RunTime.ToString())
                                        });
                }
            }

            IsRunning = false;
            TaskRunner removed;
            _taskRunnerMap.TryRemove(ActiveTaskRunGuid, out removed);

            // Raise event if event handler exists
            OnTestEvent?.Invoke(this, new TaskRunnerEventArgs(ActiveTaskRun.TaskStatus, (int)ActiveTaskRun.ExitCode, null));
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
                // See if you get lucky and the task finishes on its own???
                if (!TaskProcess.WaitForExit(500))
                {
                    TaskAborted = true;
                    TaskProcess.Kill();
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

            return true;
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
        private int nextIndexToSave = 0;

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
        static TaskRun_Server()
        {
            _taskRunMap = new ConcurrentDictionary<Guid, TaskRun_Server>();
            _taskMapLock = new object();
        }

        public static List<Guid> GetTaskRunGuidsByTaskGuid(Guid taskGuid)
        {
            return _taskRunMap.Values.Where(x => x.OwningTaskGuid == taskGuid).Select(x => x.Guid).ToList();
        }

        public static TaskRun_Server GetTaskRunByGuid(Guid taskRunGuid)
        {
            if (_taskRunMap.ContainsKey(taskRunGuid))
            {
                return _taskRunMap[taskRunGuid];
            }
            else
            {
                return null;
            }
        }

        public static TaskRun_Server CreateTaskRunWithoutTask(string filePath, string arguments, string logFileOrFolder, TaskType type)
        {
            return new TaskRun_Server(filePath, arguments, logFileOrFolder, type);
        }

        public static void RemoveTaskRun(Guid taskRunGuid, bool preserveLogs)
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

        public static bool UpdateTaskRun(TaskRun updatedTaskRun)
        {
            if (updatedTaskRun == null)
            {
                return false;
            }

            lock (_taskMapLock)
            {
                if (!_taskRunMap.ContainsKey(updatedTaskRun.Guid))
                {
                    return false;
                }

                var run = _taskRunMap[updatedTaskRun.Guid];

                if (run.TaskRunComplete)
                {
                    // TaskRun was marked as finished, don't let it be edited post-completion
                    return false;
                }

                run.ExitCode = updatedTaskRun.ExitCode;
                run.TaskStatus = updatedTaskRun.TaskStatus;
                run.TimeFinished = updatedTaskRun.TimeFinished;
                run.TimeStarted = updatedTaskRun.TimeStarted;
                run.UpdateOwningTaskFromTaskRun();

                return true;
            }
        }

        public static void RemoveTaskRunsForTask(Guid taskGuid, bool preserveLogs)
        {
            Parallel.ForEach(GetTaskRunGuidsByTaskGuid(taskGuid), taskRunGuid =>
            {
                RemoveTaskRun(taskRunGuid, preserveLogs);
            });
        }

        public TaskRun_Server(TaskBase owningTask, string defaultLogFolder, Guid TaskListGuid) : this(owningTask, defaultLogFolder)
        {
            OwningTaskListGuid = TaskListGuid;
        }

        public TaskRun_Server(TaskBase owningTask, string defaultLogFolder, Guid TaskListGuid, Guid TaskRunGuid) : this(owningTask, defaultLogFolder)
        {
            OwningTaskListGuid = TaskListGuid;

            // CtorCommon already added the wrong guid to the map. Remove autogenned guid, add ours by re-running CtorCommon()
            RemoveTaskRun(this.Guid, false);
            Guid = TaskRunGuid;
            CtorCommon();

            // Fix log path, as it also used the wrong guid.
            SetLogFolderFromTask(defaultLogFolder);
        }

        public TaskRun_Server(TaskBase owningTask, string defaultLogFolder) : base(owningTask)
        {
            CtorCommon();
            OwningTask = owningTask;

            // Setup log path
            SetLogFolderFromTask(defaultLogFolder);
        }

        public static string FindFileInPath(string file)
        {
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

        /// <summary>
        /// Private Ctor used for task runs not backed by a TaskBase object
        /// </summary>
        /// <param name="taskPath"></param>
        /// <param name="arguments"></param>
        /// <param name="logFileOrPath"></param>
        private TaskRun_Server(string taskPath, string arguments, string logFileOrPath, TaskType type) : base(null)
        {
            // Set values via method args. They weren't set by base Ctor since the owning task is null.
            TaskPath = taskPath;
            Arguments = arguments;
            TaskName = Path.GetFileName(taskPath);
            TaskType = type;

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

            // Add to GUID -> TaskRun map
            lock (_taskMapLock)
            {
                _taskRunMap.TryAdd(Guid, this);
            }
        }

        private void SetLogFolderFromTask(string defaultLogFolder)
        {
            string LogFolder = OwningTask.LogFolder;
            if (LogFolder == null)
            {
                LogFolder = defaultLogFolder;
            }

            LogFilePath = Path.Combine(LogFolder, TaskName, $"Run_{Guid}.log");
        }
        private TaskRun_Server(TaskRun taskRun, TaskBase owningTask) : base(owningTask)
        {
            // This is used as a copy constructor. Don't add it to the map, it should be there already.
            LogFilePath = taskRun.LogFilePath;
            TaskStatus = taskRun.TaskStatus;
            TimeFinished = taskRun.TimeFinished;
            TimeStarted = taskRun.TimeStarted;
            ExitCode = taskRun.ExitCode;
            TaskOutput = taskRun.TaskOutput;
            OwningTask = owningTask;
        }


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

        public void StartWaitingForExternalResult()
        {
            TimeStarted = DateTime.Now;
            TaskStatus = TaskStatus.WaitingForExternalResult;
            UpdateOwningTaskFromTaskRun();
        }

        public void EndWaitingForExternalResult()
        {
            TimeFinished = DateTime.Now;
            UpdateOwningTaskFromTaskRun();
        }

        [JsonIgnore]
        public TaskBase OwningTask { get; }

        [JsonIgnore]
        public Guid OwningTaskListGuid { get; }

        public TaskRunner GetOwningTaskRunner()
        {
            return TaskRunner.GetTaskRunnerForTaskRun(this.Guid);
        }

        /// <summary>
        /// Tracks all the task runs that have ever occured, mapped by the task run GUID
        /// </summary>
        private static ConcurrentDictionary<Guid, TaskRun_Server> _taskRunMap;
        private static object _taskMapLock;
    }
}