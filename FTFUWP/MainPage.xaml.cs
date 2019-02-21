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
            // We generate 10 TestLists each with 100 Tests, every 5 tests pass and the rest fail
        }

        public TestViewModel TestViewModel { get; set; }

        private ObservableCollection<String> GetTestNames(Guid guid)
        {
            return new ObservableCollection<String>(TestViewModel.TestData.TestListMap[guid].Tests.Values.Select(x => x.TestName).ToList());
        }

        private void TestListsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TestListsView.SelectedItem != null)
            {
                Guid testListGuid = (Guid)TestListsView.SelectedItem;
                _poller = new TestListPoller(testListGuid, ((App)Application.Current).IpcClient);
                _poller.OnUpdatedTestList += OnUpdatedTestList;
                _poller.StartPolling();
                TestViewModel.TestData.TestNames = GetTestNames(testListGuid);
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
                TestViewModel.TestData.TestNames = new ObservableCollection<String>(testNamesAndResults);
            }
        }

        private void OnUpdatedTestList(object source, TestListPollEventArgs e)
        {
            // call updateui api to update the testlist the viewmodel uses
        }

        private TestListPoller _poller;
    }
}
