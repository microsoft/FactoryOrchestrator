// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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
using Windows.ApplicationModel.Resources;
using System.Globalization;
using Windows.UI.Xaml.Automation;

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

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            BackButton.IsEnabled = this.Frame.CanGoBack;

            if (e != null && e.Parameter != null)
            {
                isNewList = false;
                activeList = (TaskList)e.Parameter;
            }
            else
            {
                isNewList = true;
                var guid = Guid.NewGuid();
                activeList = new TaskList(guid.ToString(), guid);
            }

            try
            {
                var items = await Client.GetInstalledApps();
                items.Sort();
                AppComboBox.ItemsSource = items;
            }
            catch (Exception)
            {
                // WDP might not be running, just dont put any apps in the list
                AppComboBox.ItemsSource = new List<string>();
            }

            try
            {
                _ = await Client.GetOSPlatform();
                supportsCommandLineTask = true;
            }
            catch (FactoryOrchestratorVersionMismatchException)
            {
                // Service version < 9.1.0, must use BatchFileTask
                supportsCommandLineTask = false;
            }

            ParallelCheck.IsChecked = activeList.RunInParallel;
            BlockingCheck.IsChecked = activeList.AllowOtherTaskListsToRun;
            TerminateBgTasksCheck.IsChecked = activeList.TerminateBackgroundTasksOnCompletion;
            UpdateHeader();

            TasksCollection = new ObservableCollection<TaskBase>(activeList.Tasks);
            TaskListView.ItemsSource = TasksCollection;

            BackgroundTasksCollection = new ObservableCollection<TaskBase>(activeList.BackgroundTasks);
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


            var style = new Style(typeof(FlyoutPresenter));
            style.Setters.Add(new Setter(FlyoutPresenter.MinWidthProperty, Window.Current.CoreWindow.Bounds.Width));
            style.Setters.Add(new Setter(FlyoutPresenter.MinHeightProperty, Window.Current.CoreWindow.Bounds.Height));
            EditFlyout.SetValue(Flyout.FlyoutPresenterStyleProperty, style);
        }

        private void UpdateHeader()
        {
            TaskListHeader.Text = resourceLoader.GetString("EditingTaskList") + $": {activeList.Name}";
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
                        
                        Title = resourceLoader.GetString("SaveTaskListTitle"),
                        Content = resourceLoader.GetString("SaveTaskListContent"),
                        PrimaryButtonText = resourceLoader.GetString("Yes"),
                        SecondaryButtonText = resourceLoader.GetString("No"),
                        CloseButtonText = resourceLoader.GetString("Cancel")
                    };

                    ContentDialogResult result = await deleteFileDialog.ShowAsync();

                    if (result == ContentDialogResult.Primary)
                    {
                        if (await SaveTaskList())
                        {
                            this.Frame.GoBack();
                        }
                    }                    
                    else if (result == ContentDialogResult.Secondary)
                    {
                        this.Frame.GoBack();
                    }
                }
                else
                {
                    this.Frame.GoBack();
                }

                return true;

            }
            return false;
        }

        private void BgDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            BackgroundTasksCollection.Remove(GetTaskFromButton(sender as Button));
            listEdited = true;

            if (BackgroundTasksCollection.Count == 0)
            {
                BgTasksHeader.Visibility = Visibility.Collapsed;
            }
        }

        private void BgEditButton_Click(object sender, RoutedEventArgs e)
        {
            activeTask = GetTaskFromButton(sender as Button);
            activeTaskIndex = BackgroundTasksCollection.IndexOf(activeTask);
            ConfigureFlyout(activeTask.Type, true);
            EditFlyout.ShowAt(LayoutRoot, new FlyoutShowOptions()
            {
                Placement = FlyoutPlacementMode.Auto,
                ShowMode = FlyoutShowMode.Standard
            });
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            TasksCollection.Remove(GetTaskFromButton(sender as Button));
            listEdited = true;

            if (TasksCollection.Count == 0)
            {
                TasksHeader.Visibility = Visibility.Collapsed;
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            activeTask = GetTaskFromButton(sender as Button);
            activeTaskIndex = TasksCollection.IndexOf(activeTask);
            ConfigureFlyout(activeTask.Type);
            EditFlyout.ShowAt(LayoutRoot, new FlyoutShowOptions()
            {
                Placement = FlyoutPlacementMode.Auto,
                ShowMode = FlyoutShowMode.Standard
            });
        }

        private TaskBase CreateTestFromFlyout(TaskType testType)
        {
            activeTaskIsNowBg = false;
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
                        if (supportsCommandLineTask)
                        {
                            activeTask = new CommandLineTask(TaskPathBox.Text);
                        }
                        else
                        {
#pragma warning disable CS0618 // Type or member is obsolete
                            activeTask = new BatchFileTask(TaskPathBox.Text);
#pragma warning restore CS0618 // Type or member is obsolete
                        }
                        break;
                    case TaskType.PowerShell:
                        activeTask = new PowerShellTask(TaskPathBox.Text);
                        break;
                }
            }

            if (!String.IsNullOrWhiteSpace(TestNameBox.Text))
            {
                activeTask.Name = TestNameBox.Text;
            }

            if (!string.IsNullOrWhiteSpace(TimeoutBox.Text))
            {
                try
                {
                    activeTask.TimeoutSeconds = Int32.Parse(TimeoutBox.Text, CultureInfo.CurrentCulture);
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

            if (!string.IsNullOrWhiteSpace(RetryBox.Text))
            {
                try
                {
                    activeTask.MaxNumberOfRetries = UInt32.Parse(RetryBox.Text, CultureInfo.CurrentCulture);
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
                    var uwpTask = activeTask as UWPTask;
                    uwpTask.Path = AppComboBox.SelectedItem.ToString();
                    uwpTask.Arguments = ArgumentsBox.Text;
                    uwpTask.AutoPassedIfLaunched = (bool)AutoPassCheck.IsChecked;
                    uwpTask.TerminateOnCompleted = (bool)TerminateOnCompleteCheck.IsChecked;
                    break;
            }

            switch (testType)
            {
                case TaskType.ConsoleExe:
                case TaskType.BatchFile:
                case TaskType.PowerShell:
                    var task = activeTask as ExecutableTask;
                    task.BackgroundTask = (bool)BgTaskBox.IsChecked;
                    if (task.BackgroundTask)
                    {
                        activeTaskIsNowBg = true;
                    }
                    break;
                default:
                    break;
            }

            activeTask.AbortTaskListOnFailed = (bool)AbortOnFailBox.IsChecked;
            activeTask.RunInContainer = (bool)ContainerBox.IsChecked;

            return activeTask;
        }

        private void TimeoutBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                Int32.Parse(TimeoutBox.Text, CultureInfo.CurrentCulture);
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
                UInt32.Parse(RetryBox.Text, CultureInfo.CurrentCulture);
            }
            catch (Exception)
            {
                RetryBox.Text = "";
            }
        }

        private void BgTaskBox_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)BgTaskBox.IsChecked)
            {
                TimeoutBox.IsEnabled = false;
                RetryBox.IsEnabled = false;
                AbortOnFailBox.IsEnabled = false;
                TimeoutBox.Text = "-1";
                RetryBox.Text = "0";
            }
            else
            {
                TimeoutBox.IsEnabled = true;
                RetryBox.IsEnabled = true;
                AbortOnFailBox.IsEnabled = true;
            }
        }

        private void AutoPassCheck_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)AutoPassCheck.IsChecked)
            {
                TerminateOnCompleteCheck.IsEnabled = false;
            }
            else
            {
                TerminateOnCompleteCheck.IsEnabled = true;
            }
        }

        private async void ConfigureFlyout(TaskType testType, bool editingBgTask = false)
        {
            activeTaskType = testType;
            activeTaskWasBg = editingBgTask;

            if (activeTask != null)
            {
                TestNameBox.Text = activeTask.Name;
                TimeoutBox.Text = activeTask.TimeoutSeconds.ToString(CultureInfo.CurrentCulture);
                RetryBox.Text = activeTask.MaxNumberOfRetries.ToString(CultureInfo.CurrentCulture);
                ContainerBox.IsChecked = activeTask.RunInContainer;
                AbortOnFailBox.IsChecked = activeTask.AbortTaskListOnFailed;

                switch (testType)
                {
                    case TaskType.ConsoleExe:
                        var exeTest = activeTask as ExecutableTask;
                        TaskPathBox.Text = exeTest.Path;
                        ArgumentsBox.Text = exeTest.Arguments;
                        EditFlyoutTextHeader.Text = resourceLoader.GetString("EditFlyoutTextHeaderEditingExe");
                        resourceLoader.GetString("Path");
                        BgTaskBox.IsChecked = exeTest.BackgroundTask;
                        break;
                    case TaskType.UWP:
                        var uwpTask = activeTask as UWPTask;
                        AppComboBox.SelectedItem = uwpTask.Path;

                        if (AppComboBox.SelectedIndex == -1)
                        {
                            List<string> itemlist;
                            try
                            {
                                itemlist = await Client.GetInstalledApps();
                            }
                            catch (Exception)
                            {
                                // WDP might not be running, just dont put any apps in the list
                                itemlist = new List<string>();
                            }

                            if (!itemlist.Contains(uwpTask.Path))
                            {
                                itemlist.Add(uwpTask.Path);
                            }

                            itemlist.Sort();
                            AppComboBox.ItemsSource = itemlist;
                            AppComboBox.SelectedItem = uwpTask.Path;
                        }
                        AutoPassCheck.IsChecked = uwpTask.AutoPassedIfLaunched;
                        TerminateOnCompleteCheck.IsChecked = uwpTask.TerminateOnCompleted;
                        // Disable Terminate box if needed
                        AutoPassCheck_Click(null, null);

                        EditFlyoutTextHeader.Text = resourceLoader.GetString("EditFlyoutTextHeaderEditingUWP");
                        resourceLoader.GetString("AUMID");
                        break;
                    case TaskType.External:
                        var externalTest = activeTask as ExternalTask;
                        TaskPathBox.Text = externalTest.Path;
                        ArgumentsBox.Text = externalTest.Arguments;
                        PathBlock.Text = resourceLoader.GetString("EditFlyoutImagePath");
                        EditFlyoutTextHeader.Text = resourceLoader.GetString("EditFlyoutTextHeaderEditingExternal");
                        break;
                    case TaskType.TAEFDll:
                        var taefTest = activeTask as TAEFTest;
                        TaskPathBox.Text = taefTest.Path;
                        ArgumentsBox.Text = taefTest.Arguments;
                        EditFlyoutTextHeader.Text = resourceLoader.GetString("EditFlyoutTextHeaderEditingTAEF");
                        resourceLoader.GetString("Path");
                        break;
                    case TaskType.PowerShell:
                        var script = activeTask as PowerShellTask;
                        TaskPathBox.Text = script.Path;
                        ArgumentsBox.Text = script.Arguments;
                        EditFlyoutTextHeader.Text = resourceLoader.GetString("EditFlyoutTextHeaderEditingPS");
                        BgTaskBox.IsChecked = script.BackgroundTask;
                        resourceLoader.GetString("Path");
                        break;
                    case TaskType.BatchFile:
                        var cmd = activeTask as CommandLineTask;
                        TaskPathBox.Text = cmd.Path;
                        ArgumentsBox.Text = cmd.Arguments;
                        EditFlyoutTextHeader.Text = resourceLoader.GetString("EditFlyoutTextHeaderEditingCMD");
                        BgTaskBox.IsChecked = cmd.BackgroundTask;
                        resourceLoader.GetString("Path");
                        break;
                }
            }
            else
            {
                switch (testType)
                {
                    case TaskType.ConsoleExe:
                        EditFlyoutTextHeader.Text = resourceLoader.GetString("NewExecutableButton/Content");
                        resourceLoader.GetString("Path");
                        break;
                    case TaskType.UWP:
                        EditFlyoutTextHeader.Text = resourceLoader.GetString("NewUWPButton/Content");
                        AutoPassCheck.IsChecked = false;
                        TerminateOnCompleteCheck.IsChecked = true;
                        // Enable Terminate box if needed
                        AutoPassCheck_Click(null, null);
                        resourceLoader.GetString("AUMID");
                        break;
                    case TaskType.External:
                        EditFlyoutTextHeader.Text = resourceLoader.GetString("NewExternalButton/Content");
                        PathBlock.Text = resourceLoader.GetString("EditFlyoutImagePath");
                        break;
                    case TaskType.TAEFDll:
                        EditFlyoutTextHeader.Text = resourceLoader.GetString("NewTAEFButton/Content");
                        resourceLoader.GetString("Path");
                        break;
                    case TaskType.PowerShell:
                        EditFlyoutTextHeader.Text = resourceLoader.GetString("NewPSButton/Content");
                        resourceLoader.GetString("Path");
                        break;
                    case TaskType.BatchFile:
                        EditFlyoutTextHeader.Text = resourceLoader.GetString("NewCMDButton/Content");
                        resourceLoader.GetString("Path");
                        break;
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
                case TaskType.PowerShell:
                case TaskType.BatchFile:
                    PathBlock.Visibility = Visibility.Visible;
                    TaskPathBox.Visibility = Visibility.Visible;
                    AppComboBox.Visibility = Visibility.Collapsed;
                    AppBlock.Visibility = Visibility.Collapsed;
                    resourceLoader.GetString("ArgumentsBlock/Text");
                    ArgumentsBlock.Visibility = Visibility.Visible;
                    ArgumentsBox.Visibility = Visibility.Visible;
                    BgTaskBox.Visibility = Visibility.Visible;
                    AutoPassCheck.Visibility = Visibility.Collapsed;
                    TerminateOnCompleteCheck.Visibility = Visibility.Collapsed;
                    break;
                case TaskType.External:
                case TaskType.TAEFDll:
                    PathBlock.Visibility = Visibility.Visible;
                    TaskPathBox.Visibility = Visibility.Visible;
                    AppComboBox.Visibility = Visibility.Collapsed;
                    AppBlock.Visibility = Visibility.Collapsed;
                    resourceLoader.GetString("ArgumentsBlock/Text");
                    ArgumentsBlock.Visibility = Visibility.Visible;
                    ArgumentsBox.Visibility = Visibility.Visible;
                    BgTaskBox.Visibility = Visibility.Collapsed;
                    AutoPassCheck.Visibility = Visibility.Collapsed;
                    TerminateOnCompleteCheck.Visibility = Visibility.Collapsed;
                    break;
                case TaskType.UWP:
                    PathBlock.Visibility = Visibility.Collapsed;
                    TaskPathBox.Visibility = Visibility.Collapsed;
                    AppComboBox.Visibility = Visibility.Visible;
                    AppBlock.Visibility = Visibility.Visible;
                    resourceLoader.GetString("UWPArguments");
                    ArgumentsBlock.Visibility = Visibility.Visible;
                    ArgumentsBox.Visibility = Visibility.Visible;
                    BgTaskBox.Visibility = Visibility.Collapsed;
                    AutoPassCheck.Visibility = Visibility.Visible;
                    TerminateOnCompleteCheck.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void NewExecutableButton_Click(object sender, RoutedEventArgs e)
        {
            activeTask = null;
            ConfigureFlyout(TaskType.ConsoleExe);
            EditFlyout.ShowAt(LayoutRoot, new FlyoutShowOptions() { Placement = FlyoutPlacementMode.Auto });
        }

        private void NewTAEFButton_Click(object sender, RoutedEventArgs e)
        {
            activeTask = null;
            ConfigureFlyout(TaskType.TAEFDll);
            EditFlyout.ShowAt(LayoutRoot, new FlyoutShowOptions() { Placement = FlyoutPlacementMode.Auto });
        }

        private void NewUWPButton_Click(object sender, RoutedEventArgs e)
        {
            activeTask = null;
            ConfigureFlyout(TaskType.UWP);
            EditFlyout.ShowAt(LayoutRoot, new FlyoutShowOptions() { Placement = FlyoutPlacementMode.Auto });
        }

        private void NewExternalButton_Click(object sender, RoutedEventArgs e)
        {
            activeTask = null;
            ConfigureFlyout(TaskType.External);
            EditFlyout.ShowAt(LayoutRoot, new FlyoutShowOptions() { Placement = FlyoutPlacementMode.Auto });
        }
        private void NewPSButton_Click(object sender, RoutedEventArgs e)
        {
            activeTask = null;
            ConfigureFlyout(TaskType.PowerShell);
            EditFlyout.ShowAt(LayoutRoot, new FlyoutShowOptions() { Placement = FlyoutPlacementMode.Auto });
        }

        private void NewCMDButton_Click(object sender, RoutedEventArgs e)
        {
            activeTask = null;
            ConfigureFlyout(TaskType.BatchFile);
            EditFlyout.ShowAt(LayoutRoot, new FlyoutShowOptions() { Placement = FlyoutPlacementMode.Auto });
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
                if (activeTaskIsNowBg)
                {
                    if (activeTaskWasBg && activeTaskIndex >= 0)
                    {
                        BackgroundTasksCollection[activeTaskIndex] = activeTask;
                    }
                    else if (!activeTaskWasBg && activeTaskIndex >= 0)
                    {
                        BackgroundTasksCollection.Add(activeTask);
                        TasksCollection.RemoveAt(activeTaskIndex);
                    }
                    else
                    {
                        BackgroundTasksCollection.Add(activeTask);
                    }
                }
                else
                {
                    if (activeTaskWasBg && activeTaskIndex >= 0)
                    {
                        TasksCollection.Add(activeTask);
                        BackgroundTasksCollection.RemoveAt(activeTaskIndex);
                    }
                    else if (!activeTaskWasBg && activeTaskIndex >= 0)
                    {
                        TasksCollection[activeTaskIndex] = activeTask;
                    }
                    else
                    {
                        TasksCollection.Add(activeTask);
                    }
                }

                listEdited = true;
            }

            if (BackgroundTasksCollection.Count > 0)
            {
                BgTasksHeader.Visibility = Visibility.Visible;
            }
            else
            {
                BgTasksHeader.Visibility = Visibility.Collapsed;
            }
            if (TasksCollection.Count > 0)
            {
                TasksHeader.Visibility = Visibility.Visible;
            }
            else
            {
                TasksHeader.Visibility = Visibility.Collapsed;
            }

            EditFlyout.Hide();
        }

        private async Task<bool> SaveTaskList()
        {
            activeList.Tasks = new List<TaskBase>();
            activeList.BackgroundTasks = new List<TaskBase>();
            foreach (var task in TasksCollection)
            {
                activeList.Tasks.Add(task);
            }
            foreach (var task in BackgroundTasksCollection)
            {
                activeList.BackgroundTasks.Add(task);
            }

            try
            {
                if (isNewList)
                {
                    await Client.CreateTaskListFromTaskList(activeList);
                }
                else
                {
                    await Client.UpdateTaskList(activeList);
                }

                return true;
            }
            catch (Exception ex)
            {
                if (ex.GetType() != typeof(FactoryOrchestratorConnectionException))
                {
                    ContentDialog failedSaveDialog = new ContentDialog
                    {
                        Title = resourceLoader.GetString("TaskListSaveFailed"),
                        Content = ex.Message,
                        CloseButtonText = resourceLoader.GetString("Ok")
                    };

                    ContentDialogResult result = await failedSaveDialog.ShowAsync();
                }
            }

            return false;
        }

        private void TaskListView_DragCompleted(UIElement sender, DragItemsCompletedEventArgs args)
        {
            listEdited = true;
        }

        private static TaskBase GetTaskFromButton(Button button)
        {
            var stack = button.Parent as StackPanel;
            var grid = stack.Parent as Grid;
            return ((ContentPresenter)(grid.Children.Where(x => x.GetType() == typeof(ContentPresenter)).First())).Content as TaskBase;
        }

        private void ListCheck_Checked(object sender, RoutedEventArgs e)
        {
            activeList.AllowOtherTaskListsToRun = (bool)BlockingCheck.IsChecked;
            activeList.RunInParallel = (bool)ParallelCheck.IsChecked;
            activeList.TerminateBackgroundTasksOnCompletion = (bool)TerminateBgTasksCheck.IsChecked;
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
                    Title = resourceLoader.GetString("TaskListInvalidNameTitle"),
                    Content = resourceLoader.GetString("TaskListInvalidNameContent"),
                    CloseButtonText = resourceLoader.GetString("Ok")
                };
                RenameBox.Text = activeList.Name;
                RenameBox.SetValue(AutomationProperties.NameProperty, activeList.Name);
                _ = await failedEdit.ShowAsync();
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
            RenameBox.SetValue(AutomationProperties.NameProperty, activeList.Name);
        }

        private ObservableCollection<TaskBase> TasksCollection;
        private ObservableCollection<TaskBase> BackgroundTasksCollection;
        private TaskList activeList;
        private TaskBase activeTask;
        private TaskType activeTaskType;
        private int activeTaskIndex;
        private bool activeTaskWasBg;
        private bool activeTaskIsNowBg;
        private bool isNewList;
        private bool listEdited;
        private bool supportsCommandLineTask;
        private readonly FactoryOrchestratorUWPClient Client = ((App)Application.Current).Client;
        private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();
    }
}
