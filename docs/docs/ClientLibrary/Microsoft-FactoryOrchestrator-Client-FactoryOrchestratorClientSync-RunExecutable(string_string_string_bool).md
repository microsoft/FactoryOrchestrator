#### [FactoryOrchestratorClientLibrary](./FactoryOrchestratorClientLibrary.md 'FactoryOrchestratorClientLibrary')
### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClientSync](./Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClientSync.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClientSync')
## FactoryOrchestratorClientSync.RunExecutable(string, string, string, bool) Method
Runs an executable (.exe) outside of a Task/TaskList.  
```csharp
public Microsoft.FactoryOrchestrator.Core.TaskRun RunExecutable(string exeFilePath, string arguments, string logFilePath=null, bool runInContainer=false);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClientSync-RunExecutable(string_string_string_bool)-exeFilePath'></a>
`exeFilePath` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Full path to the .exe file  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClientSync-RunExecutable(string_string_string_bool)-arguments'></a>
`arguments` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Arguments to pass to the .exe  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClientSync-RunExecutable(string_string_string_bool)-logFilePath'></a>
`logFilePath` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Optional log file to save the console output to.  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClientSync-RunExecutable(string_string_string_bool)-runInContainer'></a>
`runInContainer` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, run the executable in the container of the connected device.  
  
#### Returns
[Microsoft.FactoryOrchestrator.Core.TaskRun](https://docs.microsoft.com/en-us/dotnet/api/Microsoft.FactoryOrchestrator.Core.TaskRun 'Microsoft.FactoryOrchestrator.Core.TaskRun')  
The TaskRun associated with the .exe  
