using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using Microsoft.FactoryTestFramework.Client;
using Microsoft.FactoryTestFramework.Core;
using System.Linq;
using System.Threading;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Microsoft.FactoryTestFramework.UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TestListExecutionPage : Page
    {

        public TestListExecutionPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
            this.TestViewModel = new TestViewModel();
            this.DataContext = TestViewModel;
            _listUpdateSem = new SemaphoreSlim(1, 1);
            _selectedTestList = -1;
            mainPage = null;
#if DEBUG
            DisablePolling.Visibility = Visibility.Visible;
#endif
        }

        public TestViewModel TestViewModel { get; set; }

        private void TestListsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TestListsView.SelectedItem != null)
            {
                Guid testListGuid = (Guid)TestListsView.SelectedItem;
                _selectedTestList = TestListsView.SelectedIndex;
                TestViewModel.SetActiveTestList(testListGuid);
                if (_activeListPoller != null)
                {
                    _activeListPoller.StopPolling();
                }
                _activeListPoller = new FTFPoller(testListGuid, typeof(TestList), IPCClientHelper.IpcClient, 2000);
                _activeListPoller.OnUpdatedObject += OnUpdatedTestListAsync;
#if DEBUG
                if ((DisablePolling.IsChecked != null) && (bool)(!DisablePolling.IsChecked))
#endif
                {
                    _activeListPoller.StartPolling();
                }
            }
        }

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            if (TestListsView.SelectedItem != null)
            {
                if (RunButtonIcon.Symbol == Symbol.Play)
                {
                    RunButtonIcon.Symbol = Symbol.Stop;
                    Guid testListGuid = (Guid)TestListsView.SelectedItem;
                    await IPCClientHelper.IpcClient.InvokeAsync(x => x.Run(testListGuid, false, (bool)RunListInParallel.IsChecked));
                }
                else if (RunButtonIcon.Symbol == Symbol.Stop)
                {
                    // call Stop Test API
                    await IPCClientHelper.IpcClient.InvokeAsync(x => x.StopAll());
                    RunButtonIcon.Symbol = Symbol.Play;
                }
            }
        }

        private void TestsView_SelectionChanged(object sender, RoutedEventArgs e)
        {
            ListView control = (ListView)sender;
            int index = control.SelectedIndex;
            if (index != -1)
            {
                var testGuid = TestViewModel.TestData.TestGuidsMap[index];
                TestBase test = TestViewModel.TestData.TestListMap[(Guid)TestViewModel.TestData.SelectedTestListGuid].Tests[testGuid];
                ////TESTCODE
                //var test = new ExecutableTest("foo.dll")
                //{
                //    LastTimeStarted = DateTime.Now - TimeSpan.FromMinutes(2),
                //    LastTimeFinished = DateTime.Now,
                //    TestStatus = TestStatus.TestPassed,
                //    IsEnabled = true,
                //    Arguments = "/arg1:anc /arg2:ghbrigsdr",
                //    TestOutput = new List<string>()
                //}; // old: get the test / testrun based on the guid
                //for (int i = 0; i < 1000; i++)
                //{
                //    test.TestOutput.Add("Line " + i + ":" + Guid.NewGuid().ToString() + Guid.NewGuid().ToString() + Guid.NewGuid().ToString() + Guid.NewGuid().ToString() + Guid.NewGuid().ToString() + Guid.NewGuid().ToString());
                //}

                // Navigate from the MainPage frame so this is a "full screen" page
                mainPage.Navigate(typeof(ResultsPage), test);
            }
        }

        private async void OnUpdatedTestListAsync(object source, FTFPollEventArgs e)
        {
            if (e.Result != null)
            {
                TestList list = (TestList)e.Result;
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {

                    TestViewModel.AddOrUpdateTestList(list);

                    if (list.TestListStatus == TestStatus.Running)
                    {
                        RunButtonIcon.Symbol = Symbol.Stop;
                    }
                    else
                    {
                        RunButtonIcon.Symbol = Symbol.Play;
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
                        _listUpdateSem.Wait();
                        if (!TestViewModel.TestData.TestListGuids.Contains(guid))
                        {
                            var list = await IPCClientHelper.IpcClient.InvokeAsync(x => x.QueryTestList(guid));

                            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                            {
                                TestViewModel.AddOrUpdateTestList(list);
                                TestListsView.ItemsSource = TestViewModel.TestData.TestListGuids;
                                if (TestListsView.SelectedItem == null)
                                {
                                    TestViewModel.TestData.SelectedTestListGuid = list.Guid;
                                    TestListsView.SelectedItem = list.Guid;
                                }
                            });
                        }

                        _listUpdateSem.Release();
                    }
                }
                
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    if (TestViewModel.PruneKnownTestLists(testListGuids))
                    {
                        TestListsView.ItemsSource = TestViewModel.TestData.TestListGuids;
                    }
                });
                
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            mainPage = (Frame)e.Parameter;

            // TODO: Quality: This is a hack so that if you click on the same test again after returning from the results page the selection is changed
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
            if ((DisablePolling.IsChecked != null) && (bool)(!DisablePolling.IsChecked))
#endif
            {
                _testListGuidPoller.StartPolling();
            }

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

        private Frame mainPage;
        private FTFPoller _activeListPoller;
        private FTFPoller _testListGuidPoller;
        private int _selectedTestList;
        private SemaphoreSlim _listUpdateSem;
    }
}
