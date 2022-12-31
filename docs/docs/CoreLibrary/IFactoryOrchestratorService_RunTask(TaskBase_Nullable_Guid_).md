### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.RunTask(TaskBase, Nullable&lt;Guid&gt;) Method
Runs a Task outside of a TaskList.  
```csharp
Microsoft.FactoryOrchestrator.Core.TaskRun RunTask(Microsoft.FactoryOrchestrator.Core.TaskBase task, System.Nullable<System.Guid> desiredTaskRunGuid=null);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService_RunTask(Microsoft_FactoryOrchestrator_Core_TaskBase_System_Nullable_System_Guid_)_task'></a>
`task` [TaskBase](TaskBase.md 'Microsoft.FactoryOrchestrator.Core.TaskBase')  
The Task to run.
  
<a name='Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService_RunTask(Microsoft_FactoryOrchestrator_Core_TaskBase_System_Nullable_System_Guid_)_desiredTaskRunGuid'></a>
`desiredTaskRunGuid` [System.Nullable&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable')[System.Guid](https://docs.microsoft.com/en-us/dotnet/api/System.Guid 'System.Guid')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable')  
The desired GUID for the returned TaskRun. It is not used if a TaskRun already exists with the same GUID.
  
#### Returns
[TaskRun](TaskRun.md 'Microsoft.FactoryOrchestrator.Core.TaskRun')  
The TaskRun associated with the run.
