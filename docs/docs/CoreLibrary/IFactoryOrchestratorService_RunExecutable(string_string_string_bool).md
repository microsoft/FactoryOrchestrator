### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.RunExecutable(string, string, string, bool) Method
Runs an executable (.exe) outside of a Task/TaskList.  
```csharp
Microsoft.FactoryOrchestrator.Core.TaskRun RunExecutable(string exeFilePath, string arguments, string logFilePath=null, bool runInContainer=false);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService_RunExecutable(string_string_string_bool)_exeFilePath'></a>
`exeFilePath` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Full path to the .exe file
  
<a name='Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService_RunExecutable(string_string_string_bool)_arguments'></a>
`arguments` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Arguments to pass to the .exe
  
<a name='Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService_RunExecutable(string_string_string_bool)_logFilePath'></a>
`logFilePath` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Optional log file to save the console output to.
  
<a name='Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService_RunExecutable(string_string_string_bool)_runInContainer'></a>
`runInContainer` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, run the executable in the container of the connected device.
  
#### Returns
[TaskRun](TaskRun.md 'Microsoft.FactoryOrchestrator.Core.TaskRun')  
The TaskRun associated with the .exe
