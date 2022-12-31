### [Microsoft.FactoryOrchestrator.Client](Microsoft_FactoryOrchestrator_Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.RunTask(TaskBase, Nullable&lt;Guid&gt;) Method
Asynchronously Runs a Task outside of a TaskList.  
```csharp
public System.Threading.Tasks.Task<Microsoft.FactoryOrchestrator.Core.TaskRun> RunTask(Microsoft.FactoryOrchestrator.Core.TaskBase task, System.Nullable<System.Guid> desiredTaskRunGuid=null);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_RunTask(Microsoft_FactoryOrchestrator_Core_TaskBase_System_Nullable_System_Guid_)_task'></a>
`task` [TaskBase](./../CoreLibrary/TaskBase.md 'Microsoft.FactoryOrchestrator.Core.TaskBase')  
The Task to run.
  
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_RunTask(Microsoft_FactoryOrchestrator_Core_TaskBase_System_Nullable_System_Guid_)_desiredTaskRunGuid'></a>
`desiredTaskRunGuid` [System.Nullable&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable')[System.Guid](https://docs.microsoft.com/en-us/dotnet/api/System.Guid 'System.Guid')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable')  
The desired GUID for the returned TaskRun. It is not used if a TaskRun already exists with the same GUID.
  
#### Returns
[System.Threading.Tasks.Task&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task')[TaskRun](./../CoreLibrary/TaskRun.md 'Microsoft.FactoryOrchestrator.Core.TaskRun')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task')  
The TaskRun associated with the run.
