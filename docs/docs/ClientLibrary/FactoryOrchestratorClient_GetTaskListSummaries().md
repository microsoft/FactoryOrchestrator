### [Microsoft.FactoryOrchestrator.Client](Microsoft_FactoryOrchestrator_Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.GetTaskListSummaries() Method
Asynchronously Gets TaskList summaries for every "active" TaskList on the Service.  If "IsExecutingBootTasks()" returns true, this returns the "boot" TaskLists. Otherwise, it returns the "normal" TaskLists. The summary contains basic info about the TaskList.  
```csharp
public System.Threading.Tasks.Task<System.Collections.Generic.List<Microsoft.FactoryOrchestrator.Core.TaskListSummary>> GetTaskListSummaries();
```
#### Returns
[System.Threading.Tasks.Task&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task')[System.Collections.Generic.List&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')[TaskListSummary](./../CoreLibrary/TaskListSummary.md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task')  
A list of TaskListSummary objects.