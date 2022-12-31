### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core')
## TaskList Class
A TaskList is a grouping of Factory Orchestrator Tasks.  
```csharp
public class TaskList : Microsoft.FactoryOrchestrator.Core.NotifyPropertyChangedBase
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; [NotifyPropertyChangedBase](NotifyPropertyChangedBase.md 'Microsoft.FactoryOrchestrator.Core.NotifyPropertyChangedBase') &#129106; TaskList  

| Constructors | |
| :--- | :--- |
| [TaskList(string, Guid)](TaskList_TaskList(string_Guid).md 'Microsoft.FactoryOrchestrator.Core.TaskList.TaskList(string, System.Guid)') | Initializes a new instance of the [TaskList](TaskList.md 'Microsoft.FactoryOrchestrator.Core.TaskList') class. Used for editing an existing TaskList.<br/> |

| Properties | |
| :--- | :--- |
| [AllowOtherTaskListsToRun](TaskList_AllowOtherTaskListsToRun.md 'Microsoft.FactoryOrchestrator.Core.TaskList.AllowOtherTaskListsToRun') | If false, while this TaskList is running no other TaskList may run.<br/> |
| [BackgroundTasks](TaskList_BackgroundTasks.md 'Microsoft.FactoryOrchestrator.Core.TaskList.BackgroundTasks') | Background Tasks in the TaskList.<br/> |
| [Guid](TaskList_Guid.md 'Microsoft.FactoryOrchestrator.Core.TaskList.Guid') | The GUID identifying this TaskList.<br/> |
| [IsRunningOrPending](TaskList_IsRunningOrPending.md 'Microsoft.FactoryOrchestrator.Core.TaskList.IsRunningOrPending') | True if the TaskList is running or queued to run.<br/> |
| [Name](TaskList_Name.md 'Microsoft.FactoryOrchestrator.Core.TaskList.Name') | The name of this TaskList.<br/> |
| [RunInParallel](TaskList_RunInParallel.md 'Microsoft.FactoryOrchestrator.Core.TaskList.RunInParallel') | If true, Tasks in this TaskList are run in parallel. Order is non-deterministic.<br/> |
| [TaskListStatus](TaskList_TaskListStatus.md 'Microsoft.FactoryOrchestrator.Core.TaskList.TaskListStatus') | The status of the TaskList.<br/> |
| [Tasks](TaskList_Tasks.md 'Microsoft.FactoryOrchestrator.Core.TaskList.Tasks') | The Tasks in the TaskList.<br/> |
| [TerminateBackgroundTasksOnCompletion](TaskList_TerminateBackgroundTasksOnCompletion.md 'Microsoft.FactoryOrchestrator.Core.TaskList.TerminateBackgroundTasksOnCompletion') | If true, Background Tasks defined in this TaskList are forcibly terminated when the TaskList stops running.<br/> |

| Methods | |
| :--- | :--- |
| [Equals(object)](TaskList_Equals(object).md 'Microsoft.FactoryOrchestrator.Core.TaskList.Equals(object)') | Determines whether the specified [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object'), is equal to this instance.<br/> |
| [GetHashCode()](TaskList_GetHashCode().md 'Microsoft.FactoryOrchestrator.Core.TaskList.GetHashCode()') | Returns a hash code for this instance.<br/> |
| [ShouldSerializeBackgroundTasks()](TaskList_ShouldSerializeBackgroundTasks().md 'Microsoft.FactoryOrchestrator.Core.TaskList.ShouldSerializeBackgroundTasks()') | XmlSerializer calls to check if this should be serialized.<br/> |
| [ShouldSerializeTasks()](TaskList_ShouldSerializeTasks().md 'Microsoft.FactoryOrchestrator.Core.TaskList.ShouldSerializeTasks()') | XmlSerializer calls to check if this should be serialized.<br/> |
| [ShouldSerializeTerminateBackgroundTasksOnCompletion()](TaskList_ShouldSerializeTerminateBackgroundTasksOnCompletion().md 'Microsoft.FactoryOrchestrator.Core.TaskList.ShouldSerializeTerminateBackgroundTasksOnCompletion()') | XmlSerializer calls to check if this should be serialized.<br/> |
| [ToString()](TaskList_ToString().md 'Microsoft.FactoryOrchestrator.Core.TaskList.ToString()') | Converts to string.<br/> |
| [ValidateTaskList()](TaskList_ValidateTaskList().md 'Microsoft.FactoryOrchestrator.Core.TaskList.ValidateTaskList()') | Validates the TaskList is compliant with the XSD schema and other requirements.<br/>Will assign a random Guid to any Task missing one.<br/> |
