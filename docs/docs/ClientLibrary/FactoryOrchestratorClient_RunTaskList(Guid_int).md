### [Microsoft.FactoryOrchestrator.Client](Microsoft_FactoryOrchestrator_Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.RunTaskList(Guid, int) Method
Asynchronously Executes a TaskList.  
```csharp
public System.Threading.Tasks.Task RunTaskList(System.Guid taskListGuid, int initialTask=0);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_RunTaskList(System_Guid_int)_taskListGuid'></a>
`taskListGuid` [System.Guid](https://docs.microsoft.com/en-us/dotnet/api/System.Guid 'System.Guid')  
GUID of the TaskList to run.
  
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_RunTaskList(System_Guid_int)_initialTask'></a>
`initialTask` [System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')  
Index of the Task to start the run from.
  
#### Returns
[System.Threading.Tasks.Task](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task 'System.Threading.Tasks.Task')  
