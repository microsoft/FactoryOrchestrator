using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace FTFTestExecution
{
    public enum TestStatus
    {
        TestPassed,
        TestFailed,
        TAEFTestCasePassed,
        TAEFTestCaseFailed,
        TestWarning,
        TestError,
        TestException,
        TestAborted,
        TestRunning,
        TestNotRun
    }

    public enum TestType
    {
        ConsoleExe,
        TAEFDll,
        UWP
    }

    public abstract class TestBase
    {
        public TestBase(string testPath, TestType type)
        {
            Guid = Guid.NewGuid();
            TestType = type;
            TestPath = testPath;
        }
        
        public TestType TestType { get; }

        public List<String> Arguments { get; set; }
        public Guid Guid { get; }

        public DateTime? LastTimeRun { get; set; }
        public TestStatus TestStatus { get; set; }

        public bool? TestPassed
        {
            get
            {
                if (this.TestStatus == TestStatus.TestPassed)
                {
                    return true;
                }
                else if (this.TestStatus == TestStatus.TestFailed)
                {
                    return false;
                }
                else
                {
                    return null;
                }

            }
        }

        public int? ExitCode { get; set; }
        public String LogFilePath { get; set; }

        public String TestPath { get; }

        public virtual void Reset()
        {
            LastTimeRun = null;
            TestStatus = TestStatus.TestNotRun;
            ExitCode = null;
            if (LogFilePath != null)
            {
                if (File.Exists(LogFilePath))
                {
                    try
                    {
                        File.Delete(LogFilePath);
                    }
                    catch { }
                }
            }
        }
    }

    public class ExecutableTest : TestBase
    {
        public ExecutableTest(String testPath) : base(testPath, TestType.ConsoleExe)
        {
            Reset();
        }

        protected ExecutableTest(String testPath, TestType type) : base(testPath, type)
        {
            Reset();
        }

        public override string ToString()
        {
            return TestName;
        }

        //public override bool Equals(object obj)
        //{
        //    try
        //    {
        //        if (String.Compare(((FactoryTest)obj).TestPath, TestPath, true) == 0)
        //        {
        //            return true;
        //        }
        //        else
        //        {
        //            return false;
        //        }
        //    }
        //    catch (InvalidCastException)
        //    {
        //        return false;
        //    }
        //}

        public override void Reset()
        {
            base.Reset();
            TestRunner = null;
        }

        public TimeSpan TestRunTime
        {
            get
            {
                if (TestRunner != null)
                {
                    return TestRunner._timer.Elapsed;
                }
                else
                {
                    return new TimeSpan();
                }
            }
        }

        public String TestName
        {
            get
            {
                return Path.GetFileName(TestPath);
            }
        }

        [JsonIgnore]
        public TestRunner TestRunner;
    }

    public class TAEFTest : ExecutableTest
    {
        public TAEFTest(string testPath) : base(testPath, TestType.TAEFDll)
        {
        }

        private List<TAEFTestCase> _testCases;
        private String _wtlFilePath;
    }

    public class UWPTest : TestBase
    {
        public UWPTest(string packageFamilyName) : base(packageFamilyName, TestType.UWP)
        {
        }

        public string TestName { get; set; }
        public TimeSpan TestRunTime {get; set; }
    }

    public class TAEFTestCase
    {
        public TAEFTestCase()
        {
        }

        public String Name;
        public TestStatus TestStatus { get; set; }
        public bool? TestPassed
        {
            get
            {
                if (this.TestStatus == TestStatus.TAEFTestCasePassed)
                {
                    return true;
                }
                else if (this.TestStatus == TestStatus.TAEFTestCaseFailed)
                {
                    return false;
                }
                else
                {
                    return null;
                }
            }
        }
    }

    public class TestList
    {
        public Dictionary<Guid, Tuple<ExecutableTest, bool>> Tests;
        public Guid Guid { get; }
        private bool? _result;

        public TestList()
        {
            Tests = new Dictionary<Guid, Tuple<ExecutableTest, bool>>();
            Guid = Guid.NewGuid();
        }
    }
}
