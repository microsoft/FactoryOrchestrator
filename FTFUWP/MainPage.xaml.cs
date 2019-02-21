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
using System.ComponentModel;
using FTFClient;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace FTFUWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        public MainPage()
        {
            this.InitializeComponent();
            this.TestViewModel= new TestViewModel();
            this.DataContext = TestViewModel;
            // create testlist in service, not working rn
            //foreach (TestList tl in TestViewModel.TestData.TestListMap.Values)
            //{
            //    ((App)Application.Current).IpcClient.InvokeAsync(x => x.CreateTestListFromTestList(tl));
            //}
            //await((App)(Application.Current)).IpcClient.InvokeAsync(x => x.CreateTestListFromTestList(TestViewModel.TestData.TestListMap);
            // We generate 10 TestLists each with 100 Tests, every 5 tests pass and the rest fail
        }

        public TestViewModel TestViewModel { get; set; }

        private ObservableCollection<String> GetTestNames(Guid guid)
        {
            return TestViewModel.GetTestNames(guid);
        }

        private void SetTestNames(Guid guid)
        {
            TestViewModel.SetTestNames(guid);
        }

        private void SetTestNames(ObservableCollection<String> testNames)
        {
            TestViewModel.SetTestNames(testNames);
        }

        private void SetTestList(TestList testList)
        {
            TestViewModel.SetTestList(testList);
        }

        private void TestListsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TestListsView.SelectedItem != null)
            {
                Guid testListGuid = (Guid)TestListsView.SelectedItem;
<<<<<<< HEAD
                // comment out polling code to test UI, currently crashes after selecting a testlist
                _poller = new TestListPoller(testListGuid, ((App)Application.Current).IpcClient);
                _poller.OnUpdatedTestList += OnUpdatedTestList;
                _poller.StartPolling();
                SetTestNames(testListGuid);
                //TestViewModel.TestData.TestNames = GetTestNames(testListGuid);
=======
                //_poller = new TestListPoller(testListGuid, ((App)Application.Current).IpcClient);
                //_poller.OnUpdatedTestList += OnUpdatedTestList;
                //_poller.StartPolling();
                TestViewModel.TestData.TestNames = GetTestNames(testListGuid);
>>>>>>> Results page should work, but cant test because bad OS flight
            }
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            // right now, this just prints all of the test data results
            // will really run tests and print results
            if (TestListsView.SelectedItem != null)
            {
                Guid guid = (Guid)TestListsView.SelectedItem;

                List<String> testNamesAndResults = new List<String>();
                foreach (var test in TestViewModel.TestData.TestListMap[guid].Tests.Values)
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
                        testNamesAndResults.Add(test.TestName + " ❓");
                    }
                }
                SetTestNames(new ObservableCollection<String>(testNamesAndResults));
            }
        }

        private void ResultsButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Use TestRun when it exists
            //Button button = (Button)sender;
            //var testGuid = new Guid(button.Name); // TODO: ensure all buttons have a name mapped to their guid
            var test = new ExecutableTest("foo.dll")
            {
                LastTimeStarted = DateTime.Now - TimeSpan.FromMinutes(2),
                LastTimeFinished = DateTime.Now,
                TestStatus = TestStatus.TestPassed,
                IsEnabled = true,
                Arguments = "/arg1:anc /arg2:ghbrigsdr",
                TestOutput = new List<string>()
            }; // TODO: get the test / testrun based on the guid
            for (int i = 0; i < 1000; i++)
            {
                test.TestOutput.Add("Line " + i + ":" + Guid.NewGuid().ToString() + Guid.NewGuid().ToString());
            }
            this.Frame.Navigate(typeof(ResultsPage), test);
        }

        private void OnUpdatedTestList(object source, TestListPollEventArgs e)
        {
            // TODO: call updateui api to update the testlist the viewmodel uses
            SetTestList(e.TestList);
            SetTestNames(e.TestList.Guid);
        }

        private TestListPoller _poller;
    }
}
