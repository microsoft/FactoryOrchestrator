#### [FactoryOrchestratorClientLibrary](./FactoryOrchestratorClientLibrary.md 'FactoryOrchestratorClientLibrary')
### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClientSync](./Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClientSync.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClientSync')
## FactoryOrchestratorClientSync.ResetService(bool, bool) Method
Stops all running Tasks and deletes all TaskLists.  
```csharp
public void ResetService(bool preserveLogs=false, bool factoryReset=false);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClientSync-ResetService(bool_bool)-preserveLogs'></a>
`preserveLogs` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, are logs not deleted.  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClientSync-ResetService(bool_bool)-factoryReset'></a>
`factoryReset` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, the service is restarted as if it is first boot.  
  
