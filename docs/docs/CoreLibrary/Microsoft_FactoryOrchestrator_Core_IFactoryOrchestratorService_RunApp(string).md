### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.RunApp(string) Method
Runs a UWP app outside of a Task/TaskList. Requires Windows Device Portal.  
```csharp
Microsoft.FactoryOrchestrator.Core.TaskRun RunApp(string aumid);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService_RunApp(string)_aumid'></a>
`aumid` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
The Application User Model ID (AUMID) of the app to run.
  
#### Returns
[TaskRun](Microsoft_FactoryOrchestrator_Core_TaskRun.md 'Microsoft.FactoryOrchestrator.Core.TaskRun')  
