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
            TestData = new TestData();
            TestData.TestListMap = GetTestListMapAsync();
            TestData.TestNames = new ObservableCollection<String>();
            TestData.TestStatus = new ObservableCollection<TestStatus>();
        }

        private TestList GetTestListAsync()
        {
            //TODO: make this properly async

            //var tests = await client.InvokeAsync(x => x.CreateTestListFromDirectory("c:\\data\\tests\\", false));
            //bool result = await client.InvokeAsync(x => x.Run(tests.Guid, false, false));

            // Test code to format UI
            TestList t = new TestList(Guid.NewGuid());
            for (int i = 0; i < 100; i++)
            {
                TAEFTest g = new TAEFTest(i + "foo.dll")
                {
                    LastTimeRun = DateTime.Now,
                    ExitCode = 1
                };
                if (i % 5 == 0)
                {
                    g.TestStatus = TestStatus.TestPassed;
                }
                else
                {
                    g.TestStatus = TestStatus.TestFailed;
                }
                t.Tests.Add(g.Guid, g);
            }
            return t;
        }

        private Dictionary<Guid, TestList> GetTestListMapAsync()
        {
            // TODO: Make this properly async
            Dictionary<Guid, TestList> testListMap = new Dictionary<Guid, TestList>();
            for (int i = 0; i < 10; i++)
            {
                TestList tl = GetTestListAsync();
                testListMap.Add(tl.Guid, tl);
            }
            return testListMap;
        }
    }
}
