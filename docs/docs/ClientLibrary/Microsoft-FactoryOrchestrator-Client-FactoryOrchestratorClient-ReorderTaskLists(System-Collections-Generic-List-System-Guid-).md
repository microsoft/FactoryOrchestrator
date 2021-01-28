#### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client')
### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](./Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.ReorderTaskLists(System.Collections.Generic.List&lt;System.Guid&gt;) Method
Asynchronously Reorders the TaskLists known to the Service.  
```csharp
public System.Threading.Tasks.Task ReorderTaskLists(System.Collections.Generic.List<System.Guid> newOrder);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-ReorderTaskLists(System-Collections-Generic-List-System-Guid-)-newOrder'></a>
`newOrder` [System.Collections.Generic.List&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')[System.Guid](https://docs.microsoft.com/en-us/dotnet/api/System.Guid 'System.Guid')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')  
An ordered list of GUIDs corresponding to the TaskList GUIDs known to the Service.  
  
#### Returns
[System.Threading.Tasks.Task](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task 'System.Threading.Tasks.Task')  
