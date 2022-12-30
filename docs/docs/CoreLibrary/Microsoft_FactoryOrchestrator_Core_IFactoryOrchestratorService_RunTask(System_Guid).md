### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.RunTask(Guid) Method
Runs a Task outside of a TaskList.  
```csharp
Microsoft.FactoryOrchestrator.Core.TaskRun RunTask(System.Guid taskGuid);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService_RunTask(System_Guid)_taskGuid'></a>
`taskGuid` [System.Guid](https://docs.microsoft.com/en-us/dotnet/api/System.Guid 'System.Guid')  
The GUID of the Task to run.
  
#### Returns
[TaskRun](Microsoft_FactoryOrchestrator_Core_TaskRun.md 'Microsoft.FactoryOrchestrator.Core.TaskRun')  
The TaskRun associated with the run.
