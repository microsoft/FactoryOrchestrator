### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.CreateTaskListFromDirectory(string, bool) Method
Creates a new TaskList by finding all .exe, .cmd, .bat, .ps1, and TAEF files in a given folder.  
```csharp
Microsoft.FactoryOrchestrator.Core.TaskList CreateTaskListFromDirectory(string path, bool recursive=false);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService_CreateTaskListFromDirectory(string_bool)_path'></a>
`path` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Path of the directory to search.
  
<a name='Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService_CreateTaskListFromDirectory(string_bool)_recursive'></a>
`recursive` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, search recursively.
  
#### Returns
[TaskList](Microsoft_FactoryOrchestrator_Core_TaskList.md 'Microsoft.FactoryOrchestrator.Core.TaskList')  
The created TaskList
