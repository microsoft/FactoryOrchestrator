#### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
## TaskBase Class
TaskBase is an abstract class representing a generic task. It contains all the details needed to run the task.  
It also surfaces information about the last TaskRun for this task, for easy consumption.  
```csharp
public abstract class TaskBase : Microsoft.FactoryOrchestrator.Core.NotifyPropertyChangedBase
```
Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; [Microsoft.FactoryOrchestrator.Core.NotifyPropertyChangedBase](./../../CoreLibrary/Microsoft-FactoryOrchestrator-Core-NotifyPropertyChangedBase 'Microsoft.FactoryOrchestrator.Core.NotifyPropertyChangedBase') &#129106; TaskBase  

Derived  
&#8627; [ExecutableTask](./Microsoft-FactoryOrchestrator-Core-ExecutableTask.md 'Microsoft.FactoryOrchestrator.Core.ExecutableTask')  
&#8627; [ExternalTask](./Microsoft-FactoryOrchestrator-Core-ExternalTask.md 'Microsoft.FactoryOrchestrator.Core.ExternalTask')  
### Constructors
- [TaskBase(Microsoft.FactoryOrchestrator.Core.TaskType)](./Microsoft-FactoryOrchestrator-Core-TaskBase-TaskBase(Microsoft-FactoryOrchestrator-Core-TaskType).md 'Microsoft.FactoryOrchestrator.Core.TaskBase.TaskBase(Microsoft.FactoryOrchestrator.Core.TaskType)')
- [TaskBase(string, Microsoft.FactoryOrchestrator.Core.TaskType)](./Microsoft-FactoryOrchestrator-Core-TaskBase-TaskBase(string_Microsoft-FactoryOrchestrator-Core-TaskType).md 'Microsoft.FactoryOrchestrator.Core.TaskBase.TaskBase(string, Microsoft.FactoryOrchestrator.Core.TaskType)')
### Properties
- [AbortTaskListOnFailed](./Microsoft-FactoryOrchestrator-Core-TaskBase-AbortTaskListOnFailed.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.AbortTaskListOnFailed')
- [Arguments](./Microsoft-FactoryOrchestrator-Core-TaskBase-Arguments.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.Arguments')
- [Guid](./Microsoft-FactoryOrchestrator-Core-TaskBase-Guid.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.Guid')
- [IsRunningOrPending](./Microsoft-FactoryOrchestrator-Core-TaskBase-IsRunningOrPending.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.IsRunningOrPending')
- [LatestTaskRunExitCode](./Microsoft-FactoryOrchestrator-Core-TaskBase-LatestTaskRunExitCode.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.LatestTaskRunExitCode')
- [LatestTaskRunGuid](./Microsoft-FactoryOrchestrator-Core-TaskBase-LatestTaskRunGuid.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.LatestTaskRunGuid')
- [LatestTaskRunPassed](./Microsoft-FactoryOrchestrator-Core-TaskBase-LatestTaskRunPassed.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.LatestTaskRunPassed')
- [LatestTaskRunRunTime](./Microsoft-FactoryOrchestrator-Core-TaskBase-LatestTaskRunRunTime.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.LatestTaskRunRunTime')
- [LatestTaskRunStatus](./Microsoft-FactoryOrchestrator-Core-TaskBase-LatestTaskRunStatus.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.LatestTaskRunStatus')
- [LatestTaskRunTimeFinished](./Microsoft-FactoryOrchestrator-Core-TaskBase-LatestTaskRunTimeFinished.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.LatestTaskRunTimeFinished')
- [LatestTaskRunTimeStarted](./Microsoft-FactoryOrchestrator-Core-TaskBase-LatestTaskRunTimeStarted.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.LatestTaskRunTimeStarted')
- [MaxNumberOfRetries](./Microsoft-FactoryOrchestrator-Core-TaskBase-MaxNumberOfRetries.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.MaxNumberOfRetries')
- [Name](./Microsoft-FactoryOrchestrator-Core-TaskBase-Name.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.Name')
- [Path](./Microsoft-FactoryOrchestrator-Core-TaskBase-Path.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.Path')
- [RunByClient](./Microsoft-FactoryOrchestrator-Core-TaskBase-RunByClient.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.RunByClient')
- [RunByServer](./Microsoft-FactoryOrchestrator-Core-TaskBase-RunByServer.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.RunByServer')
- [RunInContainer](./Microsoft-FactoryOrchestrator-Core-TaskBase-RunInContainer.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.RunInContainer')
- [TaskLock](./Microsoft-FactoryOrchestrator-Core-TaskBase-TaskLock.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.TaskLock')
- [TaskRunGuids](./Microsoft-FactoryOrchestrator-Core-TaskBase-TaskRunGuids.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.TaskRunGuids')
- [TimeoutSeconds](./Microsoft-FactoryOrchestrator-Core-TaskBase-TimeoutSeconds.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.TimeoutSeconds')
- [TimesRetried](./Microsoft-FactoryOrchestrator-Core-TaskBase-TimesRetried.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.TimesRetried')
- [Type](./Microsoft-FactoryOrchestrator-Core-TaskBase-Type.md 'Microsoft.FactoryOrchestrator.Core.TaskBase.Type')
### Methods
- [CreateTaskFromTaskRun(Microsoft.FactoryOrchestrator.Core.TaskRun)](./Microsoft-FactoryOrchestrator-Core-TaskBase-CreateTaskFromTaskRun(Microsoft-FactoryOrchestrator-Core-TaskRun).md 'Microsoft.FactoryOrchestrator.Core.TaskBase.CreateTaskFromTaskRun(Microsoft.FactoryOrchestrator.Core.TaskRun)')
- [DeepCopy()](./Microsoft-FactoryOrchestrator-Core-TaskBase-DeepCopy().md 'Microsoft.FactoryOrchestrator.Core.TaskBase.DeepCopy()')
- [Equals(object)](./Microsoft-FactoryOrchestrator-Core-TaskBase-Equals(object).md 'Microsoft.FactoryOrchestrator.Core.TaskBase.Equals(object)')
- [GetHashCode()](./Microsoft-FactoryOrchestrator-Core-TaskBase-GetHashCode().md 'Microsoft.FactoryOrchestrator.Core.TaskBase.GetHashCode()')
- [ShouldSerializeAbortTaskListOnFailed()](./Microsoft-FactoryOrchestrator-Core-TaskBase-ShouldSerializeAbortTaskListOnFailed().md 'Microsoft.FactoryOrchestrator.Core.TaskBase.ShouldSerializeAbortTaskListOnFailed()')
- [ShouldSerializeLatestTaskRunExitCode()](./Microsoft-FactoryOrchestrator-Core-TaskBase-ShouldSerializeLatestTaskRunExitCode().md 'Microsoft.FactoryOrchestrator.Core.TaskBase.ShouldSerializeLatestTaskRunExitCode()')
- [ShouldSerializeLatestTaskRunStatus()](./Microsoft-FactoryOrchestrator-Core-TaskBase-ShouldSerializeLatestTaskRunStatus().md 'Microsoft.FactoryOrchestrator.Core.TaskBase.ShouldSerializeLatestTaskRunStatus()')
- [ShouldSerializeLatestTaskRunTimeFinished()](./Microsoft-FactoryOrchestrator-Core-TaskBase-ShouldSerializeLatestTaskRunTimeFinished().md 'Microsoft.FactoryOrchestrator.Core.TaskBase.ShouldSerializeLatestTaskRunTimeFinished()')
- [ShouldSerializeLatestTaskRunTimeStarted()](./Microsoft-FactoryOrchestrator-Core-TaskBase-ShouldSerializeLatestTaskRunTimeStarted().md 'Microsoft.FactoryOrchestrator.Core.TaskBase.ShouldSerializeLatestTaskRunTimeStarted()')
- [ShouldSerializeMaxNumberOfRetries()](./Microsoft-FactoryOrchestrator-Core-TaskBase-ShouldSerializeMaxNumberOfRetries().md 'Microsoft.FactoryOrchestrator.Core.TaskBase.ShouldSerializeMaxNumberOfRetries()')
- [ShouldSerializeRunInContainer()](./Microsoft-FactoryOrchestrator-Core-TaskBase-ShouldSerializeRunInContainer().md 'Microsoft.FactoryOrchestrator.Core.TaskBase.ShouldSerializeRunInContainer()')
- [ShouldSerializeTaskRunGuids()](./Microsoft-FactoryOrchestrator-Core-TaskBase-ShouldSerializeTaskRunGuids().md 'Microsoft.FactoryOrchestrator.Core.TaskBase.ShouldSerializeTaskRunGuids()')
- [ShouldSerializeTimeoutSeconds()](./Microsoft-FactoryOrchestrator-Core-TaskBase-ShouldSerializeTimeoutSeconds().md 'Microsoft.FactoryOrchestrator.Core.TaskBase.ShouldSerializeTimeoutSeconds()')
- [ShouldSerializeTimesRetried()](./Microsoft-FactoryOrchestrator-Core-TaskBase-ShouldSerializeTimesRetried().md 'Microsoft.FactoryOrchestrator.Core.TaskBase.ShouldSerializeTimesRetried()')
