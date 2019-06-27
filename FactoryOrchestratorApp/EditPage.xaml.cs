using Microsoft.FactoryOrchestrator.Core;
using Microsoft.FactoryOrchestrator.Client;
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
using TaskStatus = Microsoft.FactoryOrchestrator.Core.TaskStatus;

namespace Microsoft.FactoryOrchestrator.UWP
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
                activeList = (TaskList)e.Parameter;
            }
            else
            {
                isNewList = true;
                activeList = new TaskList(Guid.NewGuid());
            }

            AppComboBox.ItemsSource = await IPCClientHelper.IpcClient.InvokeAsync(x => x.GetInstalledApps());
            ParallelCheck.IsChecked = activeList.RunInParallel;
            BlockingCheck.IsChecked = activeList.AllowOtherTaskListsToRun;
            TaskListHeader.Text = $"Editing TaskList {activeList.Guid}";
            TestsCollection = new ObservableCollection<TaskBase>(activeList.Tasks.Values);
            TaskListView.ItemsSource = TestsCollection;

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
                        Title = "Save TaskList?",
                        Content = "Do you want to save your changes?",
                        PrimaryButtonText = "Yes",
                        CloseButtonText = "No"
                    };

                    ContentDialogResult result = await deleteFileDialog.ShowAsync();

                    if (result == ContentDialogResult.Primary)
                    {
                        SaveTaskList();
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
            ConfigureFlyout(activeTest.Type);
            EditFlyout.ShowAt(LayoutRoot, new FlyoutShowOptions() { Placement = FlyoutPlacementMode.Full,
                                                                    ShowMode = FlyoutShowMode.Standard});
        }

        private TaskBase CreateTestFromFlyout(TaskType testType)
        {
            if (activeTest == null)
            {
                activeTestIndex = -1;
                switch (testType)
                {
                    case TaskType.ConsoleExe:
                        activeTest = new ExecutableTask(TaskPathBox.Text);
                        break;
                    case TaskType.UWP:
                        activeTest = new UWPTask(AppComboBox.SelectedItem.ToString());
                        break;
                    case TaskType.External:
                        activeTest = new ExternalTask(TaskPathBox.Text);
                        break;
                    case TaskType.TAEFDll:
                        activeTest = new TAEFTest(TaskPathBox.Text);
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
                case TaskType.ConsoleExe:
                    var exeTest = activeTest as ExecutableTask;
                    exeTest.Path = TaskPathBox.Text;
                    exeTest.Arguments = ArgumentsBox.Text;
                    exeTest.BackgroundTask = (bool)BackgroundCheck.IsChecked;
                    break;
                case TaskType.UWP:
                    var uwpTest = activeTest as UWPTask;
                    uwpTest.Path = AppComboBox.SelectedItem.ToString();
                    break;
                case TaskType.External:
                    var externalTest = activeTest as ExternalTask;
                    externalTest.Path = TaskPathBox.Text;
                    externalTest.Arguments = ArgumentsBox.Text;
                    break;
                case TaskType.TAEFDll:
                    var taefTest = activeTest as TAEFTest;
                    taefTest.Path = TaskPathBox.Text;
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

        private void ConfigureFlyout(TaskType testType)
        {
            activeTestType = testType;

            if (activeTest != null)
            {
                TestNameBox.Text = activeTest.TestName;
                TimeoutBox.Text = activeTest.TimeoutSeconds.ToString();

                switch (testType)
                {
                    case TaskType.ConsoleExe:
                        var exeTest = activeTest as ExecutableTask;
                        TaskPathBox.Text = exeTest.Path;
                        ArgumentsBox.Text = exeTest.Arguments;
                        BackgroundCheck.IsChecked = exeTest.BackgroundTask;
                        break;
                    case TaskType.UWP:
                        var uwpTest = activeTest as UWPTask;
                        AppComboBox.SelectedItem = uwpTest.Path;
                        break;
                    case TaskType.External:
                        var externalTest = activeTest as ExternalTask;
                        TaskPathBox.Text = externalTest.Path;
                        ArgumentsBox.Text = externalTest.Arguments;
                        break;
                    case TaskType.TAEFDll:
                        var taefTest = activeTest as TAEFTest;
                        TaskPathBox.Text = taefTest.Path;
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
                case TaskType.ConsoleExe:
                    PathBlock.Visibility = Visibility.Visible;
                    TaskPathBox.Visibility = Visibility.Visible;
                    AppComboBox.Visibility = Visibility.Collapsed;
                    AppBlock.Visibility = Visibility.Collapsed;
                    ArgumentsBlock.Visibility = Visibility.Visible;
                    ArgumentsBox.Visibility = Visibility.Visible;
                    BackgroundCheck.Visibility = Visibility.Visible;
                    break;
                case TaskType.UWP:
                    PathBlock.Visibility = Visibility.Collapsed;
                    TaskPathBox.Visibility = Visibility.Collapsed;
                    AppComboBox.Visibility = Visibility.Visible;
                    AppBlock.Visibility = Visibility.Visible;
                    ArgumentsBlock.Visibility = Visibility.Collapsed;
                    ArgumentsBox.Visibility = Visibility.Collapsed;
                    BackgroundCheck.Visibility = Visibility.Collapsed;
                    break;
                case TaskType.External:
                    PathBlock.Visibility = Visibility.Visible;
                    TaskPathBox.Visibility = Visibility.Visible;
                    AppComboBox.Visibility = Visibility.Collapsed;
                    AppBlock.Visibility = Visibility.Collapsed;
                    ArgumentsBlock.Visibility = Visibility.Visible;
                    ArgumentsBox.Visibility = Visibility.Visible;
                    BackgroundCheck.Visibility = Visibility.Collapsed;
                    break;
                case TaskType.TAEFDll:
                    PathBlock.Visibility = Visibility.Visible;
                    TaskPathBox.Visibility = Visibility.Visible;
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
            ConfigureFlyout(TaskType.ConsoleExe);
            EditFlyout.ShowAt(LayoutRoot, new FlyoutShowOptions() { Placement = FlyoutPlacementMode.Full });
        }

        private void NewTAEFButton_Click(object sender, RoutedEventArgs e)
        {
            activeTest = null;
            ConfigureFlyout(TaskType.TAEFDll);
            EditFlyout.ShowAt(LayoutRoot, new FlyoutShowOptions() { Placement = FlyoutPlacementMode.Full });
        }

        private void NewUWPButton_Click(object sender, RoutedEventArgs e)
        {
            activeTest = null;
            ConfigureFlyout(TaskType.UWP);
            EditFlyout.ShowAt(LayoutRoot, new FlyoutShowOptions() { Placement = FlyoutPlacementMode.Full });
        }

        private void NewExternalButton_Click(object sender, RoutedEventArgs e)
        {
            activeTest = null;
            ConfigureFlyout(TaskType.External);
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

        private async void SaveTaskList()
        {
            activeList.Tasks = new Dictionary<Guid, TaskBase>();
            foreach (var task in TestsCollection)
            {
                activeList.Tasks.Add(task.Guid, task);
            }

            if (isNewList)
            {
                await IPCClientHelper.IpcClient.InvokeAsync(x => x.CreateTaskListFromTaskList(activeList));
            }
            else
            {
                await IPCClientHelper.IpcClient.InvokeAsync(x => x.UpdateTaskList(activeList));
            }
        }

        private void TaskListView_DropCompleted(UIElement sender, DropCompletedEventArgs args)
        {
            listEdited = true;
        }

        private TaskBase GetTestFromButton(Button button)
        {
            var stack = button.Parent as StackPanel;
            var grid = stack.Parent as Grid;
            return (TaskBase)(((ContentPresenter)(grid.Children[0])).Content);
        }
        private void ListCheck_Checked(object sender, RoutedEventArgs e)
        {
            activeList.AllowOtherTaskListsToRun = (bool)BlockingCheck.IsChecked;
            activeList.RunInParallel = (bool)ParallelCheck.IsChecked;
            listEdited = true;
        }

        private ObservableCollection<TaskBase> TestsCollection;
        private TaskList activeList;
        private TaskBase activeTest;
        private TaskType activeTestType;
        private int activeTestIndex;
        private bool isNewList;
        private bool listEdited;

    }
}
