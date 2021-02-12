#### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](./Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.RunTask(Microsoft.FactoryOrchestrator.Core.TaskBase, System.Nullable&lt;System.Guid&gt;) Method
Runs a Task outside of a TaskList.  
```csharp
Microsoft.FactoryOrchestrator.Core.TaskRun RunTask(Microsoft.FactoryOrchestrator.Core.TaskBase task, System.Nullable<System.Guid> desiredTaskRunGuid=null);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService-RunTask(Microsoft-FactoryOrchestrator-Core-TaskBase_System-Nullable-System-Guid-)-task'></a>
`task` [TaskBase](./Microsoft-FactoryOrchestrator-Core-TaskBase.md 'Microsoft.FactoryOrchestrator.Core.TaskBase')  
The Task to run.  
  
<a name='Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService-RunTask(Microsoft-FactoryOrchestrator-Core-TaskBase_System-Nullable-System-Guid-)-desiredTaskRunGuid'></a>
`desiredTaskRunGuid` [System.Nullable&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable')[System.Guid](https://docs.microsoft.com/en-us/dotnet/api/System.Guid 'System.Guid')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable')  
The desired GUID for the returned TaskRun. It is not used if a TaskRun already exists with the same GUID.  
  
#### Returns
[TaskRun](./Microsoft-FactoryOrchestrator-Core-TaskRun.md 'Microsoft.FactoryOrchestrator.Core.TaskRun')  
The TaskRun associated with the run.  
