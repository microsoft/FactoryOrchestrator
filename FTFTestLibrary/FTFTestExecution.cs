using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// todo: split into client lib, shared lib, server lib
namespace FTFTestExecution
{
    public class TestManager // todo: server lib
    {
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

        public void Reset()
        {
            lock (RunningTestLock)
            {
                lock (KnownTestListLock)
                {
                    // Cancel all running tests, create new dictionaries
                    foreach (var token in RunningTestListTokens.Values)
                    {
                        token.Cancel();
                    }
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
            TestRunner runner = new TestRunner(ref maybeTAEF);
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

                
                // "No tests were executed." error, returned when a binary is not a valid TAEF test.
                // todo: but not always???
                // https://docs.microsoft.com/en-us/windows-hardware/drivers/taef/exit-codes-for-taef
                if ((maybeTAEF.ExitCode == 117440512) || (maybeTAEF.ExitCode == 0))
                {
                    // Check if it was able to enumerate the test cases.
                    if (!maybeTAEF.TestOutput.Any(x => x.Contains("Summary of Errors Outside of Tests")) && !maybeTAEF.TestOutput.Any(x => x.Contains("Failed to load")))
                    {
                        // TODO: We need a better mechanism here
                        maybeTAEF.Reset();
                        return maybeTAEF;
                    }
                    return null;
                }
                else
                {
                    throw new Exception(String.Format("TE.exe returned error {0} when trying to validate possible TAEF test: {1}", maybeTAEF.ExitCode, dllToTest));
                }
            }
            catch (Exception)
            {
                // TODO: undo this
                //throw new Exception(String.Format("Unable to validate possible TAEF test: {0}", dllToTest), e);
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
                            if (test.TestStatus == TestStatus.TestRunning)
                            {
                                if (test.UsesTestRunner)
                                {
                                    if(!((ExecutableTest)test).TestRunner.StopTest())
                                    {
                                        // Since GUIDs match error out on the first one, the tests with matching GUIDs better be the same object
                                        return false;
                                    }
                                }
                            }
                            list.Tests[latestTestStatus.Guid] = latestTestStatus;
                        }
                    }
                    return true;
                }
            }
        }

        public TestManager()
        {
            StartTestRunLock = new SemaphoreSlim(1, 1);
            KnownTestLists = new Dictionary<Guid, TestList>();
            RunningTestListTokens = new Dictionary<Guid, CancellationTokenSource>();
            TestEvents = new Dictionary<Guid, Queue<TestRunEventArgs>>();
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
                RunTestList(item.TestList, token, item.RunListInParallel);

                lock (RunningTestLock)
                {
                    RunningTestListTokens.Remove(item.TestList.Guid);
                }
                if (usedSem)
                {
                    StartTestRunLock.Release();
                }
            }
        }

        public class TestListWorkItem
        {
            public TestListWorkItem(TestList testList, bool allowOtherTestListsToRun, bool runListInParallel)
            {
                TestList = testList;
                AllowOtherTestListsToRun = allowOtherTestListsToRun;
                RunListInParallel = runListInParallel;
            }

            public TestList TestList;
            public bool AllowOtherTestListsToRun;
            public bool RunListInParallel;
        }

        public bool Run(Guid TestListGuidToRun, bool allowOtherTestListsToRun, bool runListInParallel)
        {
            TestList list = null;

            // Check if test list is valid
            lock (KnownTestListLock)
            {
                if (!KnownTestLists.ContainsKey(TestListGuidToRun))
                {
                    return false;
                }

                list = KnownTestLists[TestListGuidToRun];
            }

            // Check if test list is already running or set to be run
            lock (RunningTestLock)
            {
                if (RunningTestListTokens.ContainsKey(TestListGuidToRun))
                {
                    return true;
                }

                var workItem = new TestListWorkItem(list, allowOtherTestListsToRun, runListInParallel);
                var token = new CancellationTokenSource();
                RunningTestListTokens.Add(workItem.TestList.Guid, token);
                Task t = new Task((i) => { TestListWorker(i, token.Token); }, workItem, token.Token);
                t.Start();
            }
            return true;
        }

        private void RunTestList(TestList list, CancellationToken token, bool runInParallel = false, TestRunEventHandler testRunEventHandler = null)
        {
            // Run all enabled tests in the list
            var enumerator = list.Tests.Values.Where(x => x.IsEnabled == true);
            if (!runInParallel)
            {
                foreach (TestBase test in enumerator)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    if(test.UsesTestRunner)
                    {
                        RunTest((ExecutableTest)test, token, testRunEventHandler);
                    }
                    else
                    {
                        // TODO: Wait for UWP result
                    }
                }
            }
            else
            {
                Parallel.ForEach<TestBase>(enumerator, (test, state) =>
                {
                    if (token.IsCancellationRequested)
                    {
                        state.Stop();
                    }

                    if (test.UsesTestRunner)
                    {
                        RunTest((ExecutableTest)test, token, testRunEventHandler);
                    }
                    else
                    {
                        // TODO: Notify waiting on UWP????
                    }
                });
            }
        }

        private static void RunTest(ExecutableTest test, CancellationToken token, TestRunEventHandler testRunEventHandler = null)
        {
            if (!token.IsCancellationRequested)
            {
                TestRunner runner = new TestRunner(ref test);
                if (testRunEventHandler != null)
                {
                    runner.OnTestEvent += testRunEventHandler;
                }

                if (!runner.RunTest())
                {
                    throw new Exception(String.Format("Unable to start test {0}", test));
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
        private Dictionary<Guid, Queue<TestRunEventArgs>> TestEvents;
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
        private object outputLock = new object();

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

        public TestRunner(ref ExecutableTest testToRun)
        {
            TestContext = testToRun;
            IsRunning = false;
        }

        public TestRunner(ref TAEFTest maybeTAEF)
        {
            TestContext = maybeTAEF;
        }

        public bool RunTest()
        {
            lock (TestContext.TestLock)
            {
                if (IsRunning == true)
                {
                    return true;
                }

                // Create test run
                // TODO: Testprocess in testrun?
                TestRun run = new TestRun(TestContext.Guid);

                // Create Process object
                TestProcess = CreateTestProcess();

                // Start the process
                return StartTestProcess();
            }
        }

        internal bool CheckIfTaefTest()
        {
            lock (TestContext.TestLock)
            {
                if (IsRunning == true)
                {
                    return true;
                }

                // Create Process object
                TestProcess = CreateTestProcess();

                // Override args to check if this is a valid TAEF test, not try to run it
                TestProcess.StartInfo.Arguments = TestContext.TestPath + " /list";

                // Start the process
                return StartTestProcess();
            }
        }

        private Process CreateTestProcess()
        {
            TestProcess = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();

            if (TestContext.TestType == TestType.TAEFDll)
            {
                startInfo.FileName = GlobalTeExePath;
                startInfo.Arguments += TestContext.TestPath + GlobalTeArgs;
            }
            else
            {
                startInfo.FileName = TestContext.TestPath;
            }

            startInfo.Arguments += TestContext.Arguments;

            // Configure IO redirection
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.CreateNoWindow = true;
            if (TestContext.TestType == TestType.TAEFDll)
            {
                startInfo.Environment["Path"] = startInfo.Environment["Path"] + ";" + Path.GetDirectoryName(startInfo.FileName);
            }
            else
            {
                startInfo.WorkingDirectory = Path.GetDirectoryName(TestContext.TestPath);
            }
            TestContext.TestOutput = new List<string>();

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

                // Start async read (OnOutputData)
                TestProcess.BeginErrorReadLine();
                TestProcess.BeginOutputReadLine();
                // Start timer
                TestContext.TestRunner = this;
                TestContext.TestStatus = TestStatus.TestRunning;
                TestContext.LastTimeStarted = DateTime.Now;
                return true;
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
                TestContext.TestOutput.Add(line);

                if (TestContext.TestType == TestType.TAEFDll)
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
                TestContext.LastTimeFinished = DateTime.Now;
                TestContext.ExitCode = TestProcess.ExitCode;
                TestContext.TestStatus = (TestContext.ExitCode == 0) ? TestStatus.TestPassed : TestStatus.TestFailed;
            }
            else
            {
                TestContext.ExitCode = -1;
                TestContext.TestStatus = TestStatus.TestAborted;
            }

            // Save test output to file
            var LogFilePath = TestContext.LogFilePath;
            if (LogFilePath == null)
            {
                LogFilePath = Path.Combine(GlobalLogFolder, TestContext.TestName + ".log");
            }

            uint i = 2;
            while (File.Exists(LogFilePath))
            {
                LogFilePath = String.Format("{0}_Run{1}{2}", LogFilePath.Substring(0, LogFilePath.Length - 4), i, ".log");
            }

            TestContext.LogFilePath = LogFilePath;

            Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath));
            File.WriteAllLines(LogFilePath,
                                new String[] { String.Format("Test: {0}", TestContext.TestName),
                                            String.Format("Result: {0}", TestContext.TestStatus),
                                            String.Format("Exit code: {0}", TestContext.ExitCode),
                                            String.Format("Date/Time run: {0}", TestContext.LastTimeStarted),
                                            String.Format("Time to complete: {0}", TestContext.TestRunTime),
                                            String.Format("---------Test's console output below--------")});
            File.AppendAllLines(LogFilePath, TestContext.TestOutput, System.Text.Encoding.UTF8);

            IsRunning = false;
            // Raise event if event handler exists
            OnTestEvent?.Invoke(this, new TestRunEventArgs(TestContext.TestStatus, (int)TestContext.ExitCode, null));
        }

        public bool StopTest()
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
        public ExecutableTest TestContext { get; }
        public event TestRunEventHandler OnTestEvent;
        public Process TestProcess;
        private bool TestAborted;
    }
}
