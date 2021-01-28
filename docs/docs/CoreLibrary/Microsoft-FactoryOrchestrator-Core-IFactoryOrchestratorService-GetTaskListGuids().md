#### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](./Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.GetTaskListGuids() Method
Gets the GUID of every "active" TaskList on the Service. If "IsExecutingBootTasks()" returns true, this returns the "boot" TaskLists. Otherwise, it returns the "normal" TaskLists.  
```csharp
System.Collections.Generic.List<System.Guid> GetTaskListGuids();
```
#### Returns
[System.Collections.Generic.List&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')[System.Guid](https://docs.microsoft.com/en-us/dotnet/api/System.Guid 'System.Guid')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')  
The list of TaskList GUIDs.  
