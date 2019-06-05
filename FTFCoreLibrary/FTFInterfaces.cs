using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.FactoryTestFramework.Core
{
    public enum ServiceEventType
    {
        NewTestList = 0,
        UpdatedTestList = 1,
        DeletedTestList = 2,
        TestListRunStarted = 3,
        TestListRunEnded = 4,
        DoneWaitingForExternalTestRun = 5,
        WaitingForExternalTestRun = 6,
        ServiceReset = 7,
        ServiceError = 8,
        ServiceStarted = 9,
        ServiceStopped = 10,
        Unknown = 11
    }

    public class ServiceEvent
    {
        [JsonConstructor]
        public ServiceEvent()
        {
            _guidStr = "";
            _eventType = ServiceEventType.Unknown;
        }

        public ServiceEvent(ServiceEventType type, Guid? guid, String message)
        {
            _eventIndex = _indexCount++;
            _eventTime = DateTime.Now;
            _eventType = type;
            if (guid != null)
            {
                _guidStr = guid.ToString();
            }
            else
            {
                _guidStr = "";
            }

            _message = message;
        }


        [JsonRequired]
        public ulong EventIndex { get => _eventIndex; }

        public DateTime EventTime { get => _eventTime; }

        public ServiceEventType ServiceEventType { get => _eventType; }

        public Guid? Guid
        {
            get
            {
                if (!String.IsNullOrWhiteSpace(_guidStr))
                {
                    return new Guid(_guidStr);
                }
                else
                {
                    return null;
                }
            }
        }

        public String Message { get => _message; }

        [JsonRequired]
        private ulong _eventIndex;

        [JsonRequired]
        private ServiceEventType _eventType;

        [JsonRequired]
        private DateTime _eventTime;

        [JsonRequired]
        private string _message;

        [JsonRequired]
        private string _guidStr;

        private static ulong _indexCount = 0;
    }

    // TODO: FeatureWork: Build out client-side lib for diffs, update polling state machine etc
    /// <summary>
    /// IFTFCommunication defines the client <> server communication model. 
    /// </summary>
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

        // Service APIs
        void ResetService(bool preserveLogs = true);
        List<ServiceEvent> GetAllServiceEvents();
        List<ServiceEvent> GetServiceEventsByTime(DateTime timeLastChecked);
        List<ServiceEvent> GetServiceEventsByIndex(ulong lastEventIndex);
        ServiceEvent GetLastServiceError();
        string GetServiceVersionString();
        bool SetDefaultTePath(string teExePath);
        bool SetDefaultLogFolder(string logFolder, bool moveExistingLogs);

        // Test List APIs
        TestList CreateTestListFromDirectory(string path, bool onlyTAEF);
        List<Guid> LoadTestListsFromXmlFile(string filename);
        bool SaveTestListToXmlFile(Guid guid, string filename);
        bool SaveAllTestListsToXmlFile(string filename);
        TestList CreateTestListFromTestList(TestList list);
        List<Guid> GetTestListGuids();
        TestList QueryTestList(Guid guid);
        bool DeleteTestList(Guid listToDelete);
        bool UpdateTestList(TestList testList);

        // Test APIs
        TestBase QueryTest(Guid guid);

        List<string> GetInstalledApps();

        // Test Execution APIs
        bool RunTestList(Guid TestListToRun);
        void AbortAllTestLists();
        void AbortTestList(Guid testListGuid);
        void AbortTestRun(Guid testRunGuid);
        TestRun RunExecutableAsBackgroundTask(string exeFilePath, string arguments, string consoleLogFilePath = null);
        TestRun RunUWPOutsideTestList(string packageFamilyName);
        TestRun RunTestOutsideTestList(Guid testGuid);

        // Test Run APIs
        bool SetTestRunStatus(TestRun testRunStatus);
        TestRun QueryTestRun(Guid testRunGuid);

        // File APIs
        byte[] GetFile(string sourceFilename);
        bool SendFile(string targetFilename, byte[] fileData);
    }
}
