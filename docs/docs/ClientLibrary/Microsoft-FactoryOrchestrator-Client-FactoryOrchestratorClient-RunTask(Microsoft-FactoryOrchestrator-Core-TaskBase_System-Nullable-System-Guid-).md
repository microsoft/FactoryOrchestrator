#### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client')
### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](./Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.RunTask(Microsoft.FactoryOrchestrator.Core.TaskBase, System.Nullable&lt;System.Guid&gt;) Method
Asynchronously Runs a Task outside of a TaskList.  
```csharp
public System.Threading.Tasks.Task<Microsoft.FactoryOrchestrator.Core.TaskRun> RunTask(Microsoft.FactoryOrchestrator.Core.TaskBase task, System.Nullable<System.Guid> desiredTaskRunGuid=null);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-RunTask(Microsoft-FactoryOrchestrator-Core-TaskBase_System-Nullable-System-Guid-)-task'></a>
`task` [Microsoft.FactoryOrchestrator.Core.TaskBase](./../../CoreLibrary/Microsoft-FactoryOrchestrator-Core-TaskBase 'Microsoft.FactoryOrchestrator.Core.TaskBase')  
The Task to run.  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-RunTask(Microsoft-FactoryOrchestrator-Core-TaskBase_System-Nullable-System-Guid-)-desiredTaskRunGuid'></a>
`desiredTaskRunGuid` [System.Nullable&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable')[System.Guid](https://docs.microsoft.com/en-us/dotnet/api/System.Guid 'System.Guid')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable')  
The desired GUID for the returned TaskRun. It is not used if a TaskRun already exists with the same GUID.  
  
#### Returns
[System.Threading.Tasks.Task&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task')[Microsoft.FactoryOrchestrator.Core.TaskRun](./../../CoreLibrary/Microsoft-FactoryOrchestrator-Core-TaskRun 'Microsoft.FactoryOrchestrator.Core.TaskRun')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task')  
The TaskRun associated with the run.  
