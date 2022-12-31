### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.CreateTaskListFromTaskList(TaskList) Method
Creates a TaskList on the Service by copying a TaskList object provided by the Client.  
```csharp
Microsoft.FactoryOrchestrator.Core.TaskList CreateTaskListFromTaskList(Microsoft.FactoryOrchestrator.Core.TaskList list);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService_CreateTaskListFromTaskList(Microsoft_FactoryOrchestrator_Core_TaskList)_list'></a>
`list` [TaskList](TaskList.md 'Microsoft.FactoryOrchestrator.Core.TaskList')  
The TaskList to add to the Service.
  
#### Returns
[TaskList](TaskList.md 'Microsoft.FactoryOrchestrator.Core.TaskList')  
The created Service TaskList.
