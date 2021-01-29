#### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client')
### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](./Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.RunTask(System.Guid) Method
Asynchronously Runs a Task outside of a TaskList.  
```csharp
public System.Threading.Tasks.Task<Microsoft.FactoryOrchestrator.Core.TaskRun> RunTask(System.Guid taskGuid);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-RunTask(System-Guid)-taskGuid'></a>
`taskGuid` [System.Guid](https://docs.microsoft.com/en-us/dotnet/api/System.Guid 'System.Guid')  
The GUID of the Task to run.  
  
#### Returns
[System.Threading.Tasks.Task&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task')[Microsoft.FactoryOrchestrator.Core.TaskRun](./../../CoreLibrary/Microsoft-FactoryOrchestrator-Core-TaskRun 'Microsoft.FactoryOrchestrator.Core.TaskRun')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task')  
The TaskRun associated with the run.  
