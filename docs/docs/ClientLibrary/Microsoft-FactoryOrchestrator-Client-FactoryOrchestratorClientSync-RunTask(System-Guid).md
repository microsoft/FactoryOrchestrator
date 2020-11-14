#### [FactoryOrchestratorClientLibrary](./FactoryOrchestratorClientLibrary.md 'FactoryOrchestratorClientLibrary')
### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClientSync](./Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClientSync.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClientSync')
## FactoryOrchestratorClientSync.RunTask(System.Guid) Method
Runs a Task outside of a TaskList.  
```csharp
public Microsoft.FactoryOrchestrator.Core.TaskRun RunTask(System.Guid taskGuid);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClientSync-RunTask(System-Guid)-taskGuid'></a>
`taskGuid` [System.Guid](https://docs.microsoft.com/en-us/dotnet/api/System.Guid 'System.Guid')  
The GUID of the Task to run.  
  
#### Returns
[Microsoft.FactoryOrchestrator.Core.TaskRun](https://docs.microsoft.com/en-us/dotnet/api/Microsoft.FactoryOrchestrator.Core.TaskRun 'Microsoft.FactoryOrchestrator.Core.TaskRun')  
The TaskRun associated with the run.  
