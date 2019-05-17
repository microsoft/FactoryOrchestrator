using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.FactoryTestFramework.Core.JSONConverters;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;

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
    [XmlInclude(typeof(ExecutableTest))]
    [XmlInclude(typeof(UWPTest))]
    [XmlInclude(typeof(ExternalTest))]
    [XmlInclude(typeof(TAEFTest))]
    public abstract class TestBase
    {
        // TODO: Quality: Use Semaphore internally to guarantee accurate state if many things are setting test state
        // lock on modification & lock on query so that internal state is guaranteed to be consistent at all times
        protected TestBase(TestType type)
        {
            TestType = type;
            IsEnabled = true;
            TestLock = new object();
            LatestTestRunStatus = TestStatus.TestNotRun;
            LatestTestRunExitCode = null;
            LatestTestRunTimeFinished = null;
            LatestTestRunTimeStarted = null;
            TestRunGuids = new List<Guid>();
        }

        public TestBase(string testPath, TestType type) : this(type)
        {
            Guid = Guid.NewGuid();
            TestPath = testPath;
        }

        // TODO: Make only getters and add internal apis to set
        public TestType TestType { get; set; }

        [XmlAttribute("TestPath")]
        public string TestPath { get; set; }
        public string LogFolder { get; set; }
        public string Arguments { get; set; }

        [XmlAttribute("Guid")]
        public Guid Guid { get; set; }
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

        [XmlAttribute("Name")]
        public virtual string TestName
        {
            get
            {
                return TestPath;
            }
            set {}
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

        // TODO: Quality: Consider using IXmlSerializable so we can make some properties "read only"
        //public XmlSchema GetSchema()
        //{
        //    return null;
        //}

        //public void ReadXml(XmlReader reader)
        //{
        //    reader.MoveToContent();
        //    Guid = reader.GetAttribute("Guid")
        //}

        //public void WriteXml(XmlWriter writer)
        //{

        //}

        [JsonIgnore]
        [XmlIgnore]
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
        private ExecutableTest() : base(TestType.ConsoleExe)
        {

        }

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

        [XmlAttribute("Name")]
        public override string TestName
        {
            get
            {
                if (_testFriendlyName == null)
                {
                    return Path.GetFileName(TestPath);
                }
                else
                {
                    return _testFriendlyName;
                }
            }
            set
            {
                _testFriendlyName = value;
            }
        }

        private string _testFriendlyName;
    }

    /// <summary>
    /// A TAEFTest is a type of ExecutableTest, which is always run by TE.exe. TAEF tests are comprised of one or more sub-tests (TAEFTestCase).
    /// Pass/Fail is determined by TE.exe.
    /// </summary>
    [JsonConverter(typeof(NoConverter))]
    public class TAEFTest : ExecutableTest
    {
        private TAEFTest() : base(null, TestType.TAEFDll)
        {

        }

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

        public List<TAEFTestCase> TestCases { get; set; }
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
        private ExternalTest() : base(TestType.External)
        {
        }

        public ExternalTest(String testName) : base(null, TestType.External)
        {
            _testFriendlyName = testName;
        }

        protected ExternalTest(String testPath, String testName, TestType type) : base(testPath, type)
        {
            _testFriendlyName = testName;
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

        [XmlAttribute("Name")]
        public override string TestName
        {
            get
            {
                if (_testFriendlyName == null)
                {
                    return TestPath;
                }
                else
                {
                    return _testFriendlyName;
                }
            }
            set
            {
                _testFriendlyName = value;
            }
        }

        private string _testFriendlyName;
    }

    /// <summary>
    /// A UWPTest is a UWP test run by the FTFUWP client. These are used for UI.
    /// Test results must be returned to the server via SetTestRunStatus().
    /// </summary>
    [JsonConverter(typeof(NoConverter))]
    public class UWPTest : ExternalTest
    {
        private UWPTest() : base(null, null, TestType.UWP)
        {
        }

        public UWPTest(string packageFamilyName, string testFriendlyName) : base(packageFamilyName, testFriendlyName, TestType.UWP)
        {
            _testFriendlyName = testFriendlyName;
        }

        public UWPTest(string packageFamilyName) : base(packageFamilyName, null, TestType.UWP)
        {
            _testFriendlyName = packageFamilyName;
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

        [XmlAttribute("Name")]
        public override string TestName
        {
            get
            {
                if (_testFriendlyName == null)
                {
                    return TestPath;
                }
                else
                {
                    return _testFriendlyName;
                }
            }
            set
            {
                _testFriendlyName = value;
            }
        }

        private string _testFriendlyName;
    }

    /// <summary>
    /// A TestList is a grouping of FTF tests. TestLists are the only object FTF can "Run".
    /// </summary>
    public class TestList
    {
        [JsonConstructor]
        internal TestList()
        {
            Tests = new Dictionary<Guid, TestBase>();
            TestsForXml = new List<TestBase>();
        }

        public TestList(Guid guid) : this()
        {
            if (guid != null)
            {
                Guid = guid;
            }
            else
            {
                Guid = Guid.NewGuid();
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

        [XmlArrayItem("Test")]
        [XmlArray("Tests")]
        [JsonIgnore]
        public List<TestBase> TestsForXml { get; set; }

        [XmlIgnore]
        public Dictionary<Guid, TestBase> Tests { get; set; }

        public Guid Guid { get; set; }
    }

    /// <summary>
    /// Shared client and server TestRun class. A TestRun represents one instance of executing any single FTF Test.
    /// TestRuns should only be created by the server, hence no public CTOR.
    /// </summary>
    public class TestRun
    {
        // TODO: Quality: Use Semaphore internally to guarantee accurate state if many things are setting test state
        // lock on modification & lock on query so that internal state is guaranteed to be consistent at all times
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
            Guid = Guid.NewGuid();
            ConsoleLogFilePath = null;
            TestStatus = TestStatus.TestNotRun;
            TimeFinished = null;
            TimeStarted = null;
            ExitCode = null;
            TestOutput = new List<string>();

            if (owningTest != null)
            {
                OwningTestGuid = owningTest.Guid;
                TestPath = owningTest.TestPath;
                Arguments = owningTest.Arguments;
                TestName = owningTest.TestName;
                TestType = owningTest.TestType;
            }
        }

        public List<string> TestOutput { get; set; }

        public Guid OwningTestGuid { get; set; }
        public string TestName { get; set; }
        public string TestPath { get; set; }
        public string Arguments { get; set; }
        public TestType TestType { get; set; }
        public Guid Guid { get; set; }
        public DateTime? TimeStarted { get; set; }
        public DateTime? TimeFinished { get; set; }
        public TestStatus TestStatus { get; set; }
        public string ConsoleLogFilePath { get; set; }
        public int? ExitCode { get; set; }

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
    }

    /// <summary>
    /// This class is only used to save & load TestLists from an XML file.
    /// </summary>
    [XmlRootAttribute(ElementName = "TestLists", IsNullable = false)]
    public partial class TestListXml
    {
        public TestListXml()
        {
            TestLists = new List<TestList>();
        }

        public List<TestList> TestLists { get; set; }

        /// <summary>
        /// Create Guids for any imported test or testlist that is missing one.
        /// Create Tests dictionary.
        /// </summary>
        public void PostDeserialize()
        {
            foreach (var list in TestLists)
            {
                if (list.Guid == Guid.Empty)
                {
                    list.Guid = Guid.NewGuid();
                }

                foreach (var test in list.TestsForXml)
                {
                    if (test.Guid == Guid.Empty)
                    {
                        test.Guid = Guid.NewGuid();
                    }

                    list.Tests.Add(test.Guid, test);
                }

                // clear old xml list
                list.TestsForXml = new List<TestBase>();
            }
        }


        /// <summary>
        /// Create TestsForXml List.
        /// </summary>
        public void PreDeserialize()
        {
            foreach (var list in TestLists)
            {
                list.TestsForXml = new List<TestBase>();
                list.TestsForXml.AddRange(list.Tests.Values);
            }
        }
    }
}