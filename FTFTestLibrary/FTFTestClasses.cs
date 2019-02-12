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

    public class FactoryTest
    {
        public FactoryTest(String testPath)
        {
            IsTAEF = false;
            TestPath = testPath;
            Guid = Guid.NewGuid();
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

        public void Reset()
        {
            LastTimeRun = null;
            TestStatus = TestStatus.TestNotRun;
            TestRunner = null;
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

        // protected instance variables
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

        public int? ExitCode { get; set; }
        public String LogFilePath { get; set; }
        public String TestPath;
        public String TestName
        {
            get
            {
                return Path.GetFileName(TestPath);
            }
        }
        public bool IsTAEF { get; set; }
        public List<String> Arguments;
        public Guid Guid { get; }

        [JsonIgnore]
        public TestRunner TestRunner;
    }

    public class TAEFTest : FactoryTest
    {
        public TAEFTest(string testPath) : base(testPath)
        {
            IsTAEF = true;
        }

        private List<TAEFTestCase> _testCases;
        private String _wtlFilePath;
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
        public Dictionary<Guid, Tuple<FactoryTest, bool>> Tests;
        public Guid Guid { get; }
        private bool? _result;

        public TestList()
        {
            Tests = new Dictionary<Guid, Tuple<FactoryTest, bool>>();
            Guid = Guid.NewGuid();
        }
    }
}
