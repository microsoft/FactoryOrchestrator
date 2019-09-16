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
                activeList = new TaskList("New TaskList", Guid.NewGuid());
            }

            AppComboBox.ItemsSource = await IPCClientHelper.IpcClient.InvokeAsync(x => x.GetInstalledApps());
            ParallelCheck.IsChecked = activeList.RunInParallel;
            BlockingCheck.IsChecked = activeList.AllowOtherTaskListsToRun;
            TerminateCheck.IsChecked = activeList.TerminateBackgroundTasksOnCompletion;
            UpdateHeader();

            TasksCollection = new ObservableCollection<TaskBase>(activeList.Tasks.Values);
            TaskListView.ItemsSource = TasksCollection;

            BackgroundTasksCollection = new ObservableCollection<TaskBase>(activeList.BackgroundTasks.Values);
            BgTaskListView.ItemsSource = BackgroundTasksCollection;

            if (BackgroundTasksCollection.Count > 0)
            {
                BgTasksHeader.Visibility = Visibility.Visible;
            }
            if (TasksCollection.Count > 0)
            {
                TasksHeader.Visibility = Visibility.Visible;
            }

            listEdited = false;
        }

        private void UpdateHeader()
        {
            TaskListHeader.Text = $"Editing TaskList: {activeList.Name}";
            TaskListHeader2.Text = $"({activeList.Guid.ToString()})";
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
                        SecondaryButtonText = "No",
                        CloseButtonText = "Cancel"
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

        private void BgDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            BackgroundTasksCollection.Remove(GetTestFromButton(sender as Button));
            listEdited = true;

            if (BackgroundTasksCollection.Count == 0)
            {
                BgTasksHeader.Visibility = Visibility.Collapsed;
            }
        }

        private void BgEditButton_Click(object sender, RoutedEventArgs e)
        {
            activeTask = GetTestFromButton(sender as Button);
            activeTaskIndex = BackgroundTasksCollection.IndexOf(activeTask);
            ConfigureFlyout(activeTask.Type, true);
            EditFlyout.ShowAt(LayoutRoot, new FlyoutShowOptions()
            {
                Placement = FlyoutPlacementMode.Full,
                ShowMode = FlyoutShowMode.Standard
            });
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            TasksCollection.Remove(GetTestFromButton(sender as Button));
            listEdited = true;

            if (TasksCollection.Count == 0)
            {
                TasksHeader.Visibility = Visibility.Collapsed;
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            activeTask = GetTestFromButton(sender as Button);
            activeTaskIndex = TasksCollection.IndexOf(activeTask);
            ConfigureFlyout(activeTask.Type);
            EditFlyout.ShowAt(LayoutRoot, new FlyoutShowOptions()
            {
                Placement = FlyoutPlacementMode.Full,
                ShowMode = FlyoutShowMode.Standard
            });
        }

        private TaskBase CreateTestFromFlyout(TaskType testType)
        {
            if (activeTask == null)
            {
                activeTaskIndex = -1;
                switch (testType)
                {
                    case TaskType.ConsoleExe:
                        activeTask = new ExecutableTask(TaskPathBox.Text);
                        break;
                    case TaskType.UWP:
                        activeTask = new UWPTask(AppComboBox.SelectedItem.ToString());
                        break;
                    case TaskType.External:
                        activeTask = new ExternalTask(TaskPathBox.Text);
                        break;
                    case TaskType.TAEFDll:
                        activeTask = new TAEFTest(TaskPathBox.Text);
                        break;
                    case TaskType.BatchFile:
                        activeTask = new BatchFileTask(TaskPathBox.Text);
                        break;
                    case TaskType.PowerShell:
                        activeTask = new PowerShellTask(TaskPathBox.Text);
                        break;
                }
            }

            activeTask.Name = TestNameBox.Text;

            if (TimeoutBox.Text != "")
            {
                try
                {
                    activeTask.TimeoutSeconds = Int32.Parse(TimeoutBox.Text);
                }
                catch (Exception)
                {
                    activeTask.TimeoutSeconds = -1;
                }
            }
            else
            {
                activeTask.TimeoutSeconds = -1;
            }

            if (RetryBox.Text != "")
            {
                try
                {
                    activeTask.MaxNumberOfRetries = UInt32.Parse(RetryBox.Text);
                }
                catch (Exception)
                {
                    activeTask.MaxNumberOfRetries = 0;
                }
            }
            else
            {
                activeTask.MaxNumberOfRetries = 0;
            }

            switch (testType)
            {
                case TaskType.ConsoleExe:
                case TaskType.BatchFile:
                case TaskType.PowerShell:
                case TaskType.TAEFDll:
                case TaskType.External:
                    var task = activeTask as TaskBase;
                    task.Path = TaskPathBox.Text;
                    task.Arguments = ArgumentsBox.Text;
                    break;
                case TaskType.UWP:
                    var uwpTest = activeTask as UWPTask;
                    uwpTest.Path = AppComboBox.SelectedItem.ToString();
                    break;
            }

            activeTask.AbortTaskListOnFailed = (bool)AbortOnFailBox.IsChecked;

            return activeTask;
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

        private void RetryBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                UInt32.Parse(RetryBox.Text);
            }
            catch (Exception)
            {
                RetryBox.Text = "";
            }
        }

        private void ConfigureFlyout(TaskType testType, bool isBg = false)
        {
            activeTaskType = testType;
            if (isBg)
            {
                activeTaskIsBg = true;
                TimeoutBlock.Visibility = Visibility.Collapsed;
                TimeoutBox.Visibility = Visibility.Collapsed;
                RetryBox.Visibility = Visibility.Collapsed;
                RetryBlock.Visibility = Visibility.Collapsed;
            }
            else
            {
                TimeoutBlock.Visibility = Visibility.Visible;
                TimeoutBox.Visibility = Visibility.Visible;
                RetryBox.Visibility = Visibility.Visible;
                RetryBlock.Visibility = Visibility.Visible;
            }

            if (activeTask != null)
            {
                if (isBg)
                {
                    EditFlyoutTextHeader.Text = $"Editing Background Task";
                }
                TestNameBox.Text = activeTask.Name;
                TimeoutBox.Text = activeTask.TimeoutSeconds.ToString();
                RetryBox.Text = activeTask.MaxNumberOfRetries.ToString();

                switch (testType)
                {
                    case TaskType.ConsoleExe:
                        var exeTest = activeTask as ExecutableTask;
                        TaskPathBox.Text = exeTest.Path;
                        ArgumentsBox.Text = exeTest.Arguments;
                        if (!isBg)
                        {
                            EditFlyoutTextHeader.Text = $"Editing Executable Task";
                        }
                        break;
                    case TaskType.UWP:
                        var uwpTest = activeTask as UWPTask;
                        AppComboBox.SelectedItem = uwpTest.Path;
                        EditFlyoutTextHeader.Text = $"Editing UWP Task";
                        break;
                    case TaskType.External:
                        var externalTest = activeTask as ExternalTask;
                        TaskPathBox.Text = externalTest.Path;
                        ArgumentsBox.Text = externalTest.Arguments;
                        EditFlyoutTextHeader.Text = $"Editing External Task";
                        break;
                    case TaskType.TAEFDll:
                        var taefTest = activeTask as TAEFTest;
                        TaskPathBox.Text = taefTest.Path;
                        ArgumentsBox.Text = taefTest.Arguments;
                        EditFlyoutTextHeader.Text = $"Editing TAEF Test";
                        break;
                    case TaskType.PowerShell:
                        var script = activeTask as PowerShellTask;
                        TaskPathBox.Text = script.Path;
                        ArgumentsBox.Text = script.Arguments;
                        EditFlyoutTextHeader.Text = $"Editing PowerShell Task";
                        break;
                    case TaskType.BatchFile:
                        var cmd = activeTask as BatchFileTask;
                        TaskPathBox.Text = cmd.Path;
                        ArgumentsBox.Text = cmd.Arguments;
                        EditFlyoutTextHeader.Text = $"Editing Batch File Task";
                        break;
                }
            }
            else
            {
                if (isBg)
                {
                    EditFlyoutTextHeader.Text = $"New Background Task";
                }
                else
                {
                    switch (testType)
                    {
                        case TaskType.ConsoleExe:
                            EditFlyoutTextHeader.Text = $"New Executable Task";
                            break;
                        case TaskType.UWP:
                            EditFlyoutTextHeader.Text = $"New UWP Task";
                            break;
                        case TaskType.External:
                            EditFlyoutTextHeader.Text = $"New External Task";
                            break;
                        case TaskType.TAEFDll:
                            EditFlyoutTextHeader.Text = $"New TAEF Test";
                            break;
                        case TaskType.PowerShell:
                            EditFlyoutTextHeader.Text = $"New PowerShell Task";
                            break;
                        case TaskType.BatchFile:
                            EditFlyoutTextHeader.Text = $"New Batch Task";
                            break;
                    }
                }

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
                case TaskType.External:
                case TaskType.PowerShell:
                case TaskType.BatchFile:
                    PathBlock.Visibility = Visibility.Visible;
                    TaskPathBox.Visibility = Visibility.Visible;
                    AppComboBox.Visibility = Visibility.Collapsed;
                    AppBlock.Visibility = Visibility.Collapsed;
                    ArgumentsBlock.Visibility = Visibility.Visible;
                    ArgumentsBox.Visibility = Visibility.Visible;
                    break;
                case TaskType.UWP:
                    PathBlock.Visibility = Visibility.Collapsed;
                    TaskPathBox.Visibility = Visibility.Collapsed;
                    AppComboBox.Visibility = Visibility.Visible;
                    AppBlock.Visibility = Visibility.Visible;
                    ArgumentsBlock.Visibility = Visibility.Collapsed;
                    ArgumentsBox.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        private void NewExecutableButton_Click(object sender, RoutedEventArgs e)
        {
            activeTask = null;
            ConfigureFlyout(TaskType.ConsoleExe);
            EditFlyout.ShowAt(LayoutRoot, new FlyoutShowOptions() { Placement = FlyoutPlacementMode.Full });
        }

        private void NewTAEFButton_Click(object sender, RoutedEventArgs e)
        {
            activeTask = null;
            ConfigureFlyout(TaskType.TAEFDll);
            EditFlyout.ShowAt(LayoutRoot, new FlyoutShowOptions() { Placement = FlyoutPlacementMode.Full });
        }

        private void NewUWPButton_Click(object sender, RoutedEventArgs e)
        {
            activeTask = null;
            ConfigureFlyout(TaskType.UWP);
            EditFlyout.ShowAt(LayoutRoot, new FlyoutShowOptions() { Placement = FlyoutPlacementMode.Full });
        }

        private void NewExternalButton_Click(object sender, RoutedEventArgs e)
        {
            activeTask = null;
            ConfigureFlyout(TaskType.External);
            EditFlyout.ShowAt(LayoutRoot, new FlyoutShowOptions() { Placement = FlyoutPlacementMode.Full });
        }
        private void NewBackgroundButton_Click(object sender, RoutedEventArgs e)
        {
            activeTask = null;
            ConfigureFlyout(TaskType.ConsoleExe, true);
            EditFlyout.ShowAt(LayoutRoot, new FlyoutShowOptions() { Placement = FlyoutPlacementMode.Full });
        }
        private void NewPSButton_Click(object sender, RoutedEventArgs e)
        {
            activeTask = null;
            ConfigureFlyout(TaskType.PowerShell);
            EditFlyout.ShowAt(LayoutRoot, new FlyoutShowOptions() { Placement = FlyoutPlacementMode.Full });
        }

        private void NewCMDButton_Click(object sender, RoutedEventArgs e)
        {
            activeTask = null;
            ConfigureFlyout(TaskType.BatchFile);
            EditFlyout.ShowAt(LayoutRoot, new FlyoutShowOptions() { Placement = FlyoutPlacementMode.Full });
        }

        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            activeTask = null;
            activeTaskIndex = -1;
            EditFlyout.Hide();
        }

        private void ConfirmEdit_Click(object sender, RoutedEventArgs e)
        {
            activeTask = CreateTestFromFlyout(activeTaskType);

            if (activeTask != null)
            {
                if (activeTaskIndex == -1)
                {
                    if (activeTaskIsBg)
                    {
                        BackgroundTasksCollection.Add(activeTask);
                    }
                    else
                    {
                        TasksCollection.Add(activeTask);
                    }
                }
                else
                {
                    if (activeTaskIsBg)
                    {

                        BackgroundTasksCollection[activeTaskIndex] = activeTask;
                    }
                    else
                    {
                        TasksCollection[activeTaskIndex] = activeTask;
                    }
                }
            }

            if (BackgroundTasksCollection.Count > 0)
            {
                BgTasksHeader.Visibility = Visibility.Visible;
            }
            if (TasksCollection.Count > 0)
            {
                TasksHeader.Visibility = Visibility.Visible;
            }

            listEdited = true;
            EditFlyout.Hide();
        }

        private async void SaveTaskList()
        {
            activeList.Tasks = new Dictionary<Guid, TaskBase>();
            activeList.BackgroundTasks = new Dictionary<Guid, TaskBase>();
            foreach (var task in TasksCollection)
            {
                activeList.Tasks.Add(task.Guid, task);
            }
            foreach (var task in BackgroundTasksCollection)
            {
                activeList.BackgroundTasks.Add(task.Guid, task);
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
            return ((ContentPresenter)(grid.Children.Where(x => x.GetType() == typeof(ContentPresenter)).First())).Content as TaskBase;
        }
        private void ListCheck_Checked(object sender, RoutedEventArgs e)
        {
            activeList.AllowOtherTaskListsToRun = (bool)BlockingCheck.IsChecked;
            activeList.RunInParallel = (bool)ParallelCheck.IsChecked;
            activeList.TerminateBackgroundTasksOnCompletion = (bool)TerminateCheck.IsChecked;
            listEdited = true;
        }

        private void CancelNameEdit_Click(object sender, RoutedEventArgs e)
        {
            EditListNameFlyout.Hide();
        }

        private async void ConfirmNameEdit_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(RenameBox.Text))
            {
                ContentDialog failedEdit = new ContentDialog
                {
                    Title = "Name must not be empty!",
                    Content = "The TaskList name must not be empty!",
                    CloseButtonText = "Ok"
                };
                RenameBox.Text = activeList.Name;

                ContentDialogResult result = await failedEdit.ShowAsync();
            }
            else if (!activeList.Name.Equals(RenameBox.Text, StringComparison.InvariantCulture))
            {
                activeList.Name = RenameBox.Text;
                listEdited = true;
                UpdateHeader();
                EditListNameFlyout.Hide();
            }
        }
        private void EditListNameFlyout_Opening(object sender, object e)
        {
            RenameBox.Text = activeList.Name;
        }

        private ObservableCollection<TaskBase> TasksCollection;
        private ObservableCollection<TaskBase> BackgroundTasksCollection;
        private TaskList activeList;
        private TaskBase activeTask;
        private TaskType activeTaskType;
        private int activeTaskIndex;
        private bool activeTaskIsBg;
        private bool isNewList;
        private bool listEdited;
    }
}
