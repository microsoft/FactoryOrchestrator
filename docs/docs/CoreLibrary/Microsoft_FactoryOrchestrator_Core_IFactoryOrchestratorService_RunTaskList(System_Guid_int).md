### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.RunTaskList(Guid, int) Method
Executes a TaskList.  
```csharp
void RunTaskList(System.Guid taskListGuid, int initialTask=0);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService_RunTaskList(System_Guid_int)_taskListGuid'></a>
`taskListGuid` [System.Guid](https://docs.microsoft.com/en-us/dotnet/api/System.Guid 'System.Guid')  
GUID of the TaskList to run.
  
<a name='Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService_RunTaskList(System_Guid_int)_initialTask'></a>
`initialTask` [System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')  
Index of the Task to start the run from.
  
