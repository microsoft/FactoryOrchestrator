### [Microsoft.FactoryOrchestrator.Client](Microsoft_FactoryOrchestrator_Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.ReorderTaskLists(List&lt;Guid&gt;) Method
Asynchronously Reorders the TaskLists known to the Service.  
```csharp
public System.Threading.Tasks.Task ReorderTaskLists(System.Collections.Generic.List<System.Guid> newOrder);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_ReorderTaskLists(System_Collections_Generic_List_System_Guid_)_newOrder'></a>
`newOrder` [System.Collections.Generic.List&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')[System.Guid](https://docs.microsoft.com/en-us/dotnet/api/System.Guid 'System.Guid')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')  
An ordered list of GUIDs corresponding to the TaskList GUIDs known to the Service.
  
#### Returns
[System.Threading.Tasks.Task](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task 'System.Threading.Tasks.Task')  
