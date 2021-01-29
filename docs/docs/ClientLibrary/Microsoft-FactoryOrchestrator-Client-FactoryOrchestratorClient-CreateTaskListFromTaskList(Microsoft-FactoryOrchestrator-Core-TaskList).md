#### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client')
### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](./Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.CreateTaskListFromTaskList(Microsoft.FactoryOrchestrator.Core.TaskList) Method
Asynchronously Creates a TaskList on the Service by copying a TaskList object provided by the Client.  
```csharp
public System.Threading.Tasks.Task<Microsoft.FactoryOrchestrator.Core.TaskList> CreateTaskListFromTaskList(Microsoft.FactoryOrchestrator.Core.TaskList list);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-CreateTaskListFromTaskList(Microsoft-FactoryOrchestrator-Core-TaskList)-list'></a>
`list` [Microsoft.FactoryOrchestrator.Core.TaskList](./../../CoreLibrary/Microsoft-FactoryOrchestrator-Core-TaskList 'Microsoft.FactoryOrchestrator.Core.TaskList')  
The TaskList to add to the Service.  
  
#### Returns
[System.Threading.Tasks.Task&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task')[Microsoft.FactoryOrchestrator.Core.TaskList](./../../CoreLibrary/Microsoft-FactoryOrchestrator-Core-TaskList 'Microsoft.FactoryOrchestrator.Core.TaskList')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task')  
The created Service TaskList.  
