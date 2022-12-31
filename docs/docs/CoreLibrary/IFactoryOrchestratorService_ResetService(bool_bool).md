### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.ResetService(bool, bool) Method
Stops all running Tasks and deletes all TaskLists.  
```csharp
void ResetService(bool preserveLogs=false, bool factoryReset=false);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService_ResetService(bool_bool)_preserveLogs'></a>
`preserveLogs` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, are logs not deleted.
  
<a name='Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService_ResetService(bool_bool)_factoryReset'></a>
`factoryReset` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, the service is restarted as if it is first boot. NOTE: Network communication is not disabled, connected clients may encounter issues and the 'EnableNetworkAccess' setting will be ignored! 
  
