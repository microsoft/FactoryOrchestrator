#### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client')
### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](./Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.DeleteTaskList(System.Guid) Method
Asynchronously Deletes a TaskList on the Service.  
```csharp
public System.Threading.Tasks.Task DeleteTaskList(System.Guid listToDelete);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-DeleteTaskList(System-Guid)-listToDelete'></a>
`listToDelete` [System.Guid](https://docs.microsoft.com/en-us/dotnet/api/System.Guid 'System.Guid')  
The GUID of the TaskList to delete.  
  
#### Returns
[System.Threading.Tasks.Task](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task 'System.Threading.Tasks.Task')  
true if it was deleted successfully.  
