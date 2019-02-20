﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Linq;
using Newtonsoft.Json.Linq;
using FTFJsonConverters;

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
            TestOutput = new List<string>();
            TestRunGuids = new List<Guid>();
        }
        
        public TestType TestType { get; }

        public string Arguments { get; set; }
        public Guid Guid { get => _guid; }
        public DateTime? LastTimeStarted { get; set; }
        public DateTime? LastTimeFinished { get; set; }
        public TestStatus TestStatus { get; set; }

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

        // public bool? TestPassed
        // {
        //     get
        //     {
        //         if (TestRunGuids.Count > 0)
        //         {
        //             return TestRun.GetTestRunByGuid(TestRunGuids[TestRunGuids.Count - 1]).TimeRun;
        //         }
        //         else
        //         {
        //             return null;
        //         }
        //     }
        // }

        public TestStatus LatestTestStatus
        {
            get
            {
                if(TestRunGuids.Count > 0)
                {
                    return TestRun.GetTestRunByGuid(TestRunGuids[TestRunGuids.Count - 1]).TestRunStatus;
                }
                else
                {
                    return TestStatus.TestNotRun;
                }
            }
        }

        // public bool? LatestTestRunPassed
        // {
        //     get
        //     {
        //         if (TestRunGuids.Count > 0)
        //         {
        //             var status = TestRun.GetTestRunByGuid(TestRunGuids[TestRunGuids.Count - 1]).TestRunStatus;
        public bool IsEnabled { get; set; }

        public int? ExitCode { get; set; }
        public string LogFilePath { get; set; }

        public string TestPath { get; }

        // TODO: This will be replaced by the testrun class
        public List<string> TestOutput { get; set; }

        public bool UsesTestRunner
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
    
        public virtual void Reset()
        {
            lock (TestLock)
            {
                TestStatus = TestStatus.TestNotRun;
                ExitCode = null;
                TestOutput = new List<string>();
                TestRunGuids = new List<Guid>();

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

        [JsonRequired]
        private Guid _guid;

        [JsonIgnore]
        internal object TestLock;

        // TestRuns are queried by GUID
        public List<Guid> TestRunGuids;
    }


    [JsonConverter(typeof(NoConverter))]
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

        // TODO: kill all running testrun instances, delete all old test runs?
        public override void Reset()
        {
            lock (TestLock)
            {
                if ((TestRunner != null) && (TestRunner.IsRunning))
                {
                    TestRunner.StopTest();
                }
                TestRunner = null;
            }
            base.Reset();
        }

        public override String TestName
        {
            get
            {
                return Path.GetFileName(TestPath);
            }
        }

        [JsonIgnore]
        public TestRunner TestRunner;
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
                if (Tests.Values.All(x => x.TestPassed == true))
                {
                    return TestStatus.TestPassed;
                }
                else if (Tests.Values.Any(x => x.TestStatus == TestStatus.TestRunning))
                {
                    return TestStatus.TestRunning;
                }
                else if (Tests.Values.Any(x => x.TestStatus == TestStatus.TestFailed))
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
    // TODO: Move testrunner into testrun
    // TODO: Move to static or child class for server use?
    public class TestRun
    {
        static TestRun()
        {
            _testRunMap = new Dictionary<Guid, TestRun>();
        }

        internal static List<Guid> GetTestRunGuidsByTestGuid(Guid testGuid)
        {
            return _testRunMap.Values.Where(x => x.OwningTestGuid == testGuid).Select(x => x.Guid).ToList();
        }

        internal static TestRun GetTestRunByGuid(Guid testRunGuid)
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

        /// <summary>
        /// Tracks all the test runs that have ever occured, mapped by the test run GUID
        /// </summary>
        private static Dictionary<Guid, TestRun> _testRunMap;

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

        public TestRun(Guid owningTestGuid)
        {
            OwningTestGuid = owningTestGuid;
            Guid = Guid.NewGuid();
            _testRunMap.Add(Guid, this);
        }

        public List<string> TestOutput
        {
            get
            {
                return _testOutput;
            }
        }

        protected internal List<string> _testOutput;

        public Guid OwningTestGuid { get; }
        public Guid Guid { get; }

        public TimeSpan TestRunTime
        {
            get
            {
                // todo: implement
                return new TimeSpan();
            }
        }

        public DateTime TimeRun { get; set; }
        public int? ExitCode { get; set; }

        public TestStatus TestRunStatus { get; set; }
    }
}
