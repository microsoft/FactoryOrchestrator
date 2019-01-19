using System;
using System.Diagnostics;
using FTFTestLibrary;

namespace FTFTestExecution
{
    public enum TestEventEnum
    {
        TestPassed,
        TestFailed,
        TAEFTestCasePassed,
        TAEFTestCaseFailed,
        TestWarning,
        TestError,
        TestException,
        TestAborted
    }

    public delegate void TestRunEventHandler(object source, TestRunEvent e);

    public class TestRunEvent : EventArgs
    {
        public TestEventEnum EventType { get; set; }
        public int EventStatusCode { get; set; }
        public String EventMessage { get; set; }
    }

    public class TestRunExceptionEvent : TestRunEvent
    {
        public Exception EventException { get; set; }
    }

    public class TestRunner
    {
        public static String GlobalLogPath;
        public static String GlobalTeExePath;
        public static String GlobalExecutionContextPath;

        public TestRunner(FactoryTest testToRun)
        {
            TestContext = testToRun;
            IsRunning = false;
        }

        public bool RunTest()
        {
            if (IsRunning == false)
            {

            }
            else
            {
                TestProcess = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();

                if (TestContext.IsTAEF)
                {
                    startInfo.FileName = GlobalTeExePath;
                    startInfo.Arguments += TestContext.TestPath;
                }
                else
                {
                    startInfo.FileName = TestContext.TestPath;
                }

                foreach (var arg in TestContext.Arguments)
                {
                    startInfo.Arguments += " " + arg;
                }

                // Configure common StartInfo parameters
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardInput = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
            }
        }

        public bool StopTest()
        {
        }

        public bool IsRunning { get; }
        private TimeSpan _elapsedTime;
        public FactoryTest TestContext { get; }
        public event TestRunEventHandler OnTestEvent;
        public uint TestProcessPID;
        public Process TestProcess;
    }
}
