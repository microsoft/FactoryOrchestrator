### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core')
## TaskListSummary Struct
A helper class containing basic information about a TaskList. Use to quickly update clients about TaskLists and their statuses.  
```csharp
public struct TaskListSummary :
System.IEquatable<Microsoft.FactoryOrchestrator.Core.TaskListSummary>
```

Implements [System.IEquatable&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.IEquatable-1 'System.IEquatable')[TaskListSummary](Microsoft_FactoryOrchestrator_Core_TaskListSummary.md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.IEquatable-1 'System.IEquatable')  

| Constructors | |
| :--- | :--- |
| [TaskListSummary(TaskListSummary)](Microsoft_FactoryOrchestrator_Core_TaskListSummary_TaskListSummary(Microsoft_FactoryOrchestrator_Core_TaskListSummary).md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary.TaskListSummary(Microsoft.FactoryOrchestrator.Core.TaskListSummary)') | Creates a new TaskListSummary.<br/> |
| [TaskListSummary(Guid, string, TaskStatus, bool, bool, bool)](Microsoft_FactoryOrchestrator_Core_TaskListSummary_TaskListSummary(System_Guid_string_Microsoft_FactoryOrchestrator_Core_TaskStatus_bool_bool_bool).md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary.TaskListSummary(System.Guid, string, Microsoft.FactoryOrchestrator.Core.TaskStatus, bool, bool, bool)') | Creates a new TaskListSummary.<br/> |

| Properties | |
| :--- | :--- |
| [AllowOtherTaskListsToRun](Microsoft_FactoryOrchestrator_Core_TaskListSummary_AllowOtherTaskListsToRun.md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary.AllowOtherTaskListsToRun') | If false, while this TaskList is running no other TaskList may run.<br/> |
| [Guid](Microsoft_FactoryOrchestrator_Core_TaskListSummary_Guid.md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary.Guid') | The GUID identifying the TaskList.<br/> |
| [IsRunningOrPending](Microsoft_FactoryOrchestrator_Core_TaskListSummary_IsRunningOrPending.md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary.IsRunningOrPending') | True if the TaskList is running or queued to run.<br/> |
| [Name](Microsoft_FactoryOrchestrator_Core_TaskListSummary_Name.md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary.Name') | The name of the TaskList.<br/> |
| [RunInParallel](Microsoft_FactoryOrchestrator_Core_TaskListSummary_RunInParallel.md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary.RunInParallel') | If true, Tasks in this TaskList are run in parallel. Order is non-deterministic.<br/> |
| [Status](Microsoft_FactoryOrchestrator_Core_TaskListSummary_Status.md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary.Status') | The status of the TaskList.<br/> |
| [TerminateBackgroundTasksOnCompletion](Microsoft_FactoryOrchestrator_Core_TaskListSummary_TerminateBackgroundTasksOnCompletion.md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary.TerminateBackgroundTasksOnCompletion') | If true, Background Tasks defined in this TaskList are forcibly terminated when the TaskList stops running.<br/> |

| Methods | |
| :--- | :--- |
| [Equals(TaskListSummary)](Microsoft_FactoryOrchestrator_Core_TaskListSummary_Equals(Microsoft_FactoryOrchestrator_Core_TaskListSummary).md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary.Equals(Microsoft.FactoryOrchestrator.Core.TaskListSummary)') | Determines whether the specified [TaskListSummary](Microsoft_FactoryOrchestrator_Core_TaskListSummary.md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary'), is equal to this instance.<br/> |
| [Equals(object)](Microsoft_FactoryOrchestrator_Core_TaskListSummary_Equals(object).md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary.Equals(object)') | Determines whether the specified [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object'), is equal to this instance.<br/> |
| [GetHashCode()](Microsoft_FactoryOrchestrator_Core_TaskListSummary_GetHashCode().md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary.GetHashCode()') | Returns a hash code for this instance.<br/> |
| [ToString()](Microsoft_FactoryOrchestrator_Core_TaskListSummary_ToString().md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary.ToString()') | Converts to string.<br/> |

| Operators | |
| :--- | :--- |
| [operator ==(TaskListSummary, TaskListSummary)](Microsoft_FactoryOrchestrator_Core_TaskListSummary_op_Equality(Microsoft_FactoryOrchestrator_Core_TaskListSummary_Microsoft_FactoryOrchestrator_Core_TaskListSummary).md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary.op_Equality(Microsoft.FactoryOrchestrator.Core.TaskListSummary, Microsoft.FactoryOrchestrator.Core.TaskListSummary)') | Implements the operator ==.<br/> |
| [operator !=(TaskListSummary, TaskListSummary)](Microsoft_FactoryOrchestrator_Core_TaskListSummary_op_Inequality(Microsoft_FactoryOrchestrator_Core_TaskListSummary_Microsoft_FactoryOrchestrator_Core_TaskListSummary).md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary.op_Inequality(Microsoft.FactoryOrchestrator.Core.TaskListSummary, Microsoft.FactoryOrchestrator.Core.TaskListSummary)') | Implements the operator !=.<br/> |
