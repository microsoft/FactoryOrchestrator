#### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](./Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.RunApp(string) Method
Runs a UWP app outside of a Task/TaskList. Requires Windows Device Portal.  
```csharp
Microsoft.FactoryOrchestrator.Core.TaskRun RunApp(string aumid);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService-RunApp(string)-aumid'></a>
`aumid` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
The Application User Model ID (AUMID) of the app to run.  
  
#### Returns
[TaskRun](./Microsoft-FactoryOrchestrator-Core-TaskRun.md 'Microsoft.FactoryOrchestrator.Core.TaskRun')  
  
