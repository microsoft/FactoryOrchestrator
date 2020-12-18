#### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](./Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.QueryTaskList(System.Guid) Method
Gets the TaskList object for a given TaskList GUID.  
```csharp
Microsoft.FactoryOrchestrator.Core.TaskList QueryTaskList(System.Guid taskListGuid);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService-QueryTaskList(System-Guid)-taskListGuid'></a>
`taskListGuid` [System.Guid](https://docs.microsoft.com/en-us/dotnet/api/System.Guid 'System.Guid')  
The TaskList GUID.  
  
#### Returns
[TaskList](./Microsoft-FactoryOrchestrator-Core-TaskList.md 'Microsoft.FactoryOrchestrator.Core.TaskList')  
The TaskList object with that GUID.  
