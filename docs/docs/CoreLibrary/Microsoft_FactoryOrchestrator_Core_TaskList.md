### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core')
## TaskList Class
A TaskList is a grouping of Factory Orchestrator Tasks.  
```csharp
public class TaskList : Microsoft.FactoryOrchestrator.Core.NotifyPropertyChangedBase
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; [NotifyPropertyChangedBase](Microsoft_FactoryOrchestrator_Core_NotifyPropertyChangedBase.md 'Microsoft.FactoryOrchestrator.Core.NotifyPropertyChangedBase') &#129106; TaskList  

| Constructors | |
| :--- | :--- |
| [TaskList(string, Guid)](Microsoft_FactoryOrchestrator_Core_TaskList_TaskList(string_System_Guid).md 'Microsoft.FactoryOrchestrator.Core.TaskList.TaskList(string, System.Guid)') | Initializes a new instance of the [TaskList](Microsoft_FactoryOrchestrator_Core_TaskList.md 'Microsoft.FactoryOrchestrator.Core.TaskList') class. Used for editing an existing TaskList.<br/> |

| Properties | |
| :--- | :--- |
| [AllowOtherTaskListsToRun](Microsoft_FactoryOrchestrator_Core_TaskList_AllowOtherTaskListsToRun.md 'Microsoft.FactoryOrchestrator.Core.TaskList.AllowOtherTaskListsToRun') | If false, while this TaskList is running no other TaskList may run.<br/> |
| [BackgroundTasks](Microsoft_FactoryOrchestrator_Core_TaskList_BackgroundTasks.md 'Microsoft.FactoryOrchestrator.Core.TaskList.BackgroundTasks') | Background Tasks in the TaskList.<br/> |
| [Guid](Microsoft_FactoryOrchestrator_Core_TaskList_Guid.md 'Microsoft.FactoryOrchestrator.Core.TaskList.Guid') | The GUID identifying this TaskList.<br/> |
| [IsRunningOrPending](Microsoft_FactoryOrchestrator_Core_TaskList_IsRunningOrPending.md 'Microsoft.FactoryOrchestrator.Core.TaskList.IsRunningOrPending') | True if the TaskList is running or queued to run.<br/> |
| [Name](Microsoft_FactoryOrchestrator_Core_TaskList_Name.md 'Microsoft.FactoryOrchestrator.Core.TaskList.Name') | The name of this TaskList.<br/> |
| [RunInParallel](Microsoft_FactoryOrchestrator_Core_TaskList_RunInParallel.md 'Microsoft.FactoryOrchestrator.Core.TaskList.RunInParallel') | If true, Tasks in this TaskList are run in parallel. Order is non-deterministic.<br/> |
| [TaskListStatus](Microsoft_FactoryOrchestrator_Core_TaskList_TaskListStatus.md 'Microsoft.FactoryOrchestrator.Core.TaskList.TaskListStatus') | The status of the TaskList.<br/> |
| [Tasks](Microsoft_FactoryOrchestrator_Core_TaskList_Tasks.md 'Microsoft.FactoryOrchestrator.Core.TaskList.Tasks') | The Tasks in the TaskList.<br/> |
| [TerminateBackgroundTasksOnCompletion](Microsoft_FactoryOrchestrator_Core_TaskList_TerminateBackgroundTasksOnCompletion.md 'Microsoft.FactoryOrchestrator.Core.TaskList.TerminateBackgroundTasksOnCompletion') | If true, Background Tasks defined in this TaskList are forcibly terminated when the TaskList stops running.<br/> |

| Methods | |
| :--- | :--- |
| [Equals(object)](Microsoft_FactoryOrchestrator_Core_TaskList_Equals(object).md 'Microsoft.FactoryOrchestrator.Core.TaskList.Equals(object)') | Determines whether the specified [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object'), is equal to this instance.<br/> |
| [GetHashCode()](Microsoft_FactoryOrchestrator_Core_TaskList_GetHashCode().md 'Microsoft.FactoryOrchestrator.Core.TaskList.GetHashCode()') | Returns a hash code for this instance.<br/> |
| [ShouldSerializeBackgroundTasks()](Microsoft_FactoryOrchestrator_Core_TaskList_ShouldSerializeBackgroundTasks().md 'Microsoft.FactoryOrchestrator.Core.TaskList.ShouldSerializeBackgroundTasks()') | XmlSerializer calls to check if this should be serialized.<br/> |
| [ShouldSerializeTasks()](Microsoft_FactoryOrchestrator_Core_TaskList_ShouldSerializeTasks().md 'Microsoft.FactoryOrchestrator.Core.TaskList.ShouldSerializeTasks()') | XmlSerializer calls to check if this should be serialized.<br/> |
| [ShouldSerializeTerminateBackgroundTasksOnCompletion()](Microsoft_FactoryOrchestrator_Core_TaskList_ShouldSerializeTerminateBackgroundTasksOnCompletion().md 'Microsoft.FactoryOrchestrator.Core.TaskList.ShouldSerializeTerminateBackgroundTasksOnCompletion()') | XmlSerializer calls to check if this should be serialized.<br/> |
| [ToString()](Microsoft_FactoryOrchestrator_Core_TaskList_ToString().md 'Microsoft.FactoryOrchestrator.Core.TaskList.ToString()') | Converts to string.<br/> |
| [ValidateTaskList()](Microsoft_FactoryOrchestrator_Core_TaskList_ValidateTaskList().md 'Microsoft.FactoryOrchestrator.Core.TaskList.ValidateTaskList()') | Validates the TaskList is compliant with the XSD schema and other requirements.<br/>Will assign a random Guid to any Task missing one.<br/> |
