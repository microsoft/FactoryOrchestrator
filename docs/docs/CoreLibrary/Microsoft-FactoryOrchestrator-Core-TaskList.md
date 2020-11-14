#### [FactoryOrchestratorCoreLibrary](./FactoryOrchestratorCoreLibrary.md 'FactoryOrchestratorCoreLibrary')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
## TaskList Class
A TaskList is a grouping of FTF tests. TaskLists are the only object FTF can "Run".  
```csharp
public class TaskList : NotifyPropertyChangedBase
```
Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; [NotifyPropertyChangedBase](./Microsoft-FactoryOrchestrator-Core-NotifyPropertyChangedBase.md 'Microsoft.FactoryOrchestrator.Core.NotifyPropertyChangedBase') &#129106; TaskList  
### Constructors
- [TaskList(string, System.Guid)](./Microsoft-FactoryOrchestrator-Core-TaskList-TaskList(string_System-Guid).md 'Microsoft.FactoryOrchestrator.Core.TaskList.TaskList(string, System.Guid)')
### Properties
- [AllowOtherTaskListsToRun](./Microsoft-FactoryOrchestrator-Core-TaskList-AllowOtherTaskListsToRun.md 'Microsoft.FactoryOrchestrator.Core.TaskList.AllowOtherTaskListsToRun')
- [BackgroundTasks](./Microsoft-FactoryOrchestrator-Core-TaskList-BackgroundTasks.md 'Microsoft.FactoryOrchestrator.Core.TaskList.BackgroundTasks')
- [Guid](./Microsoft-FactoryOrchestrator-Core-TaskList-Guid.md 'Microsoft.FactoryOrchestrator.Core.TaskList.Guid')
- [IsRunningOrPending](./Microsoft-FactoryOrchestrator-Core-TaskList-IsRunningOrPending.md 'Microsoft.FactoryOrchestrator.Core.TaskList.IsRunningOrPending')
- [Name](./Microsoft-FactoryOrchestrator-Core-TaskList-Name.md 'Microsoft.FactoryOrchestrator.Core.TaskList.Name')
- [RunInParallel](./Microsoft-FactoryOrchestrator-Core-TaskList-RunInParallel.md 'Microsoft.FactoryOrchestrator.Core.TaskList.RunInParallel')
- [TaskListStatus](./Microsoft-FactoryOrchestrator-Core-TaskList-TaskListStatus.md 'Microsoft.FactoryOrchestrator.Core.TaskList.TaskListStatus')
- [Tasks](./Microsoft-FactoryOrchestrator-Core-TaskList-Tasks.md 'Microsoft.FactoryOrchestrator.Core.TaskList.Tasks')
- [TerminateBackgroundTasksOnCompletion](./Microsoft-FactoryOrchestrator-Core-TaskList-TerminateBackgroundTasksOnCompletion.md 'Microsoft.FactoryOrchestrator.Core.TaskList.TerminateBackgroundTasksOnCompletion')
### Methods
- [Equals(object)](./Microsoft-FactoryOrchestrator-Core-TaskList-Equals(object).md 'Microsoft.FactoryOrchestrator.Core.TaskList.Equals(object)')
- [GetHashCode()](./Microsoft-FactoryOrchestrator-Core-TaskList-GetHashCode().md 'Microsoft.FactoryOrchestrator.Core.TaskList.GetHashCode()')
- [ShouldSerializeBackgroundTasks()](./Microsoft-FactoryOrchestrator-Core-TaskList-ShouldSerializeBackgroundTasks().md 'Microsoft.FactoryOrchestrator.Core.TaskList.ShouldSerializeBackgroundTasks()')
- [ShouldSerializeTasks()](./Microsoft-FactoryOrchestrator-Core-TaskList-ShouldSerializeTasks().md 'Microsoft.FactoryOrchestrator.Core.TaskList.ShouldSerializeTasks()')
- [ShouldSerializeTerminateBackgroundTasksOnCompletion()](./Microsoft-FactoryOrchestrator-Core-TaskList-ShouldSerializeTerminateBackgroundTasksOnCompletion().md 'Microsoft.FactoryOrchestrator.Core.TaskList.ShouldSerializeTerminateBackgroundTasksOnCompletion()')
- [ToString()](./Microsoft-FactoryOrchestrator-Core-TaskList-ToString().md 'Microsoft.FactoryOrchestrator.Core.TaskList.ToString()')
- [ValidateTaskList()](./Microsoft-FactoryOrchestrator-Core-TaskList-ValidateTaskList().md 'Microsoft.FactoryOrchestrator.Core.TaskList.ValidateTaskList()')
