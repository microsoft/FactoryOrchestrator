using FTFTestExecution;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FTFUWP
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
                TestStatus = new ObservableCollection<String>(),
                TestListGuids = new ObservableCollection<Guid>()
            };
        }

        //private TestList GetTestListAsync()
        //{
        //    //TODO: make this properly async

        //    //var tests = await client.InvokeAsync(x => x.CreateTestListFromDirectory("c:\\data\\tests\\", false));
        //    //bool result = await client.InvokeAsync(x => x.Run(tests.Guid, false, false));

        //    // Test code to format UI
        //    TestList t = new TestList(Guid.NewGuid());
        //    for (int i = 0; i < 100; i++)
        //    {
        //        TAEFTest g = new TAEFTest(i + "foo.dll")
        //        {
        //            LastTimeStarted = DateTime.Now,
        //            LastTimeFinished = DateTime.Now + TimeSpan.FromMinutes(1),
        //        };
        //        if (i % 5 == 0)
        //        {
        //            g.TestStatus = TestStatus.TestPassed;
        //            g.ExitCode = 0;
        //        }
        //        else
        //        {
        //            g.TestStatus = TestStatus.TestFailed;
        //            g.ExitCode = new Random().Next();
        //        }
        //        t.Tests.Add(g.Guid, g);
        //    }
        //    return t;
        //}

        //private Dictionary<Guid, TestList> GetTestListMapAsync()
        //{
        //    // TODO: Make this properly async
        //    Dictionary<Guid, TestList> testListMap = new Dictionary<Guid, TestList>();
        //    for (int i = 0; i < 10; i++)
        //    {
        //        TestList tl = GetTestListAsync();
        //        testListMap.Add(tl.Guid, tl);
        //    }
        //    return testListMap;
        //}

        public ObservableCollection<String> GetTestNames(Guid guid)
        {
            List<String> testNamesAndResults = new List<String>();
            foreach (var test in TestData.TestListMap[guid].Tests.Values)
            {
                if (test.TestStatus == TestStatus.TestPassed)
                {
                    testNamesAndResults.Add(test.TestName + " ✔");
                }
                else if (test.TestStatus == TestStatus.TestFailed)
                {
                    testNamesAndResults.Add(test.TestName + " ❌");
                }
                else
                {
                    testNamesAndResults.Add(test.TestName);
                }
            }
            //return new ObservableCollection<String>(TestData.TestListMap[guid].Tests.Values.Select(x => x.TestName).ToList());
            return new ObservableCollection<String>(testNamesAndResults);
        }

        public void SetTestNames(Guid guid)
        {
            //ObservableCollection<String> testNames = GetTestNames(guid);
            SetTestStatus(guid);
            TestData.TestNames = new ObservableCollection<String>(TestData.TestListMap[guid].Tests.Values.Select(x => x.TestName).ToList());
        }

        private void SetTestStatus(Guid guid)
        {
            List<String> testResults = new List<String>();
            foreach (var test in TestData.TestListMap[guid].Tests.Values)
            {
                if (test.TestStatus == TestStatus.TestPassed)
                {
                    testResults.Add("✔");
                }
                else if (test.TestStatus == TestStatus.TestFailed)
                {
                    testResults.Add("❌");
                }
            }
            //return new ObservableCollection<String>(TestData.TestListMap[guid].Tests.Values.Select(x => x.TestName).ToList());
            TestData.TestStatus = new ObservableCollection<String>(testResults);
        }

        public void SetTestNames(ObservableCollection<String> testNames)
        {
            TestData.TestNames = testNames;
        }

        public void SetTestList(TestList testList)
        {
            if (TestData.TestListMap.ContainsKey(testList.Guid))
            {
                TestData.TestListMap[testList.Guid] = testList;

            } else
            {
                TestData.TestListMap.Add(testList.Guid, testList);
                TestData.TestListGuids.Add(testList.Guid);
            }

            SetTestNames(testList.Guid);
        }

        public void SetTestListGuid(Guid testListGuid)
        {
            TestData.SelectedTestListGuid = testListGuid;
            SetTests(testListGuid);
        }

        public void SetTests(Guid testListGuid)
        {
            SetTestNames(testListGuid);
            SetTestGuidsMap(testListGuid);
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
