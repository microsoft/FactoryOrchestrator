using Microsoft.FactoryTestFramework.Core;
using Microsoft.FactoryTestFramework.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using System.Threading.Tasks;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Microsoft.FactoryTestFramework.UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EditPage : Page
    {
        public EditPage()
        {
            this.InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            BackButton.IsEnabled = this.Frame.CanGoBack;

            if (e.Parameter != null)
            {
                isNewList = false;
                activeList = (TestList)e.Parameter;
            }
            else
            {
                isNewList = true;
                activeList = new TestList(Guid.NewGuid());
            }

            AppComboBox.ItemsSource = await IPCClientHelper.IpcClient.InvokeAsync(x => x.GetInstalledApps());
            ParallelCheck.IsChecked = activeList.RunInParallel;
            BlockingCheck.IsChecked = activeList.AllowOtherTestListsToRun;
            TestListHeader.Text = $"Editing TestList {activeList.Guid}";
            TestsCollection = new ObservableCollection<TestBase>(activeList.Tests.Values);
            TestListView.ItemsSource = TestsCollection;

            listEdited = false;
        }

        private async void Back_Click(object sender, RoutedEventArgs e)
        {
            await On_BackRequested();
        }

        private async Task<bool> On_BackRequested()
        {
            if (this.Frame.CanGoBack)
            {
                if (listEdited)
                {
                    ContentDialog deleteFileDialog = new ContentDialog
                    {
                        Title = "Save TestList?",
                        Content = "Do you want to save your changes?",
                        PrimaryButtonText = "Yes",
                        CloseButtonText = "No"
                    };

                    ContentDialogResult result = await deleteFileDialog.ShowAsync();

                    if (result == ContentDialogResult.Primary)
                    {
                        SaveTestList();
                    }
                }

                this.Frame.GoBack();
                return true;
            }
            return false;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            TestsCollection.Remove(GetTestFromButton(sender as Button));
            listEdited = true;
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            activeTest = GetTestFromButton(sender as Button);
            activeTestIndex = TestsCollection.IndexOf(activeTest);
            ConfigureFlyout(activeTest.TestType);
            EditFlyout.ShowAt(LayoutRoot, new FlyoutShowOptions() { Placement = FlyoutPlacementMode.Full,
                                                                    ShowMode = FlyoutShowMode.Standard});
        }

        private TestBase CreateTestFromFlyout(TestType testType)
        {
            if (activeTest == null)
            {
                activeTestIndex = -1;
                switch (testType)
                {
                    case TestType.ConsoleExe:
                        activeTest = new ExecutableTest(TestPathBox.Text);
                        break;
                    case TestType.UWP:
                        activeTest = new UWPTest(AppComboBox.SelectedItem.ToString());
                        break;
                    case TestType.External:
                        activeTest = new ExternalTest(TestPathBox.Text);
                        break;
                    case TestType.TAEFDll:
                        activeTest = new TAEFTest(TestPathBox.Text);
                        break;
                }
            }

            activeTest.TestName = TestNameBox.Text;

            if (TimeoutBox.Text != "")
            {
                try
                {
                    activeTest.TimeoutSeconds = Int32.Parse(TimeoutBox.Text);
                }
                catch (Exception)
                {
                    activeTest.TimeoutSeconds = -1;
                }
            }
            else
            {
                activeTest.TimeoutSeconds = -1;
            }

            switch (testType)
            {
                case TestType.ConsoleExe:
                    var exeTest = activeTest as ExecutableTest;
                    exeTest.TestPath = TestPathBox.Text;
                    exeTest.Arguments = ArgumentsBox.Text;
                    exeTest.BackgroundTask = (bool)BackgroundCheck.IsChecked;
                    break;
                case TestType.UWP:
                    var uwpTest = activeTest as UWPTest;
                    uwpTest.TestPath = AppComboBox.SelectedItem.ToString();
                    break;
                case TestType.External:
                    var externalTest = activeTest as ExternalTest;
                    externalTest.TestPath = TestPathBox.Text;
                    externalTest.Arguments = ArgumentsBox.Text;
                    break;
                case TestType.TAEFDll:
                    var taefTest = activeTest as TAEFTest;
                    taefTest.TestPath = TestPathBox.Text;
                    taefTest.Arguments = ArgumentsBox.Text;
                    break;
            }

            return activeTest;
        }

        private void TimeoutBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                Int32.Parse(TimeoutBox.Text);
            }
            catch (Exception)
            {
                TimeoutBox.Text = "";
            }
        }

        private void ConfigureFlyout(TestType testType)
        {
            activeTestType = testType;

            if (activeTest != null)
            {
                TestNameBox.Text = activeTest.TestName;
                TimeoutBox.Text = activeTest.TimeoutSeconds.ToString();

                switch (testType)
                {
                    case TestType.ConsoleExe:
                        var exeTest = activeTest as ExecutableTest;
                        TestPathBox.Text = exeTest.TestPath;
                        ArgumentsBox.Text = exeTest.Arguments;
                        BackgroundCheck.IsChecked = exeTest.BackgroundTask;
                        break;
                    case TestType.UWP:
                        var uwpTest = activeTest as UWPTest;
                        AppComboBox.SelectedItem = uwpTest.TestPath;
                        break;
                    case TestType.External:
                        var externalTest = activeTest as ExternalTest;
                        TestPathBox.Text = externalTest.TestPath;
                        ArgumentsBox.Text = externalTest.Arguments;
                        break;
                    case TestType.TAEFDll:
                        var taefTest = activeTest as TAEFTest;
                        TestPathBox.Text = taefTest.TestPath;
                        ArgumentsBox.Text = taefTest.Arguments;
                        break;
                }
            }
            else
            {
                BackgroundCheck.IsChecked = false;
                var boxes = FlyoutGrid.Children.Where(x => x.GetType() == typeof(TextBox));
                foreach (var item in boxes)
                {
                    var box = item as TextBox;
                    box.Text = "";
                }
            }

            switch (testType)
            {
                case TestType.ConsoleExe:
                    PathBlock.Visibility = Visibility.Visible;
                    TestPathBox.Visibility = Visibility.Visible;
                    AppComboBox.Visibility = Visibility.Collapsed;
                    AppBlock.Visibility = Visibility.Collapsed;
                    ArgumentsBlock.Visibility = Visibility.Visible;
                    ArgumentsBox.Visibility = Visibility.Visible;
                    BackgroundCheck.Visibility = Visibility.Visible;
                    break;
                case TestType.UWP:
                    PathBlock.Visibility = Visibility.Collapsed;
                    TestPathBox.Visibility = Visibility.Collapsed;
                    AppComboBox.Visibility = Visibility.Visible;
                    AppBlock.Visibility = Visibility.Visible;
                    ArgumentsBlock.Visibility = Visibility.Collapsed;
                    ArgumentsBox.Visibility = Visibility.Collapsed;
                    BackgroundCheck.Visibility = Visibility.Collapsed;
                    break;
                case TestType.External:
                    PathBlock.Visibility = Visibility.Visible;
                    TestPathBox.Visibility = Visibility.Visible;
                    AppComboBox.Visibility = Visibility.Collapsed;
                    AppBlock.Visibility = Visibility.Collapsed;
                    ArgumentsBlock.Visibility = Visibility.Visible;
                    ArgumentsBox.Visibility = Visibility.Visible;
                    BackgroundCheck.Visibility = Visibility.Collapsed;
                    break;
                case TestType.TAEFDll:
                    PathBlock.Visibility = Visibility.Visible;
                    TestPathBox.Visibility = Visibility.Visible;
                    AppComboBox.Visibility = Visibility.Collapsed;
                    AppBlock.Visibility = Visibility.Collapsed;
                    ArgumentsBlock.Visibility = Visibility.Visible;
                    ArgumentsBox.Visibility = Visibility.Visible;
                    BackgroundCheck.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        private void NewExecutableButton_Click(object sender, RoutedEventArgs e)
        {
            activeTest = null;
            ConfigureFlyout(TestType.ConsoleExe);
            EditFlyout.ShowAt(LayoutRoot, new FlyoutShowOptions() { Placement = FlyoutPlacementMode.Full });
        }

        private void NewTAEFButton_Click(object sender, RoutedEventArgs e)
        {
            activeTest = null;
            ConfigureFlyout(TestType.TAEFDll);
            EditFlyout.ShowAt(LayoutRoot, new FlyoutShowOptions() { Placement = FlyoutPlacementMode.Full });
        }

        private void NewUWPButton_Click(object sender, RoutedEventArgs e)
        {
            activeTest = null;
            ConfigureFlyout(TestType.UWP);
            EditFlyout.ShowAt(LayoutRoot, new FlyoutShowOptions() { Placement = FlyoutPlacementMode.Full });
        }

        private void NewExternalButton_Click(object sender, RoutedEventArgs e)
        {
            activeTest = null;
            ConfigureFlyout(TestType.External);
            EditFlyout.ShowAt(LayoutRoot, new FlyoutShowOptions() { Placement = FlyoutPlacementMode.Full });
        }

        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            activeTest = null;
            activeTestIndex = -1;
            EditFlyout.Hide();
        }

        private void ConfirmEdit_Click(object sender, RoutedEventArgs e)
        {
            activeTest = CreateTestFromFlyout(activeTestType);

            if (activeTest != null)
            {
                if (activeTestIndex == -1)
                {
                    TestsCollection.Add(activeTest);
                }
                else
                {
                    TestsCollection[activeTestIndex] = activeTest;
                }
            }

            listEdited = true;
            EditFlyout.Hide();
        }

        private async void SaveTestList()
        {
            activeList.Tests = new Dictionary<Guid, TestBase>();
            foreach (var test in TestsCollection)
            {
                activeList.Tests.Add(test.Guid, test);
            }

            if (isNewList)
            {
                await IPCClientHelper.IpcClient.InvokeAsync(x => x.CreateTestListFromTestList(activeList));
            }
            else
            {
                await IPCClientHelper.IpcClient.InvokeAsync(x => x.UpdateTestList(activeList));
            }
        }

        private void TestListView_DropCompleted(UIElement sender, DropCompletedEventArgs args)
        {
            listEdited = true;
        }

        private TestBase GetTestFromButton(Button button)
        {
            var stack = button.Parent as StackPanel;
            var grid = stack.Parent as Grid;
            return (TestBase)(((ContentPresenter)(grid.Children[0])).Content);
        }
        private void ListCheck_Checked(object sender, RoutedEventArgs e)
        {
            activeList.AllowOtherTestListsToRun = (bool)BlockingCheck.IsChecked;
            activeList.RunInParallel = (bool)ParallelCheck.IsChecked;
            listEdited = true;
        }

        private ObservableCollection<TestBase> TestsCollection;
        private TestList activeList;
        private TestBase activeTest;
        private TestType activeTestType;
        private int activeTestIndex;
        private bool isNewList;
        private bool listEdited;

    }
}
