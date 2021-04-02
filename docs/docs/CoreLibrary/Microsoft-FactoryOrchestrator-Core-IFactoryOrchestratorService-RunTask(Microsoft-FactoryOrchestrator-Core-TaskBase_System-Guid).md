#### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](./Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.RunTask(Microsoft.FactoryOrchestrator.Core.TaskBase, System.Guid) Method
Runs a Task outside of a TaskList.  
```csharp
Microsoft.FactoryOrchestrator.Core.TaskRun RunTask(Microsoft.FactoryOrchestrator.Core.TaskBase task, System.Guid desiredTaskRunGuid);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService-RunTask(Microsoft-FactoryOrchestrator-Core-TaskBase_System-Guid)-task'></a>
`task` [TaskBase](./Microsoft-FactoryOrchestrator-Core-TaskBase.md 'Microsoft.FactoryOrchestrator.Core.TaskBase')  
The Task to run.  
  
<a name='Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService-RunTask(Microsoft-FactoryOrchestrator-Core-TaskBase_System-Guid)-desiredTaskRunGuid'></a>
`desiredTaskRunGuid` [System.Guid](https://docs.microsoft.com/en-us/dotnet/api/System.Guid 'System.Guid')  
The desired GUID for the returned TaskRun. It is not used if a TaskRun already exists with the same GUID.  
  
#### Returns
[TaskRun](./Microsoft-FactoryOrchestrator-Core-TaskRun.md 'Microsoft.FactoryOrchestrator.Core.TaskRun')  
The TaskRun associated with the run.  
