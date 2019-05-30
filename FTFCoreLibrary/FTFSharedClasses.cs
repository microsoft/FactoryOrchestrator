using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.FactoryTestFramework.Core.JSONConverters;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Reflection;

namespace Microsoft.FactoryTestFramework.Core
{
    public enum TestStatus
    {
        TestPassed,
        Failed,
        Aborted,
        Timeout,
        Running,
        NotRun,
        WaitingForExternalResult,
        Unknown
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
            TestLock = new object();
            LatestTestRunStatus = TestStatus.NotRun;
            LatestTestRunExitCode = null;
            LatestTestRunTimeFinished = null;
            LatestTestRunTimeStarted = null;
            TestRunGuids = new List<Guid>();
            TimeoutSeconds = -1;
        }

        public TestBase(string testPath, TestType type) : this(type)
        {
            Guid = Guid.NewGuid();
            TestPath = testPath;
        }

        // TODO: Make only getters and add internal apis to set
        [XmlAttribute("Name")]
        public virtual string TestName
        {
            get
            {
                return TestPath;
            }
            set { }
        }

        [XmlIgnore]
        public TestType TestType { get; set; }
        [XmlAttribute("Path")]
        public string TestPath { get; set; }
        public string LogFolder { get; set; }
        [XmlAttribute]
        public string Arguments { get; set; }
        [XmlAttribute]
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
                else if (LatestTestRunStatus == TestStatus.Failed)
                {
                    return false;
                }
                else
                {
                    return null;
                }
            }
        }

        [XmlAttribute("Timeout")]
        public int TimeoutSeconds { get; set; }

        public int? LatestTestRunExitCode { get; set; }

        // TestRuns are queried by GUID
        [XmlArrayItem("Guid")]
        public List<Guid> TestRunGuids { get; set; }

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

        // XmlSerializer calls these to check if these values are set.
        // If not set, don't serialize.
        // https://docs.microsoft.com/en-us/dotnet/framework/winforms/controls/defining-default-values-with-the-shouldserialize-and-reset-methods
        public bool ShouldSerializeLatestTestRunTimeStarted()
        {
            return LatestTestRunTimeStarted.HasValue;
        }

        public bool ShouldSerializeLatestTestRunTimeFinished()
        {
            return LatestTestRunTimeFinished.HasValue;
        }

        public bool ShouldSerializeLatestTestRunExitCode()
        {
            return LatestTestRunExitCode.HasValue;
        }
        public bool ShouldSerializeTestRunGuids()
        {
            return TestRunGuids.Count > 0;
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
    }

    /// <summary>
    /// An ExecutableTest is an .exe binary that is run by the FTFServer. The exit code of the process determines if the test passed or failed.
    /// 0 == PASS, all others == FAIL.
    /// </summary>
    [JsonConverter(typeof(NoConverter))]
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public class ExecutableTest : TestBase
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    {
        private ExecutableTest() : base(TestType.ConsoleExe)
        {
            BackgroundTask = false;
        }

        public ExecutableTest(String testPath) : base(testPath, TestType.ConsoleExe)
        {
            BackgroundTask = false;
        }

        protected ExecutableTest(String testPath, TestType type) : base(testPath, type)
        {
            BackgroundTask = false;
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

        [XmlAttribute]
        public bool BackgroundTask { get; set; }

        private string _testFriendlyName;
    }

    /// <summary>
    /// A TAEFTest is a type of ExecutableTest, which is always run by TE.exe. TAEF tests are comprised of one or more sub-tests (TAEFTestCase).
    /// Pass/Fail is determined by TE.exe.
    /// </summary>
    [JsonConverter(typeof(NoConverter))]
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public class TAEFTest : ExecutableTest
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
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
        //private String _wtlFilePath;
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
                else if (this.TestStatus == TestStatus.Failed)
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
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public class ExternalTest : TestBase
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
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
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public class UWPTest : ExternalTest
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
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
            RunInParallel = false;
            AllowOtherTestListsToRun = false;
            TerminateBackgroundTasksOnCompletion = true;
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
                else if (Tests.Values.Any(x => (x.LatestTestRunStatus == TestStatus.Running) || (x.LatestTestRunStatus == TestStatus.WaitingForExternalResult)))
                {
                    return TestStatus.Running;
                }
                else if (Tests.Values.Any(x => (x.LatestTestRunStatus == TestStatus.Failed) || (x.LatestTestRunStatus == TestStatus.Timeout)))
                {
                    return TestStatus.Failed;
                }
                else if (Tests.Values.Any(x => x.LatestTestRunStatus == TestStatus.Unknown))
                {
                    return TestStatus.Unknown;
                }
                else
                {
                    return TestStatus.NotRun;
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

        /// <summary>
        /// XML serializer can't serialize Dictionaries. Use a list instead for XML.
        /// </summary>
        [XmlArrayItem("Test")]
        [XmlArray("Tests")]
        [JsonIgnore]
        public List<TestBase> TestsForXml { get; set; }

        /// <summary>
        /// Tests in the TestList, tracked by test GUID
        /// </summary>
        [XmlIgnore]
        public Dictionary<Guid, TestBase> Tests { get; set; }

        [XmlAttribute]
        public Guid Guid { get; set; }


        [XmlAttribute]
        public bool RunInParallel { get; set; }

        [XmlAttribute]
        public bool AllowOtherTestListsToRun { get; set; }

        [XmlAttribute]
        public bool TerminateBackgroundTasksOnCompletion { get; set; }
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
        /// Test Run shared constructor. 
        /// </summary>
        /// <param name="owningTest"></param>
        protected TestRun(TestBase owningTest)
        {
            Guid = Guid.NewGuid();
            OwningTestGuid = null;
            ConsoleLogFilePath = null;
            TestStatus = TestStatus.NotRun;
            TimeFinished = null;
            TimeStarted = null;
            ExitCode = null;
            TestOutput = new List<string>();
            TimeoutSeconds = -1;
            BackgroundTask = false;

            if (owningTest != null)
            {
                OwningTestGuid = owningTest.Guid;
                TestPath = owningTest.TestPath;
                Arguments = owningTest.Arguments;
                TestName = owningTest.TestName;
                TestType = owningTest.TestType;
                TimeoutSeconds = owningTest.TimeoutSeconds;
                if (TestType == TestType.ConsoleExe)
                {
                    BackgroundTask = ((ExecutableTest)owningTest).BackgroundTask;
                }
            }
        }

        public TestRun DeepCopy()
        {
            TestRun copy = (TestRun)this.MemberwiseClone();
            copy.TestOutput = new List<string>(this.TestOutput.Count);
            copy.TestOutput.AddRange(this.TestOutput.GetRange(0, copy.TestOutput.Capacity));

            var stringProps = typeof(TestRun).GetProperties().Where(x => x.PropertyType == typeof(string));
            foreach (var prop in stringProps)
            {
                var value = prop.GetValue(this);
                if (value != null)
                {
                    var copyStr = String.Copy(value as string);
                    prop.SetValue(copy, copyStr);
                }
                else
                {
                    prop.SetValue(copy, null);
                }
            }

            return copy;
        }

        public List<string> TestOutput { get; set; }

        public Guid? OwningTestGuid { get; set; }
        public string TestName { get; set; }
        public string TestPath { get; set; }
        public string Arguments { get; set; }
        public bool BackgroundTask { get; set; }
        public TestType TestType { get; set; }
        public Guid Guid { get; set; }
        public DateTime? TimeStarted { get; set; }
        public DateTime? TimeFinished { get; set; }
        public TestStatus TestStatus { get; set; }
        public string ConsoleLogFilePath { get; set; }
        public int? ExitCode { get; set; }
        public int TimeoutSeconds { get; set; }

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
                    case TestStatus.Aborted:
                    case TestStatus.Failed:
                    case TestStatus.TestPassed:
                    case TestStatus.Timeout:
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
    /// This class is used to save & load TestLists from an XML file.
    /// </summary>
    [XmlRootAttribute(ElementName = "FTFXML", IsNullable = false)]
    public partial class FTFXML
    {
        public FTFXML()
        {
            TestLists = new List<TestList>();
        }

        [XmlArrayItem("TestList")]
        public List<TestList> TestLists { get; set; }

        /// <summary>
        /// Create Guids for any imported test or testlist that is missing one.
        /// Create Tests dictionary.
        /// </summary>
        private void PostDeserialize()
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
        private void PreSerialize()
        {
            foreach (var list in TestLists)
            {
                list.TestsForXml = new List<TestBase>();
                list.TestsForXml.AddRange(list.Tests.Values);
            }
        }

        public static FTFXML Load(string filename)
        {
            FTFXML xml;

            try
            {
                lock (XmlSerializeLock)
                {
                    XmlIsValid = false;
                    ValidationErrors = "";

                    if (!File.Exists(filename))
                    {
                        throw new FileNotFoundException($"{filename} does not exist!");
                    }

                    // Validate XSD
                    var asm = Assembly.GetAssembly(typeof(FTFXML));
                    using (Stream xsdStream = asm.GetManifestResourceStream(GetResourceName(Assembly.GetAssembly(typeof(FTFXML)), "FTFXML.xsd", false)))
                    {
                        XmlReaderSettings settings = new XmlReaderSettings();
                        settings.XmlResolver = null;

                        using (XmlReader xsdReader = XmlReader.Create(xsdStream, settings))
                        {
                            XmlSchema xmlSchema = xmlSchema = XmlSchema.Read(xsdReader, ValidationEventHandler);

                            using (XmlReader reader = XmlReader.Create(filename, settings))
                            {
                                XmlDocument document = new XmlDocument();
                                document.XmlResolver = null;
                                document.Schemas.Add(xmlSchema);

                                // Remove xsi:type so they are properly validated against the shared "Test" XSD type
                                document.Load(reader);
                                var tests = document.SelectNodes("//Test");
                                foreach (var testNode in tests)
                                {
                                    var removed = ((XmlNode)testNode).Attributes.RemoveNamedItem("xsi:type");
                                }
                                XmlIsValid = true;
                                document.Validate(ValidationEventHandler);
                            }

                            if (!XmlIsValid)
                            {
                                // Throw all the errors we found
                                throw new XmlSchemaValidationException(ValidationErrors);
                            }
                        }
                    }
                }

                // Deserialize
                using (XmlReader reader = XmlReader.Create(filename))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(FTFXML));
                    xml = (FTFXML)serializer.Deserialize(reader);
                }

                xml.PostDeserialize();
            }
            catch (Exception e)
            {
                throw new FileLoadException($"Could not load {filename} as FTFXML!", e);
            }

            return xml;
        }

        private static void ValidationEventHandler(object sender, ValidationEventArgs e)
        {
            if (e.Severity == XmlSeverityType.Error)
            {
                // Save the error, instead of throwing now.
                // This allows valiatation to catch mutiple errors in one pass.
                XmlIsValid = false;

                if (!String.IsNullOrEmpty(ValidationErrors))
                {
                    ValidationErrors += System.Environment.NewLine;
                }

                ValidationErrors += e.Message;
            }
        }

        private static string GetResourceName(Assembly assembly, string resourceIdentifier, bool matchWholeWord = true)
        {
            resourceIdentifier = resourceIdentifier.ToLowerInvariant();
            var ListOfResources = assembly.GetManifestResourceNames();

            foreach (string resource in ListOfResources)
            {
                if (matchWholeWord)
                {
                    if (resource.ToLowerInvariant().Equals(resourceIdentifier))
                    {
                        return resource;
                    }
                }
                else if (resource.ToLowerInvariant().Contains(resourceIdentifier))
                {
                    return resource;
                }
            }

            throw new FileNotFoundException("Could not find embedded resource", resourceIdentifier);
        }

        public bool Save(string filename)
        {
            PreSerialize();

            var xmlWriterSettings = new XmlWriterSettings() { Indent = true };
            using (XmlWriter writer = XmlWriter.Create(filename, xmlWriterSettings))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(FTFXML));
                serializer.Serialize(writer, this);
            }

            return true;
        }

        private static bool XmlIsValid { get; set; }
        private static object XmlSerializeLock = new object();
        private static string ValidationErrors;
    }
}