### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.GetBootTaskListSummaries() Method
Gets "boot" TaskList summaries for every "boot" TaskList on the Service. The summary contains basic info about the TaskList.  
```csharp
System.Collections.Generic.List<Microsoft.FactoryOrchestrator.Core.TaskListSummary> GetBootTaskListSummaries();
```
#### Returns
[System.Collections.Generic.List&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')[TaskListSummary](Microsoft_FactoryOrchestrator_Core_TaskListSummary.md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')  
A list of TaskListSummary objects.
