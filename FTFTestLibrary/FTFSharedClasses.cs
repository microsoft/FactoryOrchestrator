using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using FTFJsonConverters;

namespace FTFSharedLibrary
{
    public enum TestStatus
    {
        TestPassed,
        TestFailed,
        TestAborted,
        TestRunning,
        TestNotRun
    }

    public enum TestType
    {
        ConsoleExe = 0,
        TAEFDll = 1,
        UWP = 2
    }

    [JsonConverter(typeof(TestBaseConverter))]
    public abstract class TestBase
    {
        // TODO: how do we guarantee accurate state when many things are querying test state?????
        public TestBase(string testPath, TestType type)
        {
            _guid = Guid.NewGuid();
            TestType = type;
            TestPath = testPath;
            IsEnabled = true;
            TestLock = new object();
            LastRunStatus = TestStatus.TestNotRun;
            LastExitCode = null;
            LastTimeFinished = null;
            LastTimeStarted = null;
            TestRunGuids = new List<Guid>();
        }
        
        // TODO: Make only getters and add internal apis to set
        public TestType TestType { get; }
        public string TestPath { get; }
        public string LogFolder { get; set; }
        public string Arguments { get; set; }
        public Guid Guid { get => _guid; }
        public DateTime? LastTimeStarted { get; set; }
        public DateTime? LastTimeFinished { get; set; }
        public TestStatus LastRunStatus { get; set; }
        public bool? LastRunPassed
        {
            get
            {
                if (LastRunStatus == TestStatus.TestPassed)
                {
                    return true;
                }
                else if (LastRunStatus == TestStatus.TestFailed)
                {
                    return false;
                }
                else
                {
                    return null;
                }
            }
        }

        public bool IsEnabled { get; set; }

        public int? LastExitCode { get; set; }

        public virtual TimeSpan? TestRunTime
        {
            get
            {
                if (LastTimeStarted != null)
                {
                    if (LastTimeFinished != null)
                    {
                        return LastTimeFinished - LastTimeStarted;
                    }
                    else
                    {
                        return DateTime.Now - LastTimeStarted;
                    }
                }
                else
                {
                    return null;
                }
            }
        }



        public bool RunByServer
        {
            get
            {
                return !(TestType == TestType.UWP);
            }
        }

        public virtual string TestName
        {
            get
            {
                return TestPath;
            }
        }

        [JsonRequired]
        private Guid _guid;

        [JsonIgnore]
        public object TestLock;

        // TestRuns are queried by GUID
        public List<Guid> TestRunGuids { get; set; }
    }


    [JsonConverter(typeof(NoConverter))]
    public class ExecutableTest : TestBase
    {
        public ExecutableTest(String testPath) : base(testPath, TestType.ConsoleExe)
        {
        }

        protected ExecutableTest(String testPath, TestType type) : base(testPath, type)
        {
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

        // TODO: kill all running testrun instances, delete all old test runs?
        //public override void Reset()
        //{
        //    if (LatestTestStatus == TestStatus.TestRunning)
        //    {
        //        TestRunGuids.Last()
        //    }
        //    base.Reset();
        //}

        public override String TestName
        {
            get
            {
                return Path.GetFileName(TestPath);
            }
        }
    }

    [JsonConverter(typeof(NoConverter))]
    public class TAEFTest : ExecutableTest
    {
        public TAEFTest(string testPath) : base(testPath, TestType.TAEFDll)
        {
        }

        private List<TAEFTestCase> _testCases;
        private String _wtlFilePath;
    }


    [JsonConverter(typeof(NoConverter))]
    public class UWPTest : TestBase
    {
        public UWPTest(string packageFamilyName, string testFriendlyName = null) : base(packageFamilyName, TestType.UWP)
        {
            if (!String.IsNullOrWhiteSpace(testFriendlyName))
            {
                TestName = testFriendlyName;
            }
            else
            {
                TestName = packageFamilyName;
            }
        }

        public override string TestName { get; }
        public override TimeSpan? TestRunTime {get; }
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
                // TODO: Fix me up
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
    }

    public class TestList
    {
        
        public Dictionary<Guid, TestBase> Tests;

        public Guid Guid { get => _guid; }

        [JsonRequired]
        private Guid _guid;

        private bool? _result;

        [JsonConstructor]
        internal TestList()
        {
            Tests = new Dictionary<Guid, TestBase>();
        }

        public TestList(Guid guid) : this()
        {
            if (guid != null)
            {
                _guid = guid;
            }
            else
            {
                _guid = Guid.NewGuid();
            }
        }

        public TestStatus TestListStatus
        {
            get
            {
                if (Tests.Values.All(x => x.LastRunPassed == true))
                {
                    return TestStatus.TestPassed;
                }
                else if (Tests.Values.Any(x => x.LastRunStatus == TestStatus.TestRunning))
                {
                    return TestStatus.TestRunning;
                }
                else if (Tests.Values.Any(x => x.LastRunStatus == TestStatus.TestFailed))
                {
                    return TestStatus.TestFailed;
                }
                else
                {
                    return TestStatus.TestNotRun;
                }
            }
        }
    }


    // TODO: Use this to track test status instead of inside TestBase/ExecutableTest
    /// <summary>
    /// Share client & server TestRun class
    /// </summary>
    public class TestRun
    {
        // class to track an individual run of a testbase object
        // output
        // errors
        // log file
        // runtime
        // guid ptr to test
        // status
        [JsonConstructor]
        internal TestRun()
        {

        }

        public TestRun(TestBase owningTest)
        {
            OwningTestGuid = owningTest.Guid;
            Guid = Guid.NewGuid();
            LogFilePath = null;
            TestStatus = TestStatus.TestNotRun;
            TimeFinished = null;
            TimeStarted = null;
            ExitCode = null;
            TestOutput = new List<string>();
        }

        public List<string> TestOutput { get; set; }

        public Guid OwningTestGuid { get; }
        public Guid Guid { get; }

        public DateTime? TimeStarted { get; set; }
        public DateTime? TimeFinished { get; set; }
        public TestStatus TestStatus { get; set; }
        public string LogFilePath { get; set; }

        public bool TestRunComplete
        {
            get
            {
                switch (TestStatus)
                {
                    case TestStatus.TestAborted:
                    case TestStatus.TestFailed:
                    case TestStatus.TestPassed:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public virtual TimeSpan? RunTime
        {
            get
            {
                if (TimeStarted != null)
                {
                    if (TimeFinished != null)
                    {
                        return TimeFinished - TimeStarted;
                    }
                    else
                    {
                        return DateTime.Now - TimeStarted;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        public int? ExitCode { get; set; }
    }
}
