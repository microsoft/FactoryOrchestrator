using FTFSharedLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// todo: split into client lib, shared lib, server lib
namespace FTFServerLibrary
{
    public static class TestBase_ServerExtensions
    {
        public static Guid CreateTestRun(this TestBase test)
        {
            TestRun_Server run;
            lock (test.TestLock)
            {
                run = new TestRun_Server(test);
                test.TestRunGuids.Add(run.Guid);
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
                test.LastRunStatus = TestStatus.TestNotRun;
                test.LastExitCode = null;
                test.LastTimeFinished = null;
                test.LastTimeStarted = null;
                test.TestRunGuids = new List<Guid>();
            }

            TestRun_Server.RemoveTestRunsForTest(test.Guid, preserveLogs);
        }
    }

    public class TestManager_Server // todo: server lib
    {
        public TestManager_Server()
        {
            StartTestRunLock = new SemaphoreSlim(1, 1);
            KnownTestLists = new Dictionary<Guid, TestList>();
            RunningTestListTokens = new Dictionary<Guid, CancellationTokenSource>();
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
            return KnownTestLists.Remove(listToDelete);
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
            lock (RunningTestLock)
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

        public bool UpdateTestList(TestList testList)
        {
            if (testList == null)
            {
                return false;
            }

            // todo: what if it is running or queued to run?
            lock (KnownTestListLock)
            {
                if (KnownTestLists.ContainsKey(testList.Guid))
                {
                    KnownTestLists[testList.Guid] = testList;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a DLL is a TAEF test. Returns an initialized TAEFTest instance if it is.
        /// </summary>
        /// <param name="dllToTest">Path to DLL to check</param>
        /// <returns>null if DLL is not a TAEF test. TAEFTest instance if it is.</returns>
        public static TAEFTest CheckForTAEFTest(String dllToTest)
        {
            // Try to list TAEF testcases to see if it is a valid TAEF test
            TAEFTest maybeTAEF = new TAEFTest(dllToTest);
            TestRun_Server testRun = new TestRun_Server(maybeTAEF);
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
                    throw new Exception(String.Format("TE.exe returned error {0} when trying to validate possible TAEF test: {1}", maybeTAEF.LastExitCode, dllToTest));
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

        public bool UpdateTestStatus(TestBase latestTestStatus)
        {
           if (latestTestStatus == null)
           {
                return false;
           }

            // Changing state, lock the test lists
            lock (KnownTestListLock)
            {
                // Find lists with this test in them
                // TODO: I think we are converging on unique guids per item in a list, not unique guid per test (ie, foo.dll in two lists has two unique guids)
                // should this only get first() and error otherwise?
                var listsToUpdate = KnownTestLists.Values.Where(x => x.Tests.ContainsKey(latestTestStatus.Guid));

                if ((listsToUpdate == null) || (listsToUpdate.Count() == 0))
                {
                    return false;
                }
                else
                {
                    // Iterate through all lists with this test and update them
                    foreach (var list in listsToUpdate)
                    {
                        var test = list.Tests[latestTestStatus.Guid];
                        // Replace existing Test with the Test we were given
                        lock (test.TestLock)
                        {
                            if (test.LastRunStatus == TestStatus.TestRunning)
                            {
                               return false;
                            }
                            list.Tests[latestTestStatus.Guid] = latestTestStatus;
                        }
                    }
                    return true;
                }
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
            lock (RunningTestLock)
            {
                if (RunningTestListTokens.ContainsKey(TestListGuidToRun))
                {
                    return false;
                }

                // Create testrun for all tests in the list
                List<Guid> testRunGuids = new List<Guid>();
                foreach (var test in list.Tests.Values)
                {
                    testRunGuids.Add(test.CreateTestRun());
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

                lock (RunningTestLock)
                {
                    RunningTestListTokens.Remove(item.TestListGuid);
                }
                if (usedSem)
                {
                    StartTestRunLock.Release();
                }
            }
        }

        private void StartTestRuns(List<Guid> testRunGuids, CancellationToken token, bool runInParallel = false, TestRunEventHandler testRunEventHandler = null)
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
                    if(testRun.OwningTest.RunByServer)
                    {
                        StartTest(testRun, token, testRunEventHandler);
                    }
                    else
                    {
                        // TODO: Notify waiting for UWP result
                        while (!testRun.TestRunComplete)
                        {
                            Thread.Sleep(1000);
                        }
                    }
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
                    if (testRun.OwningTest.RunByServer)
                    {
                        StartTest(testRun, token, testRunEventHandler);
                    }
                    else
                    {
                        // TODO: Notify waiting for UWP result
                        while (!testRun.TestRunComplete)
                        {
                            Thread.Sleep(1000);
                        }
                    }
                });
            }
        }

        private static void StartTest(TestRun_Server testRun, CancellationToken token, TestRunEventHandler testRunEventHandler = null)
        {
            if (!token.IsCancellationRequested)
            {
                TestRunner runner = new TestRunner(testRun);
                if (testRunEventHandler != null)
                {
                    runner.OnTestEvent += testRunEventHandler;
                }

                if (!runner.RunTest())
                {
                    throw new Exception(String.Format("Unable to start TestRun {0} for test {1}", testRun.Guid, testRun.OwningTest.TestName));
                }

                // Run test, waiting for it to finish or be aborted
                bool done = false;
                while (!token.IsCancellationRequested && !done)
                {
                    done = runner.WaitForExit(0);
                    Thread.Sleep(1000);
                }
                if (token.IsCancellationRequested)
                {
                    runner.StopTest();
                }
            }
        }

        public void Abort()
        {
            lock (RunningTestLock)
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
            lock (RunningTestLock)
            {
                CancellationTokenSource token;
                if (RunningTestListTokens.TryGetValue(testListToCancel, out token))
                {
                    token.Cancel();
                    RunningTestListTokens.Remove(testListToCancel);
                }
            }
        }

        private Dictionary<Guid, TestList> KnownTestLists;
        private Dictionary<Guid, CancellationTokenSource> RunningTestListTokens;
        private readonly object KnownTestListLock = new object();
        private readonly object RunningTestLock = new object();
        private readonly SemaphoreSlim StartTestRunLock;
    }

    public delegate void TestRunEventHandler(object source, TestRunEventArgs e);

    public class TestRunEventArgs : EventArgs
    {
        public TestRunEventArgs(TestStatus testStatus, int eventStatusCode, String eventMessage)
        {
            TestStatus = testStatus;
            EventStatusCode = eventStatusCode;
            EventMessage = eventMessage;
        }

        public TestStatus TestStatus { get; }
        public int EventStatusCode { get; }
        public String EventMessage { get; }
    }

    public class TestRunExceptionEvent : TestRunEventArgs
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
        public static string GlobalTeExePath = "c:\\taef\\te.exe";
        public static string GlobalLogFolder = "c:\\data\\FTFLogs";
        private readonly static string GlobalTeArgs = "";

        public static bool SetDefaultTePath(string teExePath)
        {
            if (File.Exists(teExePath))
            {
                GlobalTeExePath = teExePath;
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool SetDefaultLogFolder(string logFolder)
        {
            try
            {
                Directory.CreateDirectory(logFolder);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public TestRunner(TestRun_Server testRun)
        {
            // TODO: Lock test to prevent a client from changing args etc when in the middle of creating a testrunner. might not be needed if args and binary name are GET only
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
            ActiveTestRun.LogFilePath = null;
            lock (runnerStateLock)
            {
                if (IsRunning == true)
                {
                    return true;
                }

                // Create Process object
                TestProcess = CreateTestProcess();

                // Override args to check if this is a valid TAEF test, not try to run it
                TestProcess.StartInfo.Arguments = ActiveTestRun.OwningTest.TestPath + " /list";

                // Start the process
                return StartTestProcess();
            }
        }

        private Process CreateTestProcess()
        {
            TestProcess = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();

            if (ActiveTestRun.OwningTest.TestType == TestType.TAEFDll)
            {
                startInfo.FileName = GlobalTeExePath;
                startInfo.Arguments += ActiveTestRun.OwningTest.TestPath + GlobalTeArgs;
            }
            else
            {
                startInfo.FileName = ActiveTestRun.OwningTest.TestPath;
            }

            startInfo.Arguments += ActiveTestRun.OwningTest.Arguments;

            // Configure IO redirection
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.CreateNoWindow = true;
            if (ActiveTestRun.OwningTest.TestType == TestType.TAEFDll)
            {
                startInfo.Environment["Path"] = startInfo.Environment["Path"] + ";" + Path.GetDirectoryName(ActiveTestRun.OwningTest.TestPath);
                startInfo.WorkingDirectory = Path.GetDirectoryName(startInfo.FileName);
            }
            else
            {
                startInfo.WorkingDirectory = Path.GetDirectoryName(ActiveTestRun.OwningTest.TestPath);
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
                ActiveTestRun.TestStatus = TestStatus.TestRunning;
                ActiveTestRun.TimeStarted = DateTime.Now;

                // Start async read (OnOutputData, OnErrorData)
                TestProcess.BeginErrorReadLine();
                TestProcess.BeginOutputReadLine();

                return true;
            }
            else
            {
                ActiveTestRun.TestStatus = TestStatus.TestFailed;
                // TODO: log error
            }

            return false;
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

                if (ActiveTestRun.OwningTest.TestType == TestType.TAEFDll)
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
            
            // Save test output to file
            var LogFilePath = ActiveTestRun.LogFilePath;
            if (LogFilePath != null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath));
                File.WriteAllLines(LogFilePath,
                                    new String[] { String.Format("Test: {0}", ActiveTestRun.OwningTest.TestName),
                                        String.Format("Result: {0}", ActiveTestRun.TestStatus),
                                        String.Format("Exit code: {0}", ActiveTestRun.ExitCode),
                                        String.Format("Date/Time run: {0}", ActiveTestRun.TimeStarted),
                                        String.Format("Time to complete: {0}", ActiveTestRun.RunTime),
                                        String.Format("---------Test's console output below--------")});
                File.AppendAllLines(LogFilePath, ActiveTestRun.TestOutput, System.Text.Encoding.UTF8);
            }

            IsRunning = false;

            // Raise event if event handler exists
            OnTestEvent?.Invoke(this, new TestRunEventArgs(ActiveTestRun.TestStatus, (int)ActiveTestRun.ExitCode, null));
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
            if (IsRunning)
            {
                return TestProcess.WaitForExit(milliseconds);
            }
            else
            {
                return true;
            }
        }

        public bool IsRunning { get; set; }
        //public ExecutableTest Test { get; }
        
        public event TestRunEventHandler OnTestEvent;

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
                        if (File.Exists(testRun.LogFilePath))
                        {
                            File.Delete(testRun.LogFilePath);
                        }
                    }
                }
            }
        }

        public static void RemoveTestRunsForTest(Guid testGuid, bool preserveLogs)
        {
            Parallel.ForEach(GetTestRunGuidsByTestGuid(testGuid), testRunGuid =>
            {
                RemoveTestRun(testRunGuid, preserveLogs);
            });
        }

        public TestRun_Server(TestBase owningTest) : base(owningTest)
        {
            lock (_testMapLock)
            {
                _testRunMap.Add(Guid, this);
            }

            OwningTest = owningTest;

            string LogFolder = OwningTest.LogFolder;
            if (LogFolder == null)
            {
                LogFolder = TestRunner.GlobalLogFolder;
            }

            LogFilePath = Path.Combine(LogFolder, String.Format("{0}_Run{1}", OwningTest.TestName, Guid));
        }

        /// <summary>
        /// Tracks all the test runs that have ever occured, mapped by the test run GUID
        /// </summary>
        private static Dictionary<Guid, TestRun_Server> _testRunMap;

        private static object _testMapLock;

        public TestBase OwningTest;
    }
}
