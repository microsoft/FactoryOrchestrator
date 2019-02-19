using System;
using System.Collections;
using System.Collections.Generic;
using FTFTestExecution;

namespace FTFInterfaces
{
    public enum FactoryTestType
    {
        Console,
        TAEF,
        TAEFTestCase,
        UWP
    }

    public enum OutputType
    {
        StdOut,
        StdError
    }

    public class TestOutput
    {
        public string Message;
        public OutputType Type;
        public DateTime Time;
    }

    public enum FTFEventType
    {
        NewTestList,
        UpdatedTestList,
        TestStatusUpdate,
        ServiceError
    }

    public class ServiceEventDatum
    {
        public FTFEventType EventType;
        public Guid Object;
    }

    public class ServiceErrorData
    {
        public uint ErrorCode;
        public string Message;
    }

    public enum TestEvent
    {
        NotRun,
        Passed,
        Failed,
        Warning,
        Error,
        Exception,
        Aborted,
        Running,
        Unknown = int.MaxValue
    }

    public class TestEventDatum : ServiceEventDatum
    {
        public TestEvent TestEvent;
        public bool TestResult;
        public uint ErrorCode;
        public string Message;
    }

    public class TestListDatum
    {
        public TestListDatum()
        { }

        public List<TestDatum> Tests;
        public Guid Guid;

        //IEnumerator IEnumerable.GetEnumerator()
        //{
        //    return ((IEnumerable)Tests).GetEnumerator();
        //}
    }

    public class TestDatum
    {
        public string TestName;
        public string TestPath;
        public FactoryTestType TestType;
        public Guid Guid;
    }

    public class TaefTestDatum : TestDatum
    {
        public List<TestDatum> TAEFTestCases;
    }

    public interface IFTFCommunication
    {
        // create test list from list
        // create test list from folder
        // query taef test
        // run test list
        // update test list
        // run test or test case
        // get status / check for updates
        // Send UWP test results
        // get failed tests
        // get test output
        // query for all test lists

        TestList CreateTestListFromDirectory(string path, bool onlyTAEF);

        List<Guid> LoadTestListsFromXmlFile(string filePath);

        TestList CreateTestListFromTestList(TestList list);

        List<Guid> GetTestListGuids();

        TestList QueryTestList(Guid guid);

        bool DeleteTestList(Guid listToDelete);
        bool UpdateTestList(TestList testList);

        void ResetService();

        ServiceEventDatum GetServiceUpdate(List<FTFEventType> types);
        ServiceEventDatum GetServiceUpdate(FTFEventType type);
        ServiceEventDatum GetServiceUpdate();

        TestEventDatum GetExecutionStatus(Guid guid);

        bool Run(Guid TestListToRun, bool allowOtherTestListsToRun, bool runListInParallel);

        bool UpdateTestStatus(TestBase latestTestStatus);

        TestOutput GetAllOutput(Guid guid);

        List<TestOutput> GetLatestOutput(Guid guid, DateTime fromTime);

        TestOutput GetErrors(Guid guid, ulong index);

        bool SetDefaultTePath(string teExePath);

        bool SetDefaultLogFolder(string logFolder);
    }
}
