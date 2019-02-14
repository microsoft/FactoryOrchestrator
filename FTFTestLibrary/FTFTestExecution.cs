﻿using System;
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

            foreach (var dll in dlls)
            {
                var maybeTAEF = CheckForTAEFTest(dll);
                if (maybeTAEF != null)
                {
                    tests.Tests.Add(maybeTAEF.Guid, new Tuple<ExecutableTest, bool>(maybeTAEF, true));
                }
            }

            // Assume every .exe is a valid console test
            if (!onlyTAEF)
            {
                foreach (var exe in exes)
                {
                    var test = new ExecutableTest(exe);
                    tests.Tests.Add(test.Guid, new Tuple<ExecutableTest, bool>(test, true));
                }
            }

            lock (KnownTestListLock)
            {
                KnownTestLists.Add(tests.Guid, tests);
            }
            return tests;
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

        /// <summary>
        /// Checks if a DLL is a TAEF test. Returns an initialized TAEFTest instance if it is.
        /// </summary>
        /// <param name="dllToTest">Path to DLL to check</param>
        /// <returns>null if DLL is not a TAEF test. TAEFTest instance if it is.</returns>
        public static TAEFTest CheckForTAEFTest(String dllToTest)
        {
            // Try to list TAEF testcases to see if it is a valid TAEF test
            TAEFTest maybeTAEF = new TAEFTest(dllToTest);
            TestRunner runner = new TestRunner(maybeTAEF);
            try
            {
                if (!runner.RunTest(new List<string>() { "/list" }))
                {
                    throw new Exception(String.Format("Unable to invoke TE.exe to validate possible TAEF test: {0}", dllToTest));
                }
                // Timeout after a second
                if (!runner.WaitForExit(1000))
                {
                    runner.StopTest();
                    throw new Exception(String.Format("TE.exe timed out trying to validate possible TAEF test: {0}", dllToTest));
                }

                // If it exits successfully, it was able to enumerate the test cases.
                if (maybeTAEF.ExitCode == 0)
                {
                    maybeTAEF.Reset();
                    return maybeTAEF;
                }
                // "No tests were executed." error, returned when a binary is not a valid TAEF test.
                // https://docs.microsoft.com/en-us/windows-hardware/drivers/taef/exit-codes-for-taef
                else if (maybeTAEF.ExitCode == 117440512)
                {
                    return null;
                }
                else
                {
                    throw new Exception(String.Format("TE.exe returned error {0} when trying to validate possible TAEF test: {1}", maybeTAEF.ExitCode, dllToTest));
                }
            }
            catch (Exception e)
            {
                //throw new Exception(String.Format("Unable to validate possible TAEF test: {0}", dllToTest), e);
                return null;
            }
        }

        public TestManager()
        {
            StartTestRunLock = new SemaphoreSlim(1, 1);
            KnownTestLists = new Dictionary<Guid, TestList>();
            RunningTestListTokens = new Dictionary<Guid, CancellationTokenSource>();
            TestEvents = new Dictionary<Guid, Queue<TestRunEventArgs>>();
        }

        //private void TestListQueueManager()
        //{
        //    while (true)
        //    {
        //        while (TestListRunQueue.Count > 0)
        //        {
        //            var run = TestListRunQueue.Dequeue();
        //            var token = new CancellationTokenSource();
        //            RunningTestListTokens.Add(run.TestList.Guid, token);
        //            Task t = new Task((i) => { TestListWorker(i, token.Token); },  run, token.Token);
        //            t.Start();
        //        }

        //        Thread.Sleep(1000);
        //    }
        //}

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
            var enumerator = list.Tests.Values.Where(x => x.Item2 == true).Select(x => x.Item1);
            if (!runInParallel)
            {
                foreach (ExecutableTest test in enumerator)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                    else
                    {
                        RunTest(test, token, testRunEventHandler);
                    }
                }
            }
            else
            {
                Parallel.ForEach<ExecutableTest>(enumerator, (test, state) =>
                {
                    if (token.IsCancellationRequested)
                    {
                        state.Stop();
                    }
                    else
                    {
                        RunTest(test, token, testRunEventHandler);
                    }
                });
            }
        }

        private static void RunTest(ExecutableTest test, CancellationToken token, TestRunEventHandler testRunEventHandler = null)
        {
            if (!token.IsCancellationRequested)
            {
                TestRunner runner = new TestRunner(test);
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
                    Thread.Sleep(500);
                }
                if (token.IsCancellationRequested)
                {
                    runner.StopTest();
                }
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
        //public static String GlobalLogPath;
        public static String GlobalTeExePath = "c:\\taef\\te.exe";
        //public static String GlobalExecutionContextPath;
        private readonly static String GlobalTeArgs = " /labMode /enableWttLogging /logOutput:High /console:flushWrites /coloredConsoleOutput:false";
        private object outputLock = new object();

        public TestRunner(ExecutableTest testToRun)
        {
            TestContext = testToRun;
            IsRunning = false;
            _timer = new Stopwatch();
        }

        public bool RunTest()
        {
            return RunTest(null);
        }
        public bool RunTest(List<String> overrideArguments)
        {
            if (IsRunning == true)
            {
                return true;
            }

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

            if (overrideArguments != null)
            {
                foreach (var arg in overrideArguments)
                {
                    startInfo.Arguments += " " + arg;
                }
            }

            // Configure IO redirection
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.CreateNoWindow = true;
            startInfo.WorkingDirectory = Path.GetDirectoryName(TestContext.TestPath);

            // Configure event handling
            TestProcess.EnableRaisingEvents = true;
            TestProcess.Exited += OnExited;
            TestProcess.OutputDataReceived += OnOutputData;
            TestProcess.ErrorDataReceived += OnOutputData;

            // Start the process
            TestProcess.StartInfo = startInfo;
            if(TestProcess.Start())
            {
                IsRunning = true;
                TestOutput = new List<string>();
                // Start async read (OnOutputData)
                TestProcess.BeginErrorReadLine();
                TestProcess.BeginOutputReadLine();
                // Start timer
                _timer.Restart();
                TestContext.TestRunner = this;
                TestContext.TestStatus = TestStatus.TestRunning;
                TestContext.LastTimeRun = DateTime.Now;
                return true;
            }

            return false;
        }

        private void OnOutputData(object sender, DataReceivedEventArgs e)
        {
            // Use mutex to ensure output data is serialized
            lock (outputLock)
            {
                TestOutput.Add(e.Data);

                if (TestContext.TestType == TestType.TAEFDll)
                {
                    ParseTeOutput(e.Data);
                }
            }
        }

        private void ParseTeOutput(string data)
        {
            //throw new NotImplementedException();
        }

        private void OnExited(object sender, EventArgs e)
        {
            _timer.Stop();

            // Get result of the test
            TestContext.ExitCode = TestAborted ? -1 : TestProcess.ExitCode;
            TestContext.TestStatus = (TestContext.ExitCode == 0) ? TestStatus.TestPassed : TestStatus.TestFailed;

            // Save test output to file
            if (TestContext.LogFilePath != null)
            {
                File.WriteAllLines(TestContext.LogFilePath,
                                   new String[] { String.Format("Test: {0}", TestContext.TestName),
                                                  String.Format("Result: {0}", TestContext.TestStatus),
                                                  String.Format("Exit code: {0}", TestContext.ExitCode),
                                                  String.Format("Date/Time run: {0}", TestContext.LastTimeRun),
                                                  String.Format("Time to complete: {0}", TestContext.TestRunTime),
                                                  String.Format("---------Test's console output below--------")});
                File.AppendAllLines(TestContext.LogFilePath, TestOutput, System.Text.Encoding.UTF8);
            }

            IsRunning = false;
            // Raise event if event handler exists
            OnTestEvent?.Invoke(this, new TestRunEventArgs(TestContext.TestStatus, (int)TestContext.ExitCode, null));
        }

        public void StopTest()
        {
            // See if you get lucky and the test finishes on its own???
            if (!TestProcess.WaitForExit(500))
            {
                TestProcess.Kill();
                TestAborted = true;
            }
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
        public List<String> TestOutput;
        internal Stopwatch _timer; // The Process class counter stops working once the process exits so make our own timer
    }
}
