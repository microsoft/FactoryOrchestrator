﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.FactoryTestFramework.Core
{
    public enum ServiceEventType
    {
        NewTestList,
        UpdatedTestList,
        DeletedTestList,
        ServiceReset,
        TestListRunStarted,
        TestListRunEnded,
        TestStatusUpdatedByClient,
        WaitingForTestRunByClient,
        ServiceError,
        ServiceStarted,
        ServiceStopped
    }

    public class ServiceEvent
    {
        ServiceEventType ServiceEventType { get; }
        Guid Guid { get; }
        String Message { get; }
    }

    // TODO: Build out client-side lib for diffs, update polling state machine etc
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
        List<ServiceEvent> GetServiceEvents(DateTime timeLastChecked, ServiceEventType serviceEventType);
        List<ServiceEvent> GetServiceEvents(long lastEventIndex, ServiceEventType serviceEventType);
        string GetServiceVersionString();
        TestRun RunExecutableOutsideTestList(string exeFilePath, string arguments, string consoleLogFilePath = null);
        TestRun RunTestOutsideTestList(Guid executableTestGuid);

        // Test List APIs
        TestList CreateTestListFromDirectory(string path, bool onlyTAEF);
        List<Guid> LoadTestListsFromXmlFile(string filePath);
        TestList CreateTestListFromTestList(TestList list);
        List<Guid> GetTestListGuids();
        TestList QueryTestList(Guid guid);
        TestBase QueryTest(Guid guid);
        bool DeleteTestList(Guid listToDelete);
        bool UpdateTestList(TestList testList);

        // Test Execution APIs
        bool Run(Guid TestListToRun, bool allowOtherTestListsToRun, bool runListInParallel);
        void StopAll();
        void Stop(Guid testListGuid);
        bool SetDefaultTePath(string teExePath);
        bool SetDefaultLogFolder(string logFolder);

        // Test Run APIs
        bool SetTestRunStatus(TestRun testRunStatus);
        TestRun QueryTestRun(Guid testRunGuid);
    }
}
