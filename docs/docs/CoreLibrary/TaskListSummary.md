### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core')
## TaskListSummary Struct
A helper class containing basic information about a TaskList. Use to quickly update clients about TaskLists and their statuses.  
```csharp
public struct TaskListSummary :
System.IEquatable<Microsoft.FactoryOrchestrator.Core.TaskListSummary>
```

Implements [System.IEquatable&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.IEquatable-1 'System.IEquatable')[TaskListSummary](TaskListSummary.md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.IEquatable-1 'System.IEquatable')  

| Constructors | |
| :--- | :--- |
| [TaskListSummary(TaskListSummary)](TaskListSummary_TaskListSummary(TaskListSummary).md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary.TaskListSummary(Microsoft.FactoryOrchestrator.Core.TaskListSummary)') | Creates a new TaskListSummary.<br/> |
| [TaskListSummary(Guid, string, TaskStatus, bool, bool, bool)](TaskListSummary_TaskListSummary(Guid_string_TaskStatus_bool_bool_bool).md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary.TaskListSummary(System.Guid, string, Microsoft.FactoryOrchestrator.Core.TaskStatus, bool, bool, bool)') | Creates a new TaskListSummary.<br/> |

| Properties | |
| :--- | :--- |
| [AllowOtherTaskListsToRun](TaskListSummary_AllowOtherTaskListsToRun.md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary.AllowOtherTaskListsToRun') | If false, while this TaskList is running no other TaskList may run.<br/> |
| [Guid](TaskListSummary_Guid.md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary.Guid') | The GUID identifying the TaskList.<br/> |
| [IsRunningOrPending](TaskListSummary_IsRunningOrPending.md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary.IsRunningOrPending') | True if the TaskList is running or queued to run.<br/> |
| [Name](TaskListSummary_Name.md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary.Name') | The name of the TaskList.<br/> |
| [RunInParallel](TaskListSummary_RunInParallel.md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary.RunInParallel') | If true, Tasks in this TaskList are run in parallel. Order is non-deterministic.<br/> |
| [Status](TaskListSummary_Status.md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary.Status') | The status of the TaskList.<br/> |
| [TerminateBackgroundTasksOnCompletion](TaskListSummary_TerminateBackgroundTasksOnCompletion.md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary.TerminateBackgroundTasksOnCompletion') | If true, Background Tasks defined in this TaskList are forcibly terminated when the TaskList stops running.<br/> |

| Methods | |
| :--- | :--- |
| [Equals(TaskListSummary)](TaskListSummary_Equals(TaskListSummary).md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary.Equals(Microsoft.FactoryOrchestrator.Core.TaskListSummary)') | Determines whether the specified [TaskListSummary](TaskListSummary.md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary'), is equal to this instance.<br/> |
| [Equals(object)](TaskListSummary_Equals(object).md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary.Equals(object)') | Determines whether the specified [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object'), is equal to this instance.<br/> |
| [GetHashCode()](TaskListSummary_GetHashCode().md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary.GetHashCode()') | Returns a hash code for this instance.<br/> |
| [ToString()](TaskListSummary_ToString().md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary.ToString()') | Converts to string.<br/> |

| Operators | |
| :--- | :--- |
| [operator ==(TaskListSummary, TaskListSummary)](TaskListSummary_operator(TaskListSummary_TaskListSummary).md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary.op_Equality(Microsoft.FactoryOrchestrator.Core.TaskListSummary, Microsoft.FactoryOrchestrator.Core.TaskListSummary)') | Implements the operator ==.<br/> |
| [operator !=(TaskListSummary, TaskListSummary)](TaskListSummary_operator!(TaskListSummary_TaskListSummary).md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary.op_Inequality(Microsoft.FactoryOrchestrator.Core.TaskListSummary, Microsoft.FactoryOrchestrator.Core.TaskListSummary)') | Implements the operator !=.<br/> |
