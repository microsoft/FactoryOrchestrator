#### [FactoryOrchestratorClientLibrary](./FactoryOrchestratorClientLibrary.md 'FactoryOrchestratorClientLibrary')
### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClientSync](./Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClientSync.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClientSync')
## FactoryOrchestratorClientSync.IsExecutingBootTasks() Method
Checks if the service is executing boot tasks. While executing boot tasks, many commands cannot be run.  
```csharp
public bool IsExecutingBootTasks();
```
#### Returns
[System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
`true` is the service is executing boot tasks.  
