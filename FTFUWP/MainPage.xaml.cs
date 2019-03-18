using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using Microsoft.FactoryTestFramework.Client;
using Microsoft.FactoryTestFramework.Core;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Microsoft.FactoryTestFramework.UWP
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
            //MakeTestLists();
            // We generate 10 TestLists each with 100 Tests, every 5 tests pass and the rest fail
        }

        public async void MakeTestLists()
        {
            foreach (TestList tl in TestViewModel.TestData.TestListMap.Values)
            {
                await IPCClientHelper.IpcClient.InvokeAsync(x => x.CreateTestListFromTestList(tl));
            }
        }

        public async void Check()
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
                _activeListPoller = new FTFPoller(testListGuid, typeof(TestList), IPCClientHelper.IpcClient, 5000);
                _activeListPoller.OnUpdatedObject += OnUpdatedTestListAsync;
#if DEBUG
                if ((DisablePolling.IsChecked != null) && (bool)(!DisablePolling.IsChecked))
#endif
                {
                    _activeListPoller.StartPolling();
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
                    await IPCClientHelper.IpcClient.InvokeAsync(x => x.StopAll());
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
            if (e.Result != null)
            {
                TestList list = (TestList)e.Result;
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    
                    SetTestList(list);

                    if (list.TestListStatus == TestStatus.TestRunning)
                    {
                        RunButton.Symbol = Symbol.Stop;
                    }
                    else
                    {
                        RunButton.Symbol = Symbol.Play;
                    }
                });
            }
        }

        private async void OnUpdatedTestListGuidsAsync(object source, FTFPollEventArgs e)
        {
            var testListGuids = e.Result as List<Guid>;

            if (testListGuids != null)
            {
                foreach (var guid in testListGuids)
                {
                    if (!TestViewModel.TestData.TestListGuids.Contains(guid))
                    {
                        var list = await IPCClientHelper.IpcClient.InvokeAsync(x => x.QueryTestList(guid));
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            SetTestList(list);
                            TestListsView.ItemsSource = TestViewModel.TestData.TestListGuids;
                            TestViewModel.TestData.SelectedTestListGuid = list.Guid;
                            TestListsView.SelectedItem = list.Guid;
                        });
                    }
                }
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: This is a hack so that if you click on the same test again after returning from the results page the selection is changed
            TestsView.SelectedIndex = -1;

            if (_activeListPoller != null)
            {
#if DEBUG
                if ((DisablePolling.IsChecked != null) && (bool)(!DisablePolling.IsChecked))
#endif
                {
                    _activeListPoller.StartPolling();
                }
            }

            if (_testListGuidPoller == null)
            {
                _testListGuidPoller = new FTFPoller(null, typeof(TestList), IPCClientHelper.IpcClient, 2000);
                _testListGuidPoller.OnUpdatedObject += OnUpdatedTestListGuidsAsync;
            }

#if DEBUG
            _testListGuidPoller.StartPolling();
#endif

            if (_selectedTestList != -1)
            {
                TestListsView.SelectedIndex = _selectedTestList;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (_activeListPoller != null)
            {
                _activeListPoller.StopPolling();
            }

            _testListGuidPoller.StopPolling();
        }

        private FTFPoller _activeListPoller;
        private FTFPoller _testListGuidPoller;
        private int _selectedTestList = -1;

        private async void LoadFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var testlist = await IPCClientHelper.IpcClient.InvokeAsync(x => x.CreateTestListFromDirectory(FolderToLoad.Text, false));
        }

        private void DisablePolling_Click(object sender, RoutedEventArgs e)
        {
            if ((DisablePolling.IsChecked != null) && (bool)(DisablePolling.IsChecked))
            {
                if (_activeListPoller != null)
                {
                    _activeListPoller.StopPolling();
                }

                _testListGuidPoller.StopPolling();
            }
            else
            {
                if (_activeListPoller != null)
                {
                    _activeListPoller.StartPolling();
                }

                _testListGuidPoller.StartPolling();
            }
        }
    }
}
