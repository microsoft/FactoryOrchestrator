using Microsoft.FactoryTestFramework.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
                test.LatestTestRunStatus = TestStatus.TestNotRun;
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
                test.LatestTestRunStatus = TestStatus.TestNotRun;
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
            StartTestRunLock = new SemaphoreSlim(1, 1);
            KnownTestLists = new Dictionary<Guid, TestList>();
            RunningTestListTokens = new Dictionary<Guid, CancellationTokenSource>();
            SetDefaultLogFolder(defaultLogFolder);
        }

        public bool SetDefaultLogFolder(string logFolder)
        {
            try
            {
                _defaultLogFolder = logFolder;
                Directory.CreateDirectory(logFolder);
                return true;
            }
            catch (Exception)
            {
                // todo log
                return false;
            }
        }

        public TestList CreateTestListFromDirectory(String path, bool onlyTAEF)
        {
            // Recursive search for all exe and dll files
            var exes = Directory.EnumerateFiles(path, "*.exe", SearchOption.AllDirectories);
            var dlls = Directory.EnumerateFiles(path, "*.dll", SearchOption.AllDirectories);
            TestList tests = new TestList(Guid.NewGuid());
            // TODO: Parallel
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
            return tests;
        }

        public bool DeleteTestList(Guid listToDelete)
        {
            lock (KnownTestListLock)
            {
                // Abort() gracefully returns if the guid is invalid
                Abort(listToDelete);
                return KnownTestLists.Remove(listToDelete);   
            }
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

        public void Reset(bool preserveLogs = true)
        {
            lock (RunningTestListLock)
            {
                lock (KnownTestListLock)
                {
                    // Cancel all running tests
                    foreach (var token in RunningTestListTokens.Values)
                    {
                        token.Cancel();
                    }

                    // Reset all tests
                    foreach (var test in KnownTestLists.Values.SelectMany(x => x.Tests.Values))
                    {
                        test.Reset(preserveLogs);
                    }

                    // Create new dictionaries
                    RunningTestListTokens = new Dictionary<Guid, CancellationTokenSource>();
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
                // todo: but not always???
                // https://docs.microsoft.com/en-us/windows-hardware/drivers/taef/exit-codes-for-taef
                if ((testRun.ExitCode == 117440512) || (testRun.ExitCode == 0))
                {
                    // Check if it was able to enumerate the test cases.
                    if (!testRun.TestOutput.Any(x => x.Contains("Summary of Errors Outside of Tests")) && !testRun.TestOutput.Any(x => x.Contains("Failed to load")))
                    {
                        // TODO: We need a better mechanism here
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
                // TODO: undo this
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
                            return true;
                        }
                    }
                }
            }
        }

        public bool UpdateTestRunStatus(TestRun latestTestRun)
        {
            if (latestTestRun == null)
            {
                return false;
            }

            // Test GUIDs are unique across testlists
            var testList = KnownTestLists.Values.Where(x => x.Tests.ContainsKey(latestTestRun.OwningTestGuid)).DefaultIfEmpty(null).First();

            if (testList == null)
            {
                return false;
            }
            else
            {
                var test = testList.Tests[latestTestRun.OwningTestGuid];
                if (!test.TestRunGuids.Contains(latestTestRun.Guid))
                {
                    return false;
                }

                return TestRun_Server.UpdateTestRun(latestTestRun, test);
            }
        }

        public bool Run(Guid TestListGuidToRun, bool allowOtherTestListsToRun, bool runListInParallel)
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

                var workItem = new TestListWorkItem(list.Guid, testRunGuids, allowOtherTestListsToRun, runListInParallel);
                var token = new CancellationTokenSource();
                RunningTestListTokens.Add(workItem.TestListGuid, token);
                Task t = new Task((i) => { TestListWorker(i, token.Token); }, workItem, token.Token);
                t.Start();
            }
            return true;
        }

        public class TestListWorkItem
        {
            public TestListWorkItem(Guid testListGuid, List<Guid> testRunGuids, bool allowOtherTestListsToRun, bool runListInParallel)
            {
                TestListGuid = testListGuid;
                AllowOtherTestListsToRun = allowOtherTestListsToRun;
                RunListInParallel = runListInParallel;
                TestRunGuids = testRunGuids;
            }

            public Guid TestListGuid;
            public bool AllowOtherTestListsToRun;
            public bool RunListInParallel;
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
                    StartTestRunLock.Wait(token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                usedSem = true;
            }
            else
            {
                usedSem = StartTestRunLock.Wait(0);
            }

            if (!token.IsCancellationRequested)
            {
                StartTestRuns(item.TestRunGuids, token, item.RunListInParallel);

                lock (RunningTestListLock)
                {
                    RunningTestListTokens.Remove(item.TestListGuid);
                }
                if (usedSem)
                {
                    StartTestRunLock.Release();
                }
            }
        }

        private void StartTestRuns(List<Guid> testRunGuids, CancellationToken token, bool runInParallel = false, TestRunnerEventHandler testRunEventHandler = null)
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
                    StartTest(testRun, token, testRunEventHandler);
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
                });
            }
        }

        private void StartTest(TestRun_Server testRun, CancellationToken token, TestRunnerEventHandler testRunEventHandler = null)
        {
            if (!token.IsCancellationRequested)
            {
                if (testRun.RunByServer)
                {
                    TestRunner runner = new TestRunner(testRun);
                    if (testRunEventHandler != null)
                    {
                        runner.OnTestEvent += testRunEventHandler;
                    }

                    if (!runner.RunTest())
                    {
                        throw new Exception(String.Format("Unable to start TestRun {0} for test {1}", testRun.Guid, testRun.TestName));
                    }

                    // Run test, waiting for it to finish or be aborted
                    bool done = false;
                    while (!token.IsCancellationRequested && !done)
                    {
                        done = runner.WaitForExit(0);
                        // todo: replace with a signal mechanism
                        Thread.Sleep(1000);
                    }
                    if (token.IsCancellationRequested)
                    {
                        runner.StopTest();
                    }
                }
                else
                {
                    testRun.StartWaitingForExternalResult();
                    OnTestManagerEvent?.Invoke(this, new TestManagerEventArgs(TestManagerEventType.WaitingForExternalTestRunResult, testRun.Guid));

                    while (!testRun.TestRunComplete)
                    {
                        // todo: replace with a signal mechanism
                        Thread.Sleep(1000);
                    }

                    testRun.EndWaitingForExternalResult();
                    OnTestManagerEvent?.Invoke(this, new TestManagerEventArgs(TestManagerEventType.ExternalTestRunFinished, testRun.Guid));
                }
            }
        }

        public void Abort()
        {
            lock (RunningTestListLock)
            {
                foreach (var token in RunningTestListTokens.Values)
                {
                    token.Cancel();
                }
                RunningTestListTokens.Clear();
            }
        }

        public void Abort(Guid testListToCancel)
        {
            lock (RunningTestListLock)
            {
                CancellationTokenSource token;
                if (RunningTestListTokens.TryGetValue(testListToCancel, out token))
                {
                    token.Cancel();
                    RunningTestListTokens.Remove(testListToCancel);
                }
            }
        }

        public TestRun RunExecutableOutsideTestList(string exeFilePath, string arguments, string consoleLogFilePath = null)
        {
            var expandedPath = Environment.ExpandEnvironmentVariables(exeFilePath);
            if (!File.Exists(expandedPath))
            {
                // TODO: log error
                return null;
            }

            // TODO: enable canceling the test
            var run = TestRun_Server.CreateTestRunWithoutTest(expandedPath, arguments, consoleLogFilePath);
            Task t = new Task(() => { StartTest(run, new CancellationToken());});
            t.Start();

            return run;
        }

        public TestRun RunTestOutsideTestList(Guid executableTestGuid)
        {
            var test = KnownTestLists.SelectMany(x => x.Value.Tests.Values).Where(y => y.Guid.Equals(executableTestGuid)).DefaultIfEmpty(null).First();

            if (test == null)
            {
                // TODO: log error
                return null;
            }

            // TODO: enable canceling the test
            var runGuid = test.CreateTestRun(DefaultLogFolder);
            var run = TestRun_Server.GetTestRunByGuid(runGuid);
            Task t = new Task(() => { StartTest(run, new CancellationToken()); });
            t.Start();

            return run;
        }

        private Dictionary<Guid, TestList> KnownTestLists;
        private Dictionary<Guid, CancellationTokenSource> RunningTestListTokens;
        private readonly object KnownTestListLock = new object();
        private readonly object RunningTestListLock = new object();
        private readonly SemaphoreSlim StartTestRunLock;
        private string _defaultLogFolder;

        public string DefaultLogFolder { get => _defaultLogFolder; }

        public event TestManagerEventHandler OnTestManagerEvent;
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
                ActiveTestRun.TestStatus = TestStatus.TestRunning;
                ActiveTestRun.TimeStarted = DateTime.Now;

                // Start async read (OnOutputData, OnErrorData)
                TestProcess.BeginErrorReadLine();
                TestProcess.BeginOutputReadLine();
            }
            else
            {
                ActiveTestRun.TestStatus = TestStatus.TestFailed;
                // TODO: log error
            }

            ActiveTestRun.UpdateOwningTestFromTestRun();

            return IsRunning;
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
            // Save the result of the test
            if (!TestAborted)
            {
                ActiveTestRun.TimeFinished = DateTime.Now;
                ActiveTestRun.ExitCode = TestProcess.ExitCode;
                ActiveTestRun.TestStatus = (ActiveTestRun.ExitCode == 0) ? TestStatus.TestPassed : TestStatus.TestFailed;
            }
            else
            {
                ActiveTestRun.ExitCode = -1;
                ActiveTestRun.TestStatus = TestStatus.TestAborted;
            }
            ActiveTestRun.UpdateOwningTestFromTestRun();

            // Save test output to file
            var LogFilePath = ActiveTestRun.ConsoleLogFilePath;
            if (LogFilePath != null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath));
                File.WriteAllLines(LogFilePath,
                                    new String[] { String.Format("Test: {0}", ActiveTestRun.TestName),
                                String.Format("Result: {0}", ActiveTestRun.TestStatus),
                                String.Format("Exit code: {0}", ActiveTestRun.ExitCode),
                                String.Format("Date/Time run: {0}", ActiveTestRun.TimeStarted),
                                String.Format("Time to complete: {0}", ActiveTestRun.RunTime),
                                String.Format("---------Test's console output below--------")});
                File.AppendAllLines(LogFilePath, ActiveTestRun.TestOutput, System.Text.Encoding.UTF8);
            }

            IsRunning = false;

            // Raise event if event handler exists
            OnTestEvent?.Invoke(this, new TestRunnerEventArgs(ActiveTestRun.TestStatus, (int)ActiveTestRun.ExitCode, null));
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
        //public ExecutableTest Test { get; }

        public event TestRunnerEventHandler OnTestEvent;

        private TestRun_Server ActiveTestRun;
        private Process TestProcess;
        private bool TestAborted;

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

        public static TestRun_Server CreateTestRunWithoutTest(string filePath, string arguments, string logFileOrFolder)
        {
            return new TestRun_Server(filePath, arguments, logFileOrFolder);
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

        public static bool UpdateTestRun(TestRun updatedTestRun, TestBase test)
        {
            if (updatedTestRun == null)
            {
                return false;
            }

            if (test == null)
            {
                return false;
            }

            lock (_testMapLock)
            {
                if (!_testRunMap.ContainsKey(updatedTestRun.Guid))
                {
                    return false;
                }
                // TODO: Must fix!
                var run = _testRunMap[updatedTestRun.Guid];
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
        /// <param name="filePath"></param>
        /// <param name="arguments"></param>
        /// <param name="logFileOrPath"></param>
        private TestRun_Server(string filePath, string arguments, string logFileOrPath) : base(null)
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
            _testPath = filePath;
            _arguments = arguments;
            _testName = Path.GetFileName(filePath);
            _testType = TestType.ConsoleExe;
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
            TestStatus = TestStatus.TestWaitingForExternalResult;
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