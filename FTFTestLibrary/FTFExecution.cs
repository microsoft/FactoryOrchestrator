﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace FTFTestExecution
{
    public class TestManager
    {
        public static TestList EnumerateTests(String path, bool onlyTAEF)
        {
            // Recursive search for all exe and dll files
            var exes = Directory.EnumerateFiles(path, "*.exe", SearchOption.AllDirectories);
            var dlls = Directory.EnumerateFiles(path, "*.dll", SearchOption.AllDirectories);
            TestList tests = new TestList();

            foreach (var dll in dlls)
            {
                var maybeTAEF = CheckForTAEFTest(dll);
                if (maybeTAEF != null)
                {
                    tests.Tests.Add(maybeTAEF);
                }
            }

            // Assume every .exe is a valid console test
            if (!onlyTAEF)
            {
                foreach (var exe in exes)
                {
                    tests.Tests.Add(new FactoryTest(exe));
                }
            }

            return tests;
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
                throw new Exception(String.Format("Unable to validate possible TAEF test: {0}", dllToTest), e);
            }
        }

        //public static string GetTAEFTestName(String WttLogPath)
        //{
        //}

        public TestManager()
        {

        }

        public static bool RunTestList(TestList list, bool runInParallel = false, TestRunEventHandler testRunEventHandler = null)
        {
            foreach (FactoryTest test in list)
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

                if (!runInParallel)
                {
                    runner.WaitForExit();
                }
                else
                {
                    // TODO: Implement
                    throw new Exception(String.Format("Parallel execution not yet supported"));
                }
            }

            foreach (FactoryTest test in list)
            {
                if (test.ExitCode != 0)
                {
                    return false;
                }
            }

            return true;
        }

        public void Abort(TestList list)
        {
            foreach (var test in list.Tests.Where(x => (x.TestRunner != null) && (x.TestRunner.IsRunning)))
            {
                test.TestRunner.StopTest();
            }
        }
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
        private Mutex outputMutex = new Mutex();

        public TestRunner(FactoryTest testToRun)
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

            if (TestContext.IsTAEF)
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
            // Use mutex to ensure output data is properly serialized
            outputMutex.WaitOne();
            TestOutput.Add(e.Data);

            if (TestContext.IsTAEF)
            {
                ParseTeOutput(e.Data);
            }
            outputMutex.ReleaseMutex();
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
        public FactoryTest TestContext { get; }
        public event TestRunEventHandler OnTestEvent;
        public Process TestProcess;
        private bool TestAborted;
        public List<String> TestOutput;
        internal Stopwatch _timer; // The Process class counter stops working once the process exits so make our own timer
    }
}
