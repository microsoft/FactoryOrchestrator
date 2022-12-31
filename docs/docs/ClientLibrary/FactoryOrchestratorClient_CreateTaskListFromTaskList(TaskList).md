### [Microsoft.FactoryOrchestrator.Client](Microsoft_FactoryOrchestrator_Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.CreateTaskListFromTaskList(TaskList) Method
Asynchronously Creates a TaskList on the Service by copying a TaskList object provided by the Client.  
```csharp
public System.Threading.Tasks.Task<Microsoft.FactoryOrchestrator.Core.TaskList> CreateTaskListFromTaskList(Microsoft.FactoryOrchestrator.Core.TaskList list);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_CreateTaskListFromTaskList(Microsoft_FactoryOrchestrator_Core_TaskList)_list'></a>
`list` [TaskList](./../CoreLibrary/TaskList.md 'Microsoft.FactoryOrchestrator.Core.TaskList')  
The TaskList to add to the Service.
  
#### Returns
[System.Threading.Tasks.Task&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task')[TaskList](./../CoreLibrary/TaskList.md 'Microsoft.FactoryOrchestrator.Core.TaskList')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task')  
The created Service TaskList.
