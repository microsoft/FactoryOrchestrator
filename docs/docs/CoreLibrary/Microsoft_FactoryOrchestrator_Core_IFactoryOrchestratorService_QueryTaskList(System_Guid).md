### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.QueryTaskList(Guid) Method
Gets the TaskList object for a given TaskList GUID.  
```csharp
Microsoft.FactoryOrchestrator.Core.TaskList QueryTaskList(System.Guid taskListGuid);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService_QueryTaskList(System_Guid)_taskListGuid'></a>
`taskListGuid` [System.Guid](https://docs.microsoft.com/en-us/dotnet/api/System.Guid 'System.Guid')  
The TaskList GUID.
  
#### Returns
[TaskList](Microsoft_FactoryOrchestrator_Core_TaskList.md 'Microsoft.FactoryOrchestrator.Core.TaskList')  
The TaskList object with that GUID.
