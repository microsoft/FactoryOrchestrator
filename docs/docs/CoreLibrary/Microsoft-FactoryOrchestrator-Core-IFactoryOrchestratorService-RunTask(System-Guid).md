#### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](./Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.RunTask(System.Guid) Method
Runs a Task outside of a TaskList.  
```csharp
Microsoft.FactoryOrchestrator.Core.TaskRun RunTask(System.Guid taskGuid);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService-RunTask(System-Guid)-taskGuid'></a>
`taskGuid` [System.Guid](https://docs.microsoft.com/en-us/dotnet/api/System.Guid 'System.Guid')  
The GUID of the Task to run.  
  
#### Returns
[TaskRun](./Microsoft-FactoryOrchestrator-Core-TaskRun.md 'Microsoft.FactoryOrchestrator.Core.TaskRun')  
The TaskRun associated with the run.  
