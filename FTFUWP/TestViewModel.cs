using Microsoft.FactoryTestFramework.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.FactoryTestFramework.UWP
{
    public class TestViewModel
    {
        public TestData TestData { get; set; }
        public TestViewModel()
        {
            TestData = new TestData
            {
                TestListMap = new Dictionary<Guid, TestList>(),
                TestGuidsMap = new Dictionary<int, Guid>(),
                TestNames = new ObservableCollection<String>(),
                TestStatus = new ObservableCollection<String>()
            };
        }

        public ObservableCollection<String> GetTestNames(Guid guid)
        {
            List<String> testNamesAndResults = new List<String>();
            foreach (var test in TestData.TestListMap[guid].Tests.Values)
            {
                if (test.LatestTestRunStatus == TestStatus.TestPassed)
                {
                    testNamesAndResults.Add(test.TestName + " ✔");
                }
                else if (test.LatestTestRunStatus == TestStatus.TestFailed)
                {
                    testNamesAndResults.Add(test.TestName + " ❌");
                }
                else
                {
                    testNamesAndResults.Add(test.TestName);
                }
            }
            return new ObservableCollection<String>(testNamesAndResults);
        }

        public void SetTestNames(Guid guid)
        {
            SetTestStatus(guid);
            TestData.TestNames = new ObservableCollection<String>(TestData.TestListMap[guid].Tests.Values.Select(x => x.TestName).ToList());
        }

        private void SetTestStatus(Guid guid)
        {
            List<String> testResults = new List<String>();
            foreach (var test in TestData.TestListMap[guid].Tests.Values)
            {
                switch (test.LatestTestRunStatus)
                {
                    case TestStatus.TestPassed:
                        testResults.Add("✔ Passed");
                        break;
                    case TestStatus.TestFailed:
                        testResults.Add("❌ Failed");
                        break;
                    case TestStatus.TestRunning:
                        testResults.Add("🕒 Running");
                        break;
                    case TestStatus.TestNotRun:
                        testResults.Add("❔ Not Run");
                        break;
                    case TestStatus.TestAborted:
                        testResults.Add("⛔ Aborted");
                        break;
                    default:
                        testResults.Add("❔ Unknown");
                        break;
                }
            }
            //return new ObservableCollection<String>(TestData.TestListMap[guid].Tests.Values.Select(x => x.TestName).ToList());
            TestData.TestStatus = new ObservableCollection<String>(testResults);
        }

        public void SetTestNames(ObservableCollection<String> testNames)
        {
            TestData.TestNames = testNames;
        }

        public void AddOrUpdateTestList(TestList testList)
        {
            if (TestData.TestListMap.ContainsKey(testList.Guid))
            {
                TestData.TestListMap[testList.Guid] = testList;

            }
            else
            {
                TestData.TestListMap.Add(testList.Guid, testList);
                TestData.TestListGuids.Add(testList.Guid);
            }

            SetTestNames(testList.Guid);
        }

        public void SetActiveTestList(Guid testListGuid)
        {
            TestData.SelectedTestListGuid = testListGuid;
            SetTests(testListGuid);
        }

        public void ClearActiveTestList()
        {
            TestData.SelectedTestListGuid = null;
            TestData.TestNames = new ObservableCollection<string>();
            TestData.TestStatus = new ObservableCollection<string>();
            TestData.TestGuidsMap = new Dictionary<int, Guid>();
        }

        public void SetTests(Guid testListGuid)
        {
            SetTestNames(testListGuid);
            SetTestGuidsMap(testListGuid);
        }

        public bool PruneKnownTestLists(List<Guid> testListGuids)
        {
            bool guidRemoved = false;

            foreach (var guid in TestData.TestListGuids)
            {
                if (!testListGuids.Contains(guid))
                {
                    TestData.TestListMap.Remove(guid);
                    guidRemoved = true;
                    if (TestData.SelectedTestListGuid == guid)
                    {
                        ClearActiveTestList();
                    }
                }
            }

            return guidRemoved;
        }

        private void SetTestGuidsMap(Guid testListGuid)
        {
            Dictionary<int, Guid> testGuidsMap = new Dictionary<int, Guid>();
            int index = 0;
            foreach (var test in TestData.TestListMap[testListGuid].Tests.Values)
            {
                testGuidsMap.Add(index, test.Guid);
                index++;
            }
            TestData.TestGuidsMap = testGuidsMap;
        }
    }
}
