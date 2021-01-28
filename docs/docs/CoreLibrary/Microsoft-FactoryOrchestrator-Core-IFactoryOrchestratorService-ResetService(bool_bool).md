#### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](./Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.ResetService(bool, bool) Method
Stops all running Tasks and deletes all TaskLists.  
```csharp
void ResetService(bool preserveLogs=false, bool factoryReset=false);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService-ResetService(bool_bool)-preserveLogs'></a>
`preserveLogs` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, are logs not deleted.  
  
<a name='Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService-ResetService(bool_bool)-factoryReset'></a>
`factoryReset` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, the service is restarted as if it is first boot.  
  
