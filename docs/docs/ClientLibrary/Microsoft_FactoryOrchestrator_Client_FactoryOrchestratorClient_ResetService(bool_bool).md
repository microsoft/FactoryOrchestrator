### [Microsoft.FactoryOrchestrator.Client](Microsoft_FactoryOrchestrator_Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.ResetService(bool, bool) Method
Asynchronously Stops all running Tasks and deletes all TaskLists.  
```csharp
public System.Threading.Tasks.Task ResetService(bool preserveLogs=false, bool factoryReset=false);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_ResetService(bool_bool)_preserveLogs'></a>
`preserveLogs` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, are logs not deleted.
  
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_ResetService(bool_bool)_factoryReset'></a>
`factoryReset` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, the service is restarted as if it is first boot. NOTE: Network communication is not disabled, connected clients may encounter issues and the 'EnableNetworkAccess' setting will be ignored! 
  
#### Returns
[System.Threading.Tasks.Task](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task 'System.Threading.Tasks.Task')  
