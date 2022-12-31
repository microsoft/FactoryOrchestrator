### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core')
## TaskBase Class
TaskBase is an abstract class representing a generic task. It contains all the details needed to run the task.  
It also surfaces information about the last TaskRun for this task, for easy consumption.  
```csharp
public abstract class TaskBase : Microsoft.FactoryOrchestrator.Core.NotifyPropertyChangedBase
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; [NotifyPropertyChangedBase](NotifyPropertyChangedBase.md 'Microsoft.FactoryOrchestrator.Core.NotifyPropertyChangedBase') &#129106; TaskBase  

Derived  
&#8627; [ExecutableTask](ExecutableTask.md 'Microsoft.FactoryOrchestrator.Core.ExecutableTask')  
&#8627; [ExternalTask](ExternalTask.md 'Microsoft.FactoryOrchestrator.Core.ExternalTask')  

| Constructors | |
| :--- | :--- |
| [TaskBase(TaskType)](TaskBase_TaskBase(TaskType).md 'Microsoft.FactoryOrchestrator.Core.TaskBase.TaskBase(Microsoft.FactoryOrchestrator.Core.TaskType)') | Constructor.<br/> |
| [TaskBase(string, TaskType)](TaskBase_TaskBase(string_TaskType).md 'Microsoft.FactoryOrchestrator.Core.TaskBase.TaskBase(string, Microsoft.FactoryOrchestrator.Core.TaskType)') | Constructor.<br/> |

| Properties | |
| :--- | :--- |
| [AbortTaskListOnFailed](TaskBase_AbortTaskListOnFailed.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.AbortTaskListOnFailed') | If true, the TaskList running this Task is aborted if this Task fails.<br/> |
| [Arguments](TaskBase_Arguments.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.Arguments') | The arguments passed to the Task.<br/> |
| [Guid](TaskBase_Guid.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.Guid') | The GUID identifying the Task.<br/> |
| [IsRunningOrPending](TaskBase_IsRunningOrPending.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.IsRunningOrPending') | True if the Task is running or queued to run.<br/> |
| [LatestTaskRunExitCode](TaskBase_LatestTaskRunExitCode.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.LatestTaskRunExitCode') | The exit code of the latest run of this Task. NULL if it has never completed.<br/> |
| [LatestTaskRunGuid](TaskBase_LatestTaskRunGuid.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.LatestTaskRunGuid') | GUID of the latest run of this Task. NULL if it has never started.<br/> |
| [LatestTaskRunPassed](TaskBase_LatestTaskRunPassed.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.LatestTaskRunPassed') | True if the latest run of this Task passed. NULL if it has never been run.<br/> |
| [LatestTaskRunRunTime](TaskBase_LatestTaskRunRunTime.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.LatestTaskRunRunTime') | The amount of time elapsed while running the latest run of this Task. NULL if it has never started.<br/> |
| [LatestTaskRunStatus](TaskBase_LatestTaskRunStatus.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.LatestTaskRunStatus') | The status of the latest run of this Task.<br/> |
| [LatestTaskRunTimeFinished](TaskBase_LatestTaskRunTimeFinished.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.LatestTaskRunTimeFinished') | The time the latest run of this Task finished. NULL if it has never finished.<br/> |
| [LatestTaskRunTimeStarted](TaskBase_LatestTaskRunTimeStarted.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.LatestTaskRunTimeStarted') | The time the latest run of this Task started. NULL if it has never started.<br/> |
| [MaxNumberOfRetries](TaskBase_MaxNumberOfRetries.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.MaxNumberOfRetries') | The number of re-runs the Task automatically attempts if the run fails.<br/> |
| [Name](TaskBase_Name.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.Name') | The friendly name of the Task.<br/> |
| [Path](TaskBase_Path.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.Path') | The path to the file used for the Task such an Exe.<br/> |
| [RunByClient](TaskBase_RunByClient.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.RunByClient') | True if this Task is run by the client, such as an ExternalTask.<br/> |
| [RunByServer](TaskBase_RunByServer.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.RunByServer') | True if this Task is run by the server, such as an ExecutableTask.<br/> |
| [RunInContainer](TaskBase_RunInContainer.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.RunInContainer') | If true, the task is executed inside the Win32 container.<br/> |
| [TaskLock](TaskBase_TaskLock.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.TaskLock') | Lock used by the server when updating values in the Task object. Clients should not use.<br/> |
| [TaskRunGuids](TaskBase_TaskRunGuids.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.TaskRunGuids') | The GUIDs for all runs of this Task.<br/> |
| [TimeoutSeconds](TaskBase_TimeoutSeconds.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.TimeoutSeconds') | The timeout for this Task, in seconds.<br/> |
| [TimesRetried](TaskBase_TimesRetried.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.TimesRetried') | The number of reties so far for this latest run.<br/> |
| [Type](TaskBase_Type.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.Type') | The type of the Task.<br/> |

| Methods | |
| :--- | :--- |
| [CreateTaskFromTaskRun(TaskRun)](TaskBase_CreateTaskFromTaskRun(TaskRun).md 'Microsoft.FactoryOrchestrator.Core.TaskBase.CreateTaskFromTaskRun(Microsoft.FactoryOrchestrator.Core.TaskRun)') | Creates a Task object from a TaskRun object.<br/> |
| [DeepCopy()](TaskBase_DeepCopy().md 'Microsoft.FactoryOrchestrator.Core.TaskBase.DeepCopy()') | Create a "deep" copy of the Task.<br/> |
| [Equals(object)](TaskBase_Equals(object).md 'Microsoft.FactoryOrchestrator.Core.TaskBase.Equals(object)') | Determines whether the specified [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object'), is equal to this instance.<br/> |
| [GetHashCode()](TaskBase_GetHashCode().md 'Microsoft.FactoryOrchestrator.Core.TaskBase.GetHashCode()') | Returns a hash code for this instance.<br/> |
| [ShouldSerializeAbortTaskListOnFailed()](TaskBase_ShouldSerializeAbortTaskListOnFailed().md 'Microsoft.FactoryOrchestrator.Core.TaskBase.ShouldSerializeAbortTaskListOnFailed()') | XmlSerializer calls to check if this should be serialized.<br/> |
| [ShouldSerializeLatestTaskRunExitCode()](TaskBase_ShouldSerializeLatestTaskRunExitCode().md 'Microsoft.FactoryOrchestrator.Core.TaskBase.ShouldSerializeLatestTaskRunExitCode()') | XmlSerializer calls to check if this should be serialized.<br/> |
| [ShouldSerializeLatestTaskRunStatus()](TaskBase_ShouldSerializeLatestTaskRunStatus().md 'Microsoft.FactoryOrchestrator.Core.TaskBase.ShouldSerializeLatestTaskRunStatus()') | XmlSerializer calls to check if this should be serialized.<br/> |
| [ShouldSerializeLatestTaskRunTimeFinished()](TaskBase_ShouldSerializeLatestTaskRunTimeFinished().md 'Microsoft.FactoryOrchestrator.Core.TaskBase.ShouldSerializeLatestTaskRunTimeFinished()') | XmlSerializer calls to check if this should be serialized.<br/> |
| [ShouldSerializeLatestTaskRunTimeStarted()](TaskBase_ShouldSerializeLatestTaskRunTimeStarted().md 'Microsoft.FactoryOrchestrator.Core.TaskBase.ShouldSerializeLatestTaskRunTimeStarted()') | XmlSerializer calls to check if this should be serialized.<br/> |
| [ShouldSerializeMaxNumberOfRetries()](TaskBase_ShouldSerializeMaxNumberOfRetries().md 'Microsoft.FactoryOrchestrator.Core.TaskBase.ShouldSerializeMaxNumberOfRetries()') | XmlSerializer calls to check if this should be serialized.<br/> |
| [ShouldSerializeRunInContainer()](TaskBase_ShouldSerializeRunInContainer().md 'Microsoft.FactoryOrchestrator.Core.TaskBase.ShouldSerializeRunInContainer()') | XmlSerializer calls to check if this should be serialized.<br/> |
| [ShouldSerializeTaskRunGuids()](TaskBase_ShouldSerializeTaskRunGuids().md 'Microsoft.FactoryOrchestrator.Core.TaskBase.ShouldSerializeTaskRunGuids()') | XmlSerializer calls to check if this should be serialized.<br/> |
| [ShouldSerializeTimeoutSeconds()](TaskBase_ShouldSerializeTimeoutSeconds().md 'Microsoft.FactoryOrchestrator.Core.TaskBase.ShouldSerializeTimeoutSeconds()') | XmlSerializer calls to check if this should be serialized.<br/> |
| [ShouldSerializeTimesRetried()](TaskBase_ShouldSerializeTimesRetried().md 'Microsoft.FactoryOrchestrator.Core.TaskBase.ShouldSerializeTimesRetried()') | XmlSerializer calls to check if this should be serialized.<br/> |
