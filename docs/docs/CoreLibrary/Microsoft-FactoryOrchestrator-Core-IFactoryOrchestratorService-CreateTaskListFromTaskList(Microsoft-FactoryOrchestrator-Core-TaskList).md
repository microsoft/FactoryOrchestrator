#### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](./Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.CreateTaskListFromTaskList(Microsoft.FactoryOrchestrator.Core.TaskList) Method
Creates a TaskList on the Service by copying a TaskList object provided by the Client.  
```csharp
Microsoft.FactoryOrchestrator.Core.TaskList CreateTaskListFromTaskList(Microsoft.FactoryOrchestrator.Core.TaskList list);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService-CreateTaskListFromTaskList(Microsoft-FactoryOrchestrator-Core-TaskList)-list'></a>
`list` [TaskList](./Microsoft-FactoryOrchestrator-Core-TaskList.md 'Microsoft.FactoryOrchestrator.Core.TaskList')  
The TaskList to add to the Service.  
  
#### Returns
[TaskList](./Microsoft-FactoryOrchestrator-Core-TaskList.md 'Microsoft.FactoryOrchestrator.Core.TaskList')  
The created Service TaskList.  
