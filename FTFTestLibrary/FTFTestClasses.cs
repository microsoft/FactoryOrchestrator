﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
}