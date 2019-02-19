using FTFInterfaces;
using FTFTestExecution;
using JKang.IpcServiceFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Collections.ObjectModel;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace FTFUWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        IpcServiceClient<IFTFCommunication> client;
        private Dictionary<Guid, TestList> testListMap = new Dictionary<Guid, TestList>();
        public ObservableCollection<Guid> TestListGuids { get { return new ObservableCollection<Guid>(testListMap.Keys); } }

        private ObservableCollection<String> testNames = new ObservableCollection<String>();
        public ObservableCollection<String> TestNames { get { return testNames; } }

        public MainPage()
        {
            this.InitializeComponent();

            client = new IpcServiceClientBuilder<IFTFCommunication>()
                .UseTcp(IPAddress.Loopback, 45684)
                .Build();
            // We generate 10 TestLists each with 100 Tests, every 5 tests pass and the rest fail
            GetTestListMapAsync();
        }

        private async System.Threading.Tasks.Task<TestList> GetTestListAsync()
        {
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
                t.Tests.Add(g.Guid, new Tuple<ExecutableTest, bool>(g, true));
            }
            return t;
        }

        private async System.Threading.Tasks.Task<Dictionary<Guid, TestList>> GetTestListMapAsync()
        {
            for (int i = 0; i < 10; i++)
            {
                TestList t = await GetTestListAsync();
                testListMap.Add(t.Guid, t);
            }
            return testListMap;
        }

        private ObservableCollection<String> GetTestNames(Guid guid)
        {
            return new ObservableCollection<String>(testListMap[guid].Tests.Values.Select(x => x.Item1.TestName).ToList());
        }

        private void TestListsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TestListsView.SelectedItem != null)
            {
                Guid testListGuid = (Guid)TestListsView.SelectedItem;
                testNames = GetTestNames(testListGuid);
                TestsView.Items.Clear();
                foreach (String s in testNames)
                {
                    TestsView.Items.Add(s);
                }
            }
        }

        //void Run_Button_Click(object sender, RoutedEventArgs e)
        //{
        //    // right now, this just prints all of the test data results
        //    // will really run tests and print results
        //    if (TestListsView.SelectedItem != null)
        //    {
        //        Guid guid = (Guid)TestListsView.SelectedItem;
        //        ResultsView.Items.Clear();
        //        foreach (var test in testListMap[guid].Tests)
        //        {
        //            ResultsView.Items.Add(test.Value.Item1.TestStatus);
        //        }
        //    }
        //}

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            // right now, this just prints all of the test data results
            // will really run tests and print results
            if (TestListsView.SelectedItem != null)
            {
                Guid guid = (Guid)TestListsView.SelectedItem;
                ResultsView.Items.Clear();
                foreach (var test in testListMap[guid].Tests)
                {
                    ResultsView.Items.Add(test.Value.Item1.TestStatus);
                }
            }
        }
    }
}
