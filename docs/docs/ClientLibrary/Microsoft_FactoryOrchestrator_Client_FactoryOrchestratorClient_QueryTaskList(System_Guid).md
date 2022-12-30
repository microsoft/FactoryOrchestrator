### [Microsoft.FactoryOrchestrator.Client](Microsoft_FactoryOrchestrator_Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.QueryTaskList(Guid) Method
Asynchronously Gets the TaskList object for a given TaskList GUID.  
```csharp
public System.Threading.Tasks.Task<Microsoft.FactoryOrchestrator.Core.TaskList> QueryTaskList(System.Guid taskListGuid);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_QueryTaskList(System_Guid)_taskListGuid'></a>
`taskListGuid` [System.Guid](https://docs.microsoft.com/en-us/dotnet/api/System.Guid 'System.Guid')  
The TaskList GUID.
  
#### Returns
[System.Threading.Tasks.Task&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task')[TaskList](./../CoreLibrary/Microsoft_FactoryOrchestrator_Core_TaskList.md 'Microsoft.FactoryOrchestrator.Core.TaskList')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task')  
The TaskList object with that GUID.
