using Microsoft.FactoryTestFramework.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Microsoft.FactoryTestFramework.Server
{
    public static class TestBase_ServerExtensions
    {
        public static Guid CreateTestRun(this TestBase test, string defaultLogFolder)
        {
            TestRun_Server run;
            lock (test.TestLock)
            {
                run = new TestRun_Server(test, defaultLogFolder);
                test.TestRunGuids.Add(run.Guid);
                test.LatestTestRunExitCode = null;
                test.LatestTestRunStatus = TestStatus.NotRun;
                test.LatestTestRunTimeFinished = null;
                test.LatestTestRunTimeStarted = null;
            }
            return run.Guid;
        }

        public static void GetLatestTestRun(this TestBase test)
        {
            TestRun_Server.GetTestRunByGuid(test.TestRunGuids.Last());
        }

        public static void Reset(this TestBase test, bool preserveLogs = true)
        {
            lock (test.TestLock)
            {
                test.LatestTestRunStatus = TestStatus.NotRun;
                test.LatestTestRunExitCode = null;
                test.LatestTestRunTimeFinished = null;
                test.LatestTestRunTimeStarted = null;
                test.TestRunGuids = new List<Guid>();
            }

            TestRun_Server.RemoveTestRunsForTest(test.Guid, preserveLogs);
        }
    }

    public class TestManager_Server
    {
        public TestManager_Server(string defaultLogFolder)
        {
            _startNonParallelTestRunLock = new SemaphoreSlim(1, 1);
            KnownTestLists = new Dictionary<Guid, TestList>();
            RunningTestListTokens = new Dictionary<Guid, CancellationTokenSource>();
            RunningBackgroundTasks = new Dictionary<Guid, List<TestRunner>>();
            RunningTestTestRunTokens = new Dictionary<Guid, CancellationTokenSource>();
            TestListStateFile = Path.Combine(defaultLogFolder, "FTFServiceKnownTestLists.testlists");
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
                // Don't allow the folder to move if any test is running or we are actively modifying testlists
                lock (RunningTestListLock)
                {
                    if (IsTestListRunning)
                    {
                        throw new TestManagerTestListRunningException();
                    }

                    lock (KnownTestListLock)
                    {
                        if (moveFiles && (_defaultLogFolder != null) && (Directory.Exists(_defaultLogFolder)))
                        {
                            // Move existing folder to temp folder
                            var tempDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), "FTFTemp");
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

                        var newStateFilePath = Path.Combine(newLogFolder, "FTFServiceKnownTestLists.testlists");

                        if (!moveFiles)
                        {
                            // Move state file if needed
                            if (File.Exists(TestListStateFile))
                            {
                                File.Move(TestListStateFile, newStateFilePath);
                            }
                        }

                        // Update paths
                        _defaultLogFolder = newLogFolder;
                        TestListStateFile = newStateFilePath;
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

        public TestList CreateTestListFromDirectory(String path, bool onlyTAEF)
        {
            // Recursive search for all exe and dll files
            var exes = Directory.EnumerateFiles(path, "*.exe", SearchOption.AllDirectories);
            var dlls = Directory.EnumerateFiles(path, "*.dll", SearchOption.AllDirectories);
            TestList tests = new TestList(Guid.NewGuid());
            // TODO: Performance: Parallel
            //Parallel.ForEach<string>(dlls, (dll) =>
            foreach (var dll in dlls)
            {
                var maybeTAEF = CheckForTAEFTest(dll);
                if (maybeTAEF != null)
                {
                    tests.Tests.Add(maybeTAEF.Guid, maybeTAEF);
                }
            }
            //);

            // Assume every .exe is a valid console test
            if (!onlyTAEF)
            {
                foreach (var exe in exes)
                {
                    var test = new ExecutableTest(exe);
                    tests.Tests.Add(test.Guid, test);
                }
            }

            lock (KnownTestListLock)
            {
                KnownTestLists.Add(tests.Guid, tests);
            }

            // Update XML for state tracking (this locks KnownTestListLock)
            SaveAllTestListsToXmlFile(TestListStateFile);

            return tests;
        }

        public bool SaveAllTestListsToXmlFile(string filename)
        {
            FTFXML xml = new FTFXML();

            lock (KnownTestListLock)
            {
                if (KnownTestLists.Count > 0)
                {
                    xml.TestLists = KnownTestLists.Values.ToList();
                }
                else
                {
                    return false;
                }

                if (!xml.Save(filename))
                {
                    throw new TestManagerException($"Could not save TestLists to {filename}!");
                }
            }

            return true;
        }

        public bool SaveTestListToXmlFile(Guid guid, string filename)
        {
            if (IsTestListRunning)
            {
                throw new TestManagerTestListRunningException();
            }
            
            FTFXML xml = new FTFXML();

            if (KnownTestLists.ContainsKey(guid))
            {
                xml.TestLists.Add(KnownTestLists[guid]);
            }
            else
            {
                // TODO: Logging
                return false;
            }

            return xml.Save(filename);
        }

        public List<Guid> LoadTestListsFromXmlFile(string filename)
        {
            if (IsTestListRunning)
            {
                throw new TestManagerTestListRunningException();
            }

            FTFXML xml;
            xml = FTFXML.Load(filename);

            // Add GUIDs to any TestBase or TestList objects that don't have one
            lock (KnownTestListLock)
            {
                foreach (var list in xml.TestLists)
                {
                    // Update "running" tests, as their state is unknown
                    list.Tests.Values.Where(x => x.LatestTestRunStatus == TestStatus.Running).Select(x => x.LatestTestRunStatus = TestStatus.Unknown);
                    list.Tests.Values.Where(x => x.LatestTestRunStatus == TestStatus.WaitingForExternalResult).Select(x => x.LatestTestRunStatus = TestStatus.Unknown);

                    if (KnownTestLists.ContainsKey(list.Guid))
                    {
                        // Overwrite existing testlist
                        KnownTestLists[list.Guid] = list;

                        // todo: logging
                    }
                    else
                    {
                        KnownTestLists.Add(list.Guid, list);
                    }
                }
            }

            // Update XML for state tracking
            if (filename != TestListStateFile)
            {
                SaveAllTestListsToXmlFile(TestListStateFile);
            }

            return xml.TestLists.Select(x => x.Guid).ToList();
        }

        public bool DeleteTestList(Guid listToDelete)
        {
            bool removed = false;
            lock (KnownTestListLock)
            {
                // Abort() gracefully returns if the guid is invalid
                AbortTestList(listToDelete);
                removed = KnownTestLists.Remove(listToDelete);   
            }

            // Update XML for state tracking
            SaveAllTestListsToXmlFile(TestListStateFile);

            return removed;
        }

        public TestBase GetKnownTest(Guid testGuid)
        {
            var list = KnownTestLists.Values.Where(x => x.Tests.ContainsKey(testGuid)).First();
            if (list == null)
            {
                return null;
            }

            TestBase test;
            if (list.Tests.TryGetValue(testGuid, out test))
            {
                return test;
            }
            else
            {
                return null;
            }
        }

        public TestList GetKnownTestList(Guid guid)
        {
            TestList list = null;
            if (KnownTestLists.TryGetValue(guid, out list))
            {
                return list;
            }
            else
            {
                return null;
            }
        }

        public List<Guid> GetKnownTestListGuids()
        {
            return KnownTestLists.Keys.ToList();
        }

        public TestList CreateTestListFromTestList(TestList testList)
        {
            lock (KnownTestListLock)
            {
                try
                {
                    KnownTestLists.Add(testList.Guid, testList);
                }
                catch (ArgumentException)
                {
                    // list already exists
                    return KnownTestLists[testList.Guid];
                }
            }

            return testList;
        }

        public void Reset(bool preserveLogs = true, bool terminateBackgroundTasks = true)
        {
            lock (RunningTestListLock)
            {
                lock (KnownTestListLock)
                {
                    // Cancel all running TestLists
                    foreach (var token in RunningTestListTokens.Values)
                    {
                        token.Cancel();
                    }

                    // Cancel any started testruns
                    foreach (var token in RunningTestTestRunTokens.Values)
                    {
                        token.Cancel();
                    }

                    // Kill all background tasks
                    if (terminateBackgroundTasks)
                    {
                        Parallel.ForEach(RunningBackgroundTasks.Values.SelectMany(x => x), (bgRunner) =>
                        {
                            bgRunner.StopTest();
                        });

                        RunningBackgroundTasks = new Dictionary<Guid, List<TestRunner>>();
                    }

                    // Reset all tests
                    foreach (var test in KnownTestLists.Values.SelectMany(x => x.Tests.Values))
                    {
                        test.Reset(preserveLogs);
                    }

                    // Delete state file
                    File.Delete(TestListStateFile);

                    // Create new dictionaries
                    RunningTestListTokens = new Dictionary<Guid, CancellationTokenSource>();
                    RunningTestTestRunTokens = new Dictionary<Guid, CancellationTokenSource>();
                    KnownTestLists = new Dictionary<Guid, TestList>();
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
            TestRun_Server testRun = new TestRun_Server(maybeTAEF, DefaultLogFolder);
            TestRunner runner = new TestRunner(testRun);
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
                    runner.StopTest();
                    throw new Exception(String.Format("TE.exe timed out trying to validate possible TAEF test: {0}", dllToTest));
                }

                testRun = TestRun_Server.GetTestRunByGuid(maybeTAEF.TestRunGuids[0]);
                // "No tests were executed." error, returned when a binary is not a valid TAEF test.
                // todo: Feature: but not always???
                // https://docs.microsoft.com/en-us/windows-hardware/drivers/taef/exit-codes-for-taef
                if ((testRun.ExitCode == 117440512) || (testRun.ExitCode == 0))
                {
                    // Check if it was able to enumerate the test cases.
                    if (!testRun.TestOutput.Any(x => x.Contains("Summary of Errors Outside of Tests")) && !testRun.TestOutput.Any(x => x.Contains("Failed to load")))
                    {
                        // TODO: Feature: We need a better mechanism here
                        isTaef = true;
                    }
                }
                else
                {
                    throw new Exception(String.Format("TE.exe returned error {0} when trying to validate possible TAEF test: {1}", maybeTAEF.LatestTestRunExitCode, dllToTest));
                }
            }
            catch (Exception)
            {
                maybeTAEF.Reset(false);
                // TODO: Logging: undo this
                //throw new Exception(String.Format("Unable to validate possible TAEF test: {0}", dllToTest), e);
            }

            // Cleanup test run
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

        public bool UpdateTestList(TestList testList)
        {
            if (testList == null)
            {
                return false;
            }

            lock (KnownTestListLock)
            {
                if (KnownTestLists.ContainsKey(testList.Guid))
                {
                    lock (RunningTestListLock)
                    {
                        // if it is running don't update the testlist
                        if (RunningTestListTokens.Keys.Contains(testList.Guid))
                        {
                            return false;
                        }
                        else
                        {
                            KnownTestLists[testList.Guid] = testList;
                            return true;
                        }
                    }
                }
            }

            // Update XML for state tracking
            SaveAllTestListsToXmlFile(TestListStateFile);

            return false;
        }

        public bool UpdateTest(TestBase updatedTest)
        {
            if (updatedTest == null)
            {
                return false;
            }

            // Changing state, lock the test lists
            lock (KnownTestListLock)
            {
                // Find list this test in it
                var testList = KnownTestLists.Values.Where(x => x.Tests.ContainsKey(updatedTest.Guid)).DefaultIfEmpty(null).First();

                if (testList == null)
                {
                    return false;
                }
                else
                {
                    lock (RunningTestListLock)
                    {
                        // if it is running don't update the test
                        if (RunningTestListTokens.Keys.Contains(testList.Guid))
                        {
                            return false;
                        }
                        else
                        {
                            testList.Tests[updatedTest.Guid] = updatedTest;
                        }
                    }
                }
            }

            // Update XML for state tracking
            SaveAllTestListsToXmlFile(TestListStateFile);

            return true;
        }

        public bool UpdateTestRunStatus(TestRun latestTestRun)
        {
            if (latestTestRun == null)
            {
                return false;
            }

            return TestRun_Server.UpdateTestRun(latestTestRun);
        }

        public bool RunTestList(Guid TestListGuidToRun)
        {
            TestList list = null;

            // Check if test list is valid
            if (!KnownTestLists.TryGetValue(TestListGuidToRun, out list))
            {
                return false;
            }

            // Check if test list is already running or set to be run
            lock (RunningTestListLock)
            {
                if (RunningTestListTokens.ContainsKey(TestListGuidToRun))
                {
                    return false;
                }

                // Create testrun for all tests in the list
                List<Guid> testRunGuids = new List<Guid>();
                foreach (var test in list.Tests.Values)
                {
                    testRunGuids.Add(test.CreateTestRun(DefaultLogFolder));
                }

                // Update XML for state tracking.
                SaveAllTestListsToXmlFile(TestListStateFile);

                var workItem = new TestListWorkItem(list.Guid, testRunGuids, list.TerminateBackgroundTasksOnCompletion, list.AllowOtherTestListsToRun, list.RunInParallel);
                var token = new CancellationTokenSource();
                RunningTestListTokens.Add(workItem.TestListGuid, token);
                RunningBackgroundTasks.Add(workItem.TestListGuid, new List<TestRunner>());
                Task t = new Task((i) => { TestListWorker(i, token.Token); }, workItem, token.Token);
                t.Start();
            }

            return true;
        }

        public class TestListWorkItem
        {
            public TestListWorkItem(Guid testListGuid, List<Guid> testRunGuids, bool terminateBackgroundTasksOnCompletion, bool allowOtherTestListsToRun, bool runListInParallel)
            {
                TestListGuid = testListGuid;
                AllowOtherTestListsToRun = allowOtherTestListsToRun;
                RunListInParallel = runListInParallel;
                TerminateBackgroundTasksOnCompletion = terminateBackgroundTasksOnCompletion;
                TestRunGuids = testRunGuids;
            }

            public Guid TestListGuid;
            public bool AllowOtherTestListsToRun;
            public bool RunListInParallel;
            public bool TerminateBackgroundTasksOnCompletion;
            public List<Guid> TestRunGuids;
        }

        private void TestListWorker(object i, CancellationToken token)
        {
            var item = (TestListWorkItem)i;
            bool usedSem = false;

            if (!item.AllowOtherTestListsToRun)
            {
                try
                {
                    _startNonParallelTestRunLock.Wait(token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                usedSem = true;
            }
            else
            {
                usedSem = _startNonParallelTestRunLock.Wait(0);
            }

            if (!token.IsCancellationRequested)
            {
                StartTestRuns(item.TestListGuid, item.TestRunGuids, token, item.TerminateBackgroundTasksOnCompletion, item.RunListInParallel);

                // Tests are done! Update the tokens & locks
                lock (RunningTestListLock)
                {
                    RunningTestListTokens.Remove(item.TestListGuid);
                }
                if (usedSem)
                {
                    _startNonParallelTestRunLock.Release();
                }
            }

            if (token.IsCancellationRequested)
            {
                // Update XML for state tracking. This is only needed if a test was aborted.
                SaveAllTestListsToXmlFile(TestListStateFile);
            }
        }

        private void StartTestRuns(Guid testListGuid, List<Guid> testRunGuids, CancellationToken token, bool terminateBackgroundTasksOnCompletion = true, bool runInParallel = false, TestRunnerEventHandler testRunEventHandler = null)
        {
            // Run all enabled tests in the list
            if (!runInParallel)
            {
                foreach (var runGuid in testRunGuids)
                {
                    // find testrun, run it
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    var testRun = TestRun_Server.GetTestRunByGuid(runGuid);
                    var testToken = new CancellationTokenSource();

                    if (!testRun.BackgroundTask)
                    {
                        RunningTestTestRunTokens.Add(testRun.Guid, testToken);
                    }

                    StartTest(testRun, testToken.Token, testRunEventHandler);

                    if (testRun.BackgroundTask)
                    {
                        RunningBackgroundTasks[testListGuid].Add(testRun.OwningTestRunner);
                    }

                    // Update saved state
                    SaveAllTestListsToXmlFile(TestListStateFile);
                }
            }
            else
            {
                Parallel.ForEach(testRunGuids, (runGuid, state) =>
                {
                    if (token.IsCancellationRequested)
                    {
                        state.Stop();
                    }

                    var testRun = TestRun_Server.GetTestRunByGuid(runGuid);
                    StartTest(testRun, token, testRunEventHandler);
                    if (testRun.BackgroundTask)
                    {
                        RunningBackgroundTasks[testListGuid].Add(testRun.OwningTestRunner);
                    }

                    // Update saved state
                    SaveAllTestListsToXmlFile(TestListStateFile);
                });
            }

            // Kill all background tasks
            if (terminateBackgroundTasksOnCompletion)
            {
                Parallel.ForEach(RunningBackgroundTasks[testListGuid], (bgRunner) =>
                {
                    bgRunner.StopTest();
                });
                RunningBackgroundTasks.Remove(testListGuid);
            }
        }

        private void StartTest(TestRun_Server testRun, CancellationToken token, TestRunnerEventHandler testRunEventHandler = null)
        {
            if (!token.IsCancellationRequested)
            {
                TestRunner runner = null;

                if (testRun.RunByServer)
                {
                    runner = new TestRunner(testRun);

                    if (testRunEventHandler != null)
                    {
                        runner.OnTestEvent += testRunEventHandler;
                    }

                    if (!runner.RunTest())
                    {
                        throw new Exception(String.Format("Unable to start TestRun {0} for test {1}", testRun.Guid, testRun.TestName));
                    }
                }
                else
                {
                    // TODO: Logging : Log External tests to file
                    testRun.StartWaitingForExternalResult();
                    OnTestManagerEvent?.Invoke(this, new TestManagerEventArgs(TestManagerEventType.WaitingForExternalTestRunResult, testRun.Guid));
                }

                // Wait for test to finish, timeout, or be aborted
                if (!testRun.BackgroundTask)
                {
                    while (!token.IsCancellationRequested && !testRun.TestRunComplete)
                    {
                        // TODO: Performance: replace with a signal mechanism
                        // TODO: Feature, Performance: Just sleep for the timeout value if possible
                        Thread.Sleep(1000);
                    }
                }

                if (token.IsCancellationRequested)
                {
                    if (testRun.RunByServer)
                    {
                        runner.StopTest();
                    }
                    else
                    {
                        testRun.TestStatus = TestStatus.Aborted;
                        testRun.ExitCode = -1;
                    }
                }

                if (testRun.RunByClient)
                {
                    testRun.EndWaitingForExternalResult();
                    if (token.IsCancellationRequested)
                    {
                        OnTestManagerEvent?.Invoke(this, new TestManagerEventArgs(TestManagerEventType.ExternalTestRunAborted, testRun.Guid));
                    }
                    else
                    {
                        OnTestManagerEvent?.Invoke(this, new TestManagerEventArgs(TestManagerEventType.ExternalTestRunFinished, testRun.Guid));
                    }
                }
            }
        }

        public void AbortAllTestLists()
        {
            lock (RunningTestListLock)
            {
                foreach (var token in RunningTestListTokens.Values)
                {
                    token.Cancel();
                }
                foreach (var token in RunningTestTestRunTokens.Values)
                {
                    token.Cancel();
                }

                RunningTestListTokens.Clear();
                RunningTestTestRunTokens.Clear();
            }
        }

        public void AbortTestList(Guid testListToCancel)
        {
            lock (RunningTestListLock)
            {
                CancellationTokenSource token;
                if (RunningTestListTokens.TryGetValue(testListToCancel, out token))
                {
                    token.Cancel();
                    RunningTestListTokens.Remove(testListToCancel);

                    var testRunGuids = KnownTestLists[testListToCancel].Tests.Values.Select(x => x.Guid);
                    foreach (var guid in testRunGuids)
                    {
                        if (RunningTestTestRunTokens.TryGetValue(guid, out token))
                        {
                            token.Cancel();
                            RunningTestTestRunTokens.Remove(guid);
                        }
                    }
                }
            }
        }

        public void AbortTestRun(Guid testRunToCancel)
        {
            lock (RunningTestListLock)
            {
                CancellationTokenSource token;
                List<TestRunner> backgroundTasks;
                if (RunningTestTestRunTokens.TryGetValue(testRunToCancel, out token))
                {
                    token.Cancel();
                    RunningTestTestRunTokens.Remove(testRunToCancel);
                }
                else if (RunningBackgroundTasks.TryGetValue(testRunToCancel, out backgroundTasks))
                {
                    backgroundTasks[0].StopTest();
                    RunningBackgroundTasks.Remove(testRunToCancel);
                }
                else 
                {
                    foreach (var list in RunningBackgroundTasks.Values)
                    {
                        if (list.Select(x => x.ActiveTestRun.Guid).Contains(testRunToCancel))
                        {
                            var runner = list.First(x => x.ActiveTestRun.Guid == testRunToCancel);
                            runner.StopTest();
                            list.Remove(runner);
                        }
                    }
                }
            }
        }

        public TestRun RunExecutableAsBackgroundTask(string exeFilePath, string arguments, string consoleLogFilePath = null)
        {
            var expandedPath = Environment.ExpandEnvironmentVariables(exeFilePath);
            if (!File.Exists(expandedPath))
            {
                // TODO: Logging: log error
                return null;
            }

            var run = TestRun_Server.CreateTestRunWithoutTest(expandedPath, arguments, consoleLogFilePath, TestType.ConsoleExe);
            run.BackgroundTask = true;
            var token = new CancellationTokenSource();
            StartTest(run, token.Token);

            RunningBackgroundTasks.Add(run.Guid, new List<TestRunner>(1) { run.OwningTestRunner });

            return run;
        }

        public TestRun RunTestOutsideTestList(Guid executableTestGuid)
        {
            var test = KnownTestLists.SelectMany(x => x.Value.Tests.Values).Where(y => y.Guid.Equals(executableTestGuid)).DefaultIfEmpty(null).First();

            if (test == null)
            {
                // TODO: Logging: log error
                return null;
            }

            var runGuid = test.CreateTestRun(DefaultLogFolder);
            var run = TestRun_Server.GetTestRunByGuid(runGuid);
            var token = new CancellationTokenSource();

            if (run.BackgroundTask)
            {
                StartTest(run, token.Token);
                RunningBackgroundTasks.Add(run.Guid, new List<TestRunner>(1) { run.OwningTestRunner });
            }
            else
            {
                RunningTestTestRunTokens.Add(run.Guid, token);
                Task t = new Task(() => { StartTest(run, token.Token); });
                t.Start();
            }

            return run;
        }

        public TestRun RunUWPOutsideTestList(string packageFamilyName)
        {
            var run = TestRun_Server.CreateTestRunWithoutTest(packageFamilyName, null, null, TestType.UWP);
            var token = new CancellationTokenSource();
            RunningTestTestRunTokens.Add(run.Guid, token);
            Task t = new Task(() => { StartTest(run, token.Token); });
            t.Start();

            return run;
        }

        private Dictionary<Guid, TestList> KnownTestLists;
        private Dictionary<Guid, CancellationTokenSource> RunningTestListTokens;
        private Dictionary<Guid, CancellationTokenSource> RunningTestTestRunTokens;
        private Dictionary<Guid, List<TestRunner>> RunningBackgroundTasks;
        private readonly object KnownTestListLock = new object();
        private readonly object RunningTestListLock = new object();
        private readonly SemaphoreSlim _startNonParallelTestRunLock;
        private string _defaultLogFolder;

        public string DefaultLogFolder { get => _defaultLogFolder; }
        public string TestListStateFile { get; private set; }
        public bool IsTestListRunning
        {
            get
            {
                return (RunningTestListTokens.Count > 0);
            }
        }

        public bool CanStartNewTestListRun
        {
            get
            {
                if (RunningTestListTokens.Count == 0)
                {
                    return true;
                }
                else
                {
                    // Check if a running TestList doesn't allow a other TestLists to run
                    return !KnownTestLists.Values.Any(x => (x.TestListStatus == TestStatus.Running) && (x.AllowOtherTestListsToRun == false));
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

    public class TestManagerTestListRunningException : TestManagerException
    {
        public TestManagerTestListRunningException() : base("Cannot perform operation because one or more TestLists are actively running!")
        { }

        public TestManagerTestListRunningException(Guid guid) : base($"Cannot perform operation because TestList {guid} is actively running!", guid)
        { }
    }

    public delegate void TestManagerEventHandler(object source, TestManagerEventArgs e);

    public enum TestManagerEventType
    {
        NewTestList,
        UpdatedTestList,
        DeletedTestList,
        TestListRunStarted,
        TestListRunEnded,
        WaitingForExternalTestRunResult,
        ExternalTestRunFinished,
        ExternalTestRunAborted,
        ExternalTestRunTimeout,
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

    public delegate void TestRunnerEventHandler(object source, TestRunnerEventArgs e);

    public class TestRunnerEventArgs : EventArgs
    {
        public TestRunnerEventArgs(TestStatus testStatus, int eventStatusCode, String eventMessage)
        {
            TestStatus = testStatus;
            EventStatusCode = eventStatusCode;
            EventMessage = eventMessage;
        }

        public TestStatus TestStatus { get; }
        public int EventStatusCode { get; }
        public String EventMessage { get; }
    }

    public class TestRunExceptionEvent : TestRunnerEventArgs
    {
        public TestRunExceptionEvent(TestStatus eventType, int eventStatusCode, Exception eventException) :
            base(eventType, eventStatusCode, (eventException != null) ? eventException.ToString() : null)
        {
            EventException = eventException;
        }

        public Exception EventException { get; }
    }

    public class TestRunner
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

        public TestRunner(TestRun_Server testRun)
        {
            IsRunning = false;
            ActiveTestRun = testRun;
            BackgroundTask = testRun.BackgroundTask;
        }

        public bool RunTest()
        {
            lock (runnerStateLock)
            {
                if (IsRunning == true)
                {
                    return true;
                }

                // Create Process object
                TestProcess = CreateTestProcess();

                // Start the process
                return StartTestProcess();
            }
        }

        internal bool CheckIfTaefTest()
        {
            ActiveTestRun.ConsoleLogFilePath = null;
            lock (runnerStateLock)
            {
                if (IsRunning == true)
                {
                    return true;
                }

                // Create Process object
                TestProcess = CreateTestProcess();

                // Override args to check if this is a valid TAEF test, not try to run it
                TestProcess.StartInfo.Arguments = ActiveTestRun.TestPath + " /list";

                // Start the process
                return StartTestProcess();
            }
        }

        private Process CreateTestProcess()
        {
            TestProcess = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();

            if (ActiveTestRun.TestType == TestType.TAEFDll)
            {
                startInfo.FileName = GlobalTeExePath;
                startInfo.Arguments += ActiveTestRun.TestPath + GlobalTeArgs;
            }
            else
            {
                startInfo.FileName = ActiveTestRun.TestPath;
            }

            startInfo.Arguments += ActiveTestRun.Arguments;

            // Configure IO redirection
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.CreateNoWindow = true;
            if (ActiveTestRun.TestType == TestType.TAEFDll)
            {
                startInfo.Environment["Path"] = startInfo.Environment["Path"] + ";" + Path.GetDirectoryName(ActiveTestRun.TestPath);
                startInfo.WorkingDirectory = Path.GetDirectoryName(startInfo.FileName);
            }
            else
            {
                startInfo.WorkingDirectory = Path.GetDirectoryName(ActiveTestRun.TestPath);
            }

            // Configure event handling
            TestProcess.EnableRaisingEvents = true;
            TestProcess.Exited += OnExited;
            TestProcess.OutputDataReceived += OnOutputData;
            TestProcess.ErrorDataReceived += OnErrorData;

            TestProcess.StartInfo = startInfo;
            return TestProcess;
        }

        private bool StartTestProcess()
        {
            if (TestProcess.Start())
            {
                IsRunning = true;
                ActiveTestRun.OwningTestRunner = this;
                ActiveTestRun.TestStatus = TestStatus.Running;
                ActiveTestRun.TimeStarted = DateTime.Now;

                // Start async read (OnOutputData, OnErrorData)
                TestProcess.BeginErrorReadLine();
                TestProcess.BeginOutputReadLine();

                // Start timeout timer if needed
                if (ActiveTestRun.TimeoutSeconds != -1)
                {
                    Task.Run(async () =>
                    {
                        await Task.Delay(new TimeSpan(0, 0, ActiveTestRun.TimeoutSeconds), TimeoutToken.Token);

                        // If we hit the timeout and the test didn't finish, stop it now.
                        if (!TimeoutToken.IsCancellationRequested)
                        {
                            TimeoutTest();
                        }
                    });
                }
            }
            else
            {
                ActiveTestRun.TestStatus = TestStatus.Failed;
                // TODO: log error
            }

            ActiveTestRun.UpdateOwningTestFromTestRun();

            // Write header to file
            var LogFilePath = ActiveTestRun.ConsoleLogFilePath;
            if (LogFilePath != null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath));
                List<string> header = new List<string>();

                if (!BackgroundTask)
                {
                    header.Add(String.Format("Test: {0}", ActiveTestRun.TestName));
                }
                else
                {
                    header.Add(String.Format("Background Task: {0}", ActiveTestRun.TestName));
                }

                header.Add(String.Format("GUID: {0}", (ActiveTestRun.OwningTestGuid == null) ? "Not a known Test" : ActiveTestRun.OwningTestGuid.ToString()));
                header.Add(String.Format("TestRun GUID: {0}", ActiveTestRun.Guid));
                header.Add(String.Format("Type: {0}", ActiveTestRun.TestType));
                header.Add(String.Format("Path: {0}", ActiveTestRun.TestPath));
                header.Add(String.Format("Arguments: {0}", ActiveTestRun.Arguments));
                header.Add(String.Format("Date/Time run: {0}", (ActiveTestRun.TimeStarted == null) ? "Never Started" : ActiveTestRun.TimeStarted.ToString()));
                if (IsRunning)
                {
                    header.Add(String.Format("--------------- Console Output --------------"));
                }
                else
                {
                    header.Add("---------------------------------------------");
                    header.Add("Process failed to start!");
                    header.Add("---------------------------------------------");
                    header.Add(String.Format("Result: {0}", ActiveTestRun.TestStatus));
                    header.Add(String.Format("Exit code: {0}", ActiveTestRun.ExitCode));
                }
                File.WriteAllLines(LogFilePath, header);

                lastLineSavedToFile = -1;
                outputTimer = new Timer(OnOutputTimer, null, 5000, 5000);
            }

            return IsRunning;
        }

        private void OnOutputTimer(object state)
        {
            lock (outputLock)
            {
                File.WriteAllLines(ActiveTestRun.ConsoleLogFilePath, ActiveTestRun.TestOutput.GetRange(lastLineSavedToFile + 1, ActiveTestRun.TestOutput.Count));
                lastLineSavedToFile = ActiveTestRun.TestOutput.Count - 1;
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
                ActiveTestRun.TestOutput.Add(line);

                if (ActiveTestRun.TestType == TestType.TAEFDll)
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

            // Cancel the test timeout
            TimeoutToken.Cancel();

            // Save the result of the test
            if (!TestAborted)
            {
                ActiveTestRun.TimeFinished = DateTime.Now;
                ActiveTestRun.ExitCode = TestProcess.ExitCode;
                ActiveTestRun.TestStatus = (ActiveTestRun.ExitCode == 0) ? TestStatus.TestPassed : TestStatus.Failed;
            }
            else
            {
                ActiveTestRun.ExitCode = -1;
                ActiveTestRun.TestStatus = TestTimeout ? TestStatus.Timeout : TestStatus.Aborted;
            }
            ActiveTestRun.UpdateOwningTestFromTestRun();

            // Save test output to file
            lock (outputLock)
            {
                var LogFilePath = ActiveTestRun.ConsoleLogFilePath;
                if (LogFilePath != null)
                {

                    File.WriteAllLines(ActiveTestRun.ConsoleLogFilePath, ActiveTestRun.TestOutput.GetRange(lastLineSavedToFile + 1, ActiveTestRun.TestOutput.Count));
                    lastLineSavedToFile = ActiveTestRun.TestOutput.Count - 1;
                    File.WriteAllLines(LogFilePath,
                                        new String[] {
                                        "---------------------------------------------",
                                        String.Format("Result: {0}", ActiveTestRun.TestStatus),
                                        String.Format("Exit code: {0}", ActiveTestRun.ExitCode),
                                        String.Format("Time to complete: {0}", (ActiveTestRun.RunTime == null) ? "" : ActiveTestRun.RunTime.ToString())
                                        });
                }
            }

            IsRunning = false;

            // Raise event if event handler exists
            OnTestEvent?.Invoke(this, new TestRunnerEventArgs(ActiveTestRun.TestStatus, (int)ActiveTestRun.ExitCode, null));
        }

        private bool TimeoutTest()
        {
            TestTimeout = true;
            return StopTest();
        }

        public bool StopTest()
        {
            lock (runnerStateLock)
            {
                // See if you get lucky and the test finishes on its own???
                if (!TestProcess.WaitForExit(500))
                {
                    TestAborted = true;
                    TestProcess.Kill();
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
            return WaitForExit(-1);
        }
        public bool WaitForExit(int milliseconds)
        {
            // After TestProcess.WaitForExit returns true, we still may need to wait for the OnExited method to complete.
            // Maintain our own timer so we can still exit close to the expected ms timeout value the user supplied.
            var timer = Stopwatch.StartNew();

            if (IsRunning)
            {
                if (TestProcess.WaitForExit(milliseconds))
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
        public TestRun_Server ActiveTestRun { get; set; }

        //public ExecutableTest Test { get; }

        public event TestRunnerEventHandler OnTestEvent;

        private Process TestProcess;
        private bool TestAborted = false;
        private bool TestTimeout = false;
        private bool BackgroundTask;
        private CancellationTokenSource TimeoutToken = new CancellationTokenSource();
        private Timer outputTimer = null;
        private int lastLineSavedToFile = -1;

        /// <summary>
        /// Lock to maintain consistent state of execution status (Run, Abort etc)
        /// </summary>
        private object runnerStateLock = new object();
        /// <summary>
        /// Lock to maintain serial ordering of test stdout & stderr
        /// </summary>
        private object outputLock = new object();
    }

    /// <summary>
    /// Server side only TestRun class.
    /// </summary>
    public class TestRun_Server : TestRun
    {
        // TODO: Move testrunner into testrun_server
        static TestRun_Server()
        {
            _testRunMap = new Dictionary<Guid, TestRun_Server>();
            _testMapLock = new object();
        }

        public static List<Guid> GetTestRunGuidsByTestGuid(Guid testGuid)
        {
            return _testRunMap.Values.Where(x => x.OwningTestGuid == testGuid).Select(x => x.Guid).ToList();
        }

        public static TestRun_Server GetTestRunByGuid(Guid testRunGuid)
        {
            if (_testRunMap.ContainsKey(testRunGuid))
            {
                return _testRunMap[testRunGuid];
            }
            else
            {
                return null;
            }
        }

        public static TestRun_Server CreateTestRunWithoutTest(string filePath, string arguments, string logFileOrFolder, TestType type)
        {
            return new TestRun_Server(filePath, arguments, logFileOrFolder, type);
        }

        public static void RemoveTestRun(Guid testRunGuid, bool preserveLogs)
        {
            TestRun_Server testRun = null;
            lock (_testMapLock)
            {
                if (_testRunMap.TryGetValue(testRunGuid, out testRun))
                {
                    _testRunMap.Remove(testRunGuid);
                    if (!preserveLogs)
                    {
                        if (File.Exists(testRun.ConsoleLogFilePath))
                        {
                            File.Delete(testRun.ConsoleLogFilePath);
                        }
                    }
                }
            }
        }

        public static bool UpdateTestRun(TestRun updatedTestRun)
        {
            if (updatedTestRun == null)
            {
                return false;
            }

            lock (_testMapLock)
            {
                if (!_testRunMap.ContainsKey(updatedTestRun.Guid))
                {
                    return false;
                }

                var run = _testRunMap[updatedTestRun.Guid];

                if (run.TestRunComplete)
                {
                    // TestRun was marked as finished, don't let it be edited post-completion
                    return false;
                }

                run.ExitCode = updatedTestRun.ExitCode;
                run.TestStatus = updatedTestRun.TestStatus;
                run.TimeFinished = updatedTestRun.TimeFinished;
                run.TimeStarted = updatedTestRun.TimeStarted;
                run.UpdateOwningTestFromTestRun();

                return true;
            }
        }


        public static void RemoveTestRunsForTest(Guid testGuid, bool preserveLogs)
        {
            Parallel.ForEach(GetTestRunGuidsByTestGuid(testGuid), testRunGuid =>
            {
                RemoveTestRun(testRunGuid, preserveLogs);
            });
        }

        public TestRun_Server(TestBase owningTest, string defaultLogFolder) : base(owningTest)
        {
            CtorCommon();
            OwningTest = owningTest;

            // Setup log path
            string LogFolder = owningTest.LogFolder;
            if (LogFolder == null)
            {
                LogFolder = defaultLogFolder;
            }

            ConsoleLogFilePath = Path.Combine(LogFolder, String.Format("{0}_Run{1}.log", TestName, Guid));
        }

        /// <summary>
        /// Private Ctor used for test runs not backed by a TestBase object
        /// </summary>
        /// <param name="testPath"></param>
        /// <param name="arguments"></param>
        /// <param name="logFileOrPath"></param>
        private TestRun_Server(string testPath, string arguments, string logFileOrPath, TestType type) : base(null)
        {
            CtorCommon();
            
            if (logFileOrPath != null)
            {
                var ext = logFileOrPath.Substring(logFileOrPath.Length - 3);
                if (ext == ".log" || ext == ".txt")
                {
                    ConsoleLogFilePath = logFileOrPath;
                }
                else
                {
                    ConsoleLogFilePath = Path.Combine(logFileOrPath, String.Format("{0}_Run{1}.log", TestName, Guid));
                }
            }
            
            // Set remaining values via method args. They weren't set by base Ctor since the owning test is null.
            TestPath = testPath;
            Arguments = arguments;
            TestName = Path.GetFileName(testPath);
            TestType = type;
        }

        private void CtorCommon()
        {
            OwningTestRunner = null;

            // Add to GUID -> TestRun map
            lock (_testMapLock)
            {
                _testRunMap.Add(Guid, this);
            }
        }

        private TestRun_Server(TestRun testRun, TestBase owningTest) : base(owningTest)
        {
            // This is used as a copy constructor. Don't add it to the map, it should be there already.
            ConsoleLogFilePath = testRun.ConsoleLogFilePath;
            TestStatus = testRun.TestStatus;
            TimeFinished = testRun.TimeFinished;
            TimeStarted = testRun.TimeStarted;
            ExitCode = testRun.ExitCode;
            TestOutput = testRun.TestOutput;
            OwningTest = owningTest;
        }


        public void UpdateOwningTestFromTestRun()
        {
            if (OwningTest != null)
            {
                lock (OwningTest.TestLock)
                {
                    OwningTest.LatestTestRunStatus = this.TestStatus;
                    OwningTest.LatestTestRunTimeStarted = this.TimeStarted;
                    OwningTest.LatestTestRunTimeFinished = this.TimeFinished;
                    OwningTest.LatestTestRunExitCode = this.ExitCode;
                }
            }
        }

        public void StartWaitingForExternalResult()
        {
            TimeStarted = DateTime.Now;
            TestStatus = TestStatus.WaitingForExternalResult;
            UpdateOwningTestFromTestRun();
        }

        public void EndWaitingForExternalResult()
        {
            TimeFinished = DateTime.Now;
            UpdateOwningTestFromTestRun();
        }

        public TestBase OwningTest { get; }
        public TestRunner OwningTestRunner { get; set; }
        /// <summary>
        /// Tracks all the test runs that have ever occured, mapped by the test run GUID
        /// </summary>
        private static Dictionary<Guid, TestRun_Server> _testRunMap;
        private static object _testMapLock;
    }
}