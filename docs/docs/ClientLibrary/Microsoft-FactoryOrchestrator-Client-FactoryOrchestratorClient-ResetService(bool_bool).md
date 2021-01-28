#### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client')
### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](./Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.ResetService(bool, bool) Method
Asynchronously Stops all running Tasks and deletes all TaskLists.  
```csharp
public System.Threading.Tasks.Task ResetService(bool preserveLogs=false, bool factoryReset=false);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-ResetService(bool_bool)-preserveLogs'></a>
`preserveLogs` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, are logs not deleted.  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-ResetService(bool_bool)-factoryReset'></a>
`factoryReset` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, the service is restarted as if it is first boot.  
  
#### Returns
[System.Threading.Tasks.Task](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task 'System.Threading.Tasks.Task')  
