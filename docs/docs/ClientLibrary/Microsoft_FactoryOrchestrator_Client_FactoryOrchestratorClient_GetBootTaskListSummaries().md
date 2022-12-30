### [Microsoft.FactoryOrchestrator.Client](Microsoft_FactoryOrchestrator_Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.GetBootTaskListSummaries() Method
Asynchronously Gets "boot" TaskList summaries for every "boot" TaskList on the Service. The summary contains basic info about the TaskList.  
```csharp
public System.Threading.Tasks.Task<System.Collections.Generic.List<Microsoft.FactoryOrchestrator.Core.TaskListSummary>> GetBootTaskListSummaries();
```
#### Returns
[System.Threading.Tasks.Task&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task')[System.Collections.Generic.List&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')[TaskListSummary](./../CoreLibrary/Microsoft_FactoryOrchestrator_Core_TaskListSummary.md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task')  
A list of TaskListSummary objects.
