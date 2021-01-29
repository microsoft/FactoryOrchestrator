#### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client')
### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](./Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.RunTask(Microsoft.FactoryOrchestrator.Core.TaskBase) Method
Asynchronously Runs a Task outside of a TaskList.  
```csharp
public System.Threading.Tasks.Task<Microsoft.FactoryOrchestrator.Core.TaskRun> RunTask(Microsoft.FactoryOrchestrator.Core.TaskBase task);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-RunTask(Microsoft-FactoryOrchestrator-Core-TaskBase)-task'></a>
`task` [Microsoft.FactoryOrchestrator.Core.TaskBase](./../../CoreLibrary/Microsoft-FactoryOrchestrator-Core-TaskBase 'Microsoft.FactoryOrchestrator.Core.TaskBase')  
The Task to run.  
  
#### Returns
[System.Threading.Tasks.Task&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task')[Microsoft.FactoryOrchestrator.Core.TaskRun](./../../CoreLibrary/Microsoft-FactoryOrchestrator-Core-TaskRun 'Microsoft.FactoryOrchestrator.Core.TaskRun')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task')  
The TaskRun associated with the run.  
