### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.QueryTaskRun(Guid) Method
Gets a TaskRun object.  
```csharp
Microsoft.FactoryOrchestrator.Core.TaskRun QueryTaskRun(System.Guid taskRunGuid);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService_QueryTaskRun(System_Guid)_taskRunGuid'></a>
`taskRunGuid` [System.Guid](https://docs.microsoft.com/en-us/dotnet/api/System.Guid 'System.Guid')  
The GUID of the desired TaskRun
  
#### Returns
[TaskRun](Microsoft_FactoryOrchestrator_Core_TaskRun.md 'Microsoft.FactoryOrchestrator.Core.TaskRun')  
The TaskRun object.
