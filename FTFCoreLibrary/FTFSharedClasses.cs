using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.FactoryTestFramework.Core.JSONConverters;
using System.Threading.Tasks;


namespace Microsoft.FactoryTestFramework.Core
{
    public enum TestStatus
    {
        TestPassed,
        TestFailed,
        TestAborted,
        TestRunning,
        TestNotRun,
        TestWaitingForExternalResult
    }

    public enum TestType
    {
        ConsoleExe = 0,
        TAEFDll = 1,
        External = 2,
        UWP = 3
    }

    public class TestBaseEqualityComparer : EqualityComparer<TestBase>
    {
        public override bool Equals(TestBase x, TestBase y)
        {
            return x.Equals(y);
        }

        public override int GetHashCode(TestBase obj)
        {
            return obj.GetHashCode();
        }
    }

    /// <summary>
    /// TestBase is an abstract class representing a generic FTF test. It contains all the details needed to run the test.
    /// It also surfaces information about the last TestRun for this test, for easy consumption.
    /// </summary>
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
            LatestTestRunStatus = TestStatus.TestNotRun;
            LatestTestRunExitCode = null;
            LatestTestRunTimeFinished = null;
            LatestTestRunTimeStarted = null;
            TestRunGuids = new List<Guid>();
        }

        // TODO: Make only getters and add internal apis to set
        public TestType TestType { get; }
        public string TestPath { get; }
        public string LogFolder { get; set; }
        public string Arguments { get; set; }
        public Guid Guid { get => _guid; }
        public DateTime? LatestTestRunTimeStarted { get; set; }
        public DateTime? LatestTestRunTimeFinished { get; set; }
        public TestStatus LatestTestRunStatus { get; set; }
        public bool? LatestTestRunPassed
        {
            get
            {
                if (LatestTestRunStatus == TestStatus.TestPassed)
                {
                    return true;
                }
                else if (LatestTestRunStatus == TestStatus.TestFailed)
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

        public int? LatestTestRunExitCode { get; set; }

        public virtual TimeSpan? LatestTestRunRunTime
        {
            get
            {
                if (LatestTestRunTimeStarted != null)
                {
                    if (LatestTestRunTimeFinished != null)
                    {
                        return LatestTestRunTimeFinished - LatestTestRunTimeStarted;
                    }
                    else
                    {
                        return DateTime.Now - LatestTestRunTimeStarted;
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
                return ((TestType == TestType.TAEFDll) || (TestType == TestType.ConsoleExe));
            }
        }

        public bool RunByClient
        {
            get
            {
                return !RunByServer;
            }
        }

        public virtual string TestName
        {
            get
            {
                return TestPath;
            }
        }

        public Guid? LastTestRunGuid
        {
            get
            {
                if ((TestRunGuids != null) && (TestRunGuids.Count >= 1))
                {
                    return TestRunGuids.Last();
                }
                else
                {
                    return null;
                }
            }
        }

        public override bool Equals(object obj)
        {
            var rhs = obj as TestBase;

            if (rhs == null)
            {
                return false;
            }

            if (this.Guid != rhs.Guid)
            {
                return false;
            }

            if (this.Arguments != rhs.Arguments)
            {
                return false;
            }

            if (this.IsEnabled != rhs.IsEnabled)
            {
                return false;
            }

            if (this.LatestTestRunExitCode != rhs.LatestTestRunExitCode)
            {
                return false;
            }

            if (this.LatestTestRunStatus != rhs.LatestTestRunStatus)
            {
                return false;
            }

            if (this.LastTestRunGuid != rhs.LastTestRunGuid)
            {
                return false;
            }

            if (this.LatestTestRunTimeFinished != rhs.LatestTestRunTimeFinished)
            {
                return false;
            }

            if (this.LatestTestRunTimeStarted != rhs.LatestTestRunTimeStarted)
            {
                return false;
            }

            if (this.LogFolder != rhs.LogFolder)
            {
                return false;
            }

            if (this.TestType != rhs.TestType)
            {
                return false;
            }

            if (!this.TestRunGuids.SequenceEqual(rhs.TestRunGuids))
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return -737073652 + EqualityComparer<Guid>.Default.GetHashCode(Guid);
        }

        [JsonRequired]
        private Guid _guid;

        [JsonIgnore]
        public object TestLock;

        // TestRuns are queried by GUID
        public List<Guid> TestRunGuids { get; set; }
    }

    /// <summary>
    /// An ExecutableTest is an .exe binary that is run by the FTFServer. The exit code of the process determines if the test passed or failed.
    /// 0 == PASS, all others == FAIL.
    /// </summary>
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

        public override bool Equals(object obj)
        {
            var rhs = obj as ExecutableTest;

            if (rhs == null)
            {
                return false;
            }

            return base.Equals(obj as TestBase);
        }

        public override String TestName
        {
            get
            {
                return Path.GetFileName(TestPath);
            }
        }
    }

    /// <summary>
    /// A TAEFTest is a type of ExecutableTest, which is always run by TE.exe. TAEF tests are comprised of one or more sub-tests (TAEFTestCase).
    /// Pass/Fail is determined by TE.exe.
    /// </summary>
    [JsonConverter(typeof(NoConverter))]
    public class TAEFTest : ExecutableTest
    {
        public TAEFTest(string testPath) : base(testPath, TestType.TAEFDll)
        {
        }

        public override bool Equals(object obj)
        {
            var rhs = obj as TestBase;

            if (rhs == null)
            {
                return false;
            }

            return base.Equals(obj as ExecutableTest);
        }

        private List<TAEFTestCase> _testCases;
        private String _wtlFilePath;
    }

    /// <summary>
    /// A test case in a TAEF Test. Currently, not executable alone.
    /// </summary>
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

    /// <summary>
    /// An ExternalTest is a test run outside of the FTFServer.
    /// Test results must be returned to the server via SetTestRunStatus().
    /// </summary>
    [JsonConverter(typeof(NoConverter))]
    public class ExternalTest : TestBase
    {
        public ExternalTest(String testName) : base(null, TestType.External)
        {
            TestName = testName;
        }

        protected ExternalTest(String testPath, String testName, TestType type) : base(testPath, type)
        {
            TestName = testName;
        }

        public override string ToString()
        {
            return TestName;
        }

        public override bool Equals(object obj)
        {
            var rhs = obj as ExternalTest;

            if (rhs == null)
            {
                return false;
            }

            if (rhs.TestName != TestName)
            {
                return false;
            }

            return base.Equals(obj as TestBase);
        }

        public override string TestName { get; }
    }

    /// <summary>
    /// A UWPTest is a UWP test run by the FTFUWP client. These are used for UI.
    /// Test results must be returned to the server via SetTestRunStatus().
    /// </summary>
    [JsonConverter(typeof(NoConverter))]
    public class UWPTest : ExternalTest
    {
        public UWPTest(string packageFamilyName, string testFriendlyName) : base(packageFamilyName, testFriendlyName, TestType.UWP)
        {
            TestName = testFriendlyName;
        }

        public UWPTest(string packageFamilyName) : base(packageFamilyName, null, TestType.UWP)
        {
            TestName = packageFamilyName;
        }

        public override bool Equals(object obj)
        {
            var rhs = obj as UWPTest;

            if (rhs == null)
            {
                return false;
            }

            if (this.TestName != this.TestName)
            {
                return false;
            }

            return base.Equals(obj as ExternalTest);
        }

        public override string TestName { get; }
    }

    /// <summary>
    /// A TestList is a grouping of FTF tests. TestLists are the only object FTF can "Run".
    /// </summary>
    public class TestList
    {
        public Dictionary<Guid, TestBase> Tests;

        public Guid Guid { get => _guid; }

        [JsonRequired]
        private Guid _guid;

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
                if (Tests.Values.All(x => x.LatestTestRunPassed == true))
                {
                    return TestStatus.TestPassed;
                }
                else if (Tests.Values.Any(x => x.LatestTestRunStatus == TestStatus.TestRunning))
                {
                    return TestStatus.TestRunning;
                }
                else if (Tests.Values.Any(x => x.LatestTestRunStatus == TestStatus.TestFailed))
                {
                    return TestStatus.TestFailed;
                }
                else
                {
                    return TestStatus.TestNotRun;
                }
            }
        }

        public override bool Equals(object obj)
        {
            var rhs = obj as TestList;

            if (rhs == null)
            {
                return false;
            }

            if (this.Guid != rhs.Guid)
            {
                return false;
            }

            //if (!this.Tests.Keys.Equals(rhs.Tests.Keys))
            //{
            //    return false;
            //}

            //if (this.Tests.Values != rhs.Tests.Values)
            //{
            //    return false;
            //}

            if (!this.Tests.SequenceEqual(rhs.Tests))
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return -2045414129 + EqualityComparer<Guid>.Default.GetHashCode(Guid);
        }
    }

    /// <summary>
    /// Shared client and server TestRun class. A TestRun represents one instance of executing any single FTF Test.
    /// TestRuns should only be created by the server, hence no public CTOR.
    /// </summary>
    public class TestRun
    {
        [JsonConstructor]
        protected TestRun()
        {

        }

        /// <summary>
        /// Test Run shared constructor. TestRuns 
        /// </summary>
        /// <param name="owningTest"></param>
        protected TestRun(TestBase owningTest)
        {
            _guid = Guid.NewGuid();
            ConsoleLogFilePath = null;
            TestStatus = TestStatus.TestNotRun;
            TimeFinished = null;
            TimeStarted = null;
            ExitCode = null;
            TestOutput = new List<string>();

            if (owningTest != null)
            {
                _owningGuid = owningTest.Guid;
                _testPath = owningTest.TestPath;
                _arguments = owningTest.Arguments;
                _testName = owningTest.TestName;
                _testType = owningTest.TestType;
            }
        }

        public List<string> TestOutput { get; set; }

        public Guid OwningTestGuid { get => _owningGuid; }
        public string TestName { get => _testName; }
        public string TestPath { get => _testPath; }
        public string Arguments { get => _arguments; }
        public TestType TestType { get => _testType; }
        public Guid Guid { get => _guid; }
        public DateTime? TimeStarted { get; set; }
        public DateTime? TimeFinished { get; set; }
        public TestStatus TestStatus { get; set; }
        public string ConsoleLogFilePath { get; set; }

        public bool RunByServer
        {
            get
            {
                return ((TestType == TestType.TAEFDll) || (TestType == TestType.ConsoleExe));
            }
        }

        public bool RunByClient
        {
            get
            {
                return !RunByServer;
            }
        }

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

        public override bool Equals(object obj)
        {
            var rhs = obj as TestRun;

            if (rhs == null)
            {
                return false;
            }

            if (this.Guid != rhs.Guid)
            {
                return false;
            }

            if (this.OwningTestGuid != rhs.OwningTestGuid)
            {
                return false;
            }

            if (this.TestType != rhs.TestType)
            {
                return false;
            }

            if (this.TestName != rhs.TestName)
            {
                return false;
            }

            if (this.Arguments != rhs.Arguments)
            {
                return false;
            }

            if (this.ExitCode != rhs.ExitCode)
            {
                return false;
            }

            if (this.TestStatus != rhs.TestStatus)
            {
                return false;
            }

            if (this.TimeFinished != rhs.TimeFinished)
            {
                return false;
            }

            if (this.TimeStarted != rhs.TimeStarted)
            {
                return false;
            }

            if (this.ConsoleLogFilePath != rhs.ConsoleLogFilePath)
            {
                return false;
            }

            if (!this.TestOutput.SequenceEqual(rhs.TestOutput))
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return -737073652 + EqualityComparer<Guid>.Default.GetHashCode(Guid);
        }

        [JsonRequired]
        private Guid _guid;

        private Guid _owningGuid;
        [JsonRequired]
        protected string _testPath;
        [JsonRequired]
        protected string _testName;
        protected string _arguments;
        [JsonRequired]
        protected TestType _testType;

        public int? ExitCode { get; set; }
    }
}