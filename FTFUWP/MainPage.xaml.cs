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
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
            this.TestViewModel= new TestViewModel();
            this.DataContext = TestViewModel;
#if DEBUG
            DisablePolling.Visibility = Visibility.Visible;
#endif
            // create testlist in service, not working rn
            //foreach (TestList tl in TestViewModel.TestData.TestListMap.Values)
            //{
            // TODO: Move to a place this works properly, likely viewmodel, since it tracks testlists, or just nuke it now, since we can use real data
            //foreach (TestList tl in TestViewModel.TestData.TestListMap.Values)
            //{
            //    ((App)Application.Current).IpcClient.InvokeAsync(x => x.CreateTestListFromTestList(tl));
            //}
            //}
            //await((App)(Application.Current)).IpcClient.InvokeAsync(x => x.CreateTestListFromTestList(TestViewModel.TestData.TestListMap);
            MakeTestLists();
            // We generate 10 TestLists each with 100 Tests, every 5 tests pass and the rest fail
        }

        public async void MakeTestLists()
        {
            foreach (TestList tl in TestViewModel.TestData.TestListMap.Values)
            {
                await IPCClientHelper.IpcClient.InvokeAsync(x => x.CreateTestListFromTestList(tl));
            }
        }

        public TestViewModel TestViewModel { get; set; }

        private ObservableCollection<String> GetTestNames(Guid guid)
        {
            return TestViewModel.GetTestNames(guid);
        }

        private async void SetTestNames(Guid guid)
        {

            TestViewModel.SetTestNames(guid);
            await IPCClientHelper.IpcClient.InvokeAsync(x => x.CreateTestListFromTestList(TestViewModel.TestData.TestListMap[guid]));
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
                _selectedTestList = TestListsView.SelectedIndex;
                SetTestListGuid(testListGuid);
                _poller = new FTFPoller(testListGuid, typeof(TestList), IPCClientHelper.IpcClient, 5000);
                _poller.OnUpdatedObject += OnUpdatedTestListAsync;
#if DEBUG
                if ((DisablePolling.IsChecked != null) && (bool)(!DisablePolling.IsChecked))
#endif
                {
                    _poller.StartPolling();
                }
            }
        }

        private void SetTestListGuid(Guid testListGuid)
        {
            TestViewModel.SetTestListGuid(testListGuid);
        }

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            if (TestListsView.SelectedItem != null)
            {
                if (RunButton.Symbol == Symbol.Play)
                {
                    RunButton.Symbol = Symbol.Stop;
                    Guid testListGuid = (Guid)TestListsView.SelectedItem;
                    await IPCClientHelper.IpcClient.InvokeAsync(x => x.Run(testListGuid, false, (bool)RunListInParallel.IsChecked));
                }
                else if (RunButton.Symbol == Symbol.Stop)
                {
                    // call Stop Test API
                    RunButton.Symbol = Symbol.Play;
                }
            }
        }

        private void TestsView_SelectionChanged(object sender, RoutedEventArgs e)
        {
            // TODO: Use TestRun when it exists
            ListView control = (ListView)sender;
            int index = control.SelectedIndex;
            if (index != -1)
            {
                var testGuid = TestViewModel.TestData.TestGuidsMap[index];
                TestBase test = TestViewModel.TestData.TestListMap[TestViewModel.TestData.SelectedTestListGuid].Tests[testGuid];
                ////TESTCODE
                //var test = new ExecutableTest("foo.dll")
                //{
                //    LastTimeStarted = DateTime.Now - TimeSpan.FromMinutes(2),
                //    LastTimeFinished = DateTime.Now,
                //    TestStatus = TestStatus.TestPassed,
                //    IsEnabled = true,
                //    Arguments = "/arg1:anc /arg2:ghbrigsdr",
                //    TestOutput = new List<string>()
                //}; // TODO: get the test / testrun based on the guid
                //for (int i = 0; i < 1000; i++)
                //{
                //    test.TestOutput.Add("Line " + i + ":" + Guid.NewGuid().ToString() + Guid.NewGuid().ToString() + Guid.NewGuid().ToString() + Guid.NewGuid().ToString() + Guid.NewGuid().ToString() + Guid.NewGuid().ToString());
                //}
                this.Frame.Navigate(typeof(ResultsPage), test);
            }
        }

        private async void OnUpdatedTestListAsync(object source, FTFPollEventArgs e)
        {
            // TODO: call updateui api to update the testlist the viewmodel uses
            if (e.Result != null)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    SetTestList((TestList)e.Result);
                });
            }
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: This is a hack so that if you click on the same test again after returning from the results page the selection is changed
            TestsView.SelectedIndex = -1;

            if (_poller != null)
            {
#if DEBUG
                if ((DisablePolling.IsChecked != null) && (bool)(!DisablePolling.IsChecked))
#endif
                {
                    _poller.StartPolling();
                }
            }

            if (_selectedTestList != -1)
            {
                TestListsView.SelectedIndex = _selectedTestList;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (_poller != null)
            {
                _poller.StopPolling();
            }
        }

        private FTFPoller _poller;
        private int _selectedTestList = -1;

        private async void LoadFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var testlist = await IPCClientHelper.IpcClient.InvokeAsync(x => x.CreateTestListFromDirectory(FolderToLoad.Text, false));
            SetTestList(testlist);
            TestListsView.ItemsSource = TestViewModel.TestData.TestListGuids;
            TestViewModel.TestData.SelectedTestListGuid = testlist.Guid;
            TestListsView.SelectedItem = testlist.Guid;
        }

        private void DisablePolling_Click(object sender, RoutedEventArgs e)
        {
            if ((DisablePolling.IsChecked != null) && (bool)(DisablePolling.IsChecked))
            {
                if (_poller != null)
                {
                    _poller.StopPolling();
                }
            }
            else
            {
                if (_poller != null)
                {
                    _poller.StartPolling();
                }
            }
        }
    }
}
