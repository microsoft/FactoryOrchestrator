#### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](./Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.IsExecutingBootTasks() Method
Checks if the service is executing boot tasks. While executing boot tasks, many commands cannot be run.  
```csharp
bool IsExecutingBootTasks();
```
#### Returns
[System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
`true` is the service is executing boot tasks.  
