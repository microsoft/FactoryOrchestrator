### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.GetTaskListSummaries() Method
Gets TaskList summaries for every "active" TaskList on the Service.  If "IsExecutingBootTasks()" returns true, this returns the "boot" TaskLists. Otherwise, it returns the "normal" TaskLists. The summary contains basic info about the TaskList.  
```csharp
System.Collections.Generic.List<Microsoft.FactoryOrchestrator.Core.TaskListSummary> GetTaskListSummaries();
```
#### Returns
[System.Collections.Generic.List&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')[TaskListSummary](Microsoft_FactoryOrchestrator_Core_TaskListSummary.md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')  
A list of TaskListSummary objects.
