### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core')
## TaskRun Class
A TaskRun represents one instance of executing any single Task.  
```csharp
public class TaskRun : Microsoft.FactoryOrchestrator.Core.NotifyPropertyChangedBase
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; [NotifyPropertyChangedBase](NotifyPropertyChangedBase.md 'Microsoft.FactoryOrchestrator.Core.NotifyPropertyChangedBase') &#129106; TaskRun  

| Constructors | |
| :--- | :--- |
| [TaskRun(TaskBase)](TaskRun_TaskRun(TaskBase).md 'Microsoft.FactoryOrchestrator.Core.TaskRun.TaskRun(Microsoft.FactoryOrchestrator.Core.TaskBase)') | task Run shared constructor. <br/> |

| Properties | |
| :--- | :--- |
| [Arguments](TaskRun_Arguments.md 'Microsoft.FactoryOrchestrator.Core.TaskRun.Arguments') | The arguments of the Task (at the time the run started).<br/> |
| [BackgroundTask](TaskRun_BackgroundTask.md 'Microsoft.FactoryOrchestrator.Core.TaskRun.BackgroundTask') | Denotes if this TaskRun is for a background task.<br/> |
| [ExitCode](TaskRun_ExitCode.md 'Microsoft.FactoryOrchestrator.Core.TaskRun.ExitCode') | The exit code of this run. NULL if it has not finished.<br/> |
| [Guid](TaskRun_Guid.md 'Microsoft.FactoryOrchestrator.Core.TaskRun.Guid') | The GUID identifying this TaskRun.<br/> |
| [LogFilePath](TaskRun_LogFilePath.md 'Microsoft.FactoryOrchestrator.Core.TaskRun.LogFilePath') | The path to the log file for this run. NULL if it is not logged to a file.<br/> |
| [OwningTaskGuid](TaskRun_OwningTaskGuid.md 'Microsoft.FactoryOrchestrator.Core.TaskRun.OwningTaskGuid') | The GUID of the Task which created this run. NULL if this run is not associated with a Task.<br/> |
| [RunByClient](TaskRun_RunByClient.md 'Microsoft.FactoryOrchestrator.Core.TaskRun.RunByClient') | True if this TaskRun is run by the client, such as an ExternalTask.<br/> |
| [RunByServer](TaskRun_RunByServer.md 'Microsoft.FactoryOrchestrator.Core.TaskRun.RunByServer') | True if this TaskRun is run by the server, such as an ExecutableTask.<br/> |
| [RunInContainer](TaskRun_RunInContainer.md 'Microsoft.FactoryOrchestrator.Core.TaskRun.RunInContainer') | If true, the TaskRun is executed inside the Win32 container.<br/> |
| [RunTime](TaskRun_RunTime.md 'Microsoft.FactoryOrchestrator.Core.TaskRun.RunTime') | The amount of time this run executed for. NULL if it has never started.<br/> |
| [TaskName](TaskRun_TaskName.md 'Microsoft.FactoryOrchestrator.Core.TaskRun.TaskName') | The name of the Task (at the time the run started).<br/> |
| [TaskOutput](TaskRun_TaskOutput.md 'Microsoft.FactoryOrchestrator.Core.TaskRun.TaskOutput') | The output of the Task.<br/> |
| [TaskPath](TaskRun_TaskPath.md 'Microsoft.FactoryOrchestrator.Core.TaskRun.TaskPath') | The path of the Task (at the time the run started).<br/> |
| [TaskRunComplete](TaskRun_TaskRunComplete.md 'Microsoft.FactoryOrchestrator.Core.TaskRun.TaskRunComplete') | True if this run is finished executing.<br/> |
| [TaskStatus](TaskRun_TaskStatus.md 'Microsoft.FactoryOrchestrator.Core.TaskRun.TaskStatus') | The status of this run.<br/> |
| [TaskType](TaskRun_TaskType.md 'Microsoft.FactoryOrchestrator.Core.TaskRun.TaskType') | The type of the Task which created this run.<br/> |
| [TimeFinished](TaskRun_TimeFinished.md 'Microsoft.FactoryOrchestrator.Core.TaskRun.TimeFinished') | The time this run finished. NULL if it has never finished.<br/> |
| [TimeoutSeconds](TaskRun_TimeoutSeconds.md 'Microsoft.FactoryOrchestrator.Core.TaskRun.TimeoutSeconds') | The timeout for this run.<br/> |
| [TimeStarted](TaskRun_TimeStarted.md 'Microsoft.FactoryOrchestrator.Core.TaskRun.TimeStarted') | The time this run started. NULL if it has never started.<br/> |

| Methods | |
| :--- | :--- |
| [DeepCopy()](TaskRun_DeepCopy().md 'Microsoft.FactoryOrchestrator.Core.TaskRun.DeepCopy()') | Create a "deep" copy of the TaskRun. WARNING: If the object is a ServerTaskRun information is lost!<br/> |
| [Equals(object)](TaskRun_Equals(object).md 'Microsoft.FactoryOrchestrator.Core.TaskRun.Equals(object)') | Determines whether the specified [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object'), is equal to this instance.<br/> |
| [GetHashCode()](TaskRun_GetHashCode().md 'Microsoft.FactoryOrchestrator.Core.TaskRun.GetHashCode()') | Returns a hash code for this instance.<br/> |
