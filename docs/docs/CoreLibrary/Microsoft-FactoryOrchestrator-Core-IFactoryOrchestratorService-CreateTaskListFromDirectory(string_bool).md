#### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](./Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.CreateTaskListFromDirectory(string, bool) Method
Creates a new TaskList by finding all .exe, .cmd, .bat, .ps1, and TAEF files in a given folder.  
```csharp
Microsoft.FactoryOrchestrator.Core.TaskList CreateTaskListFromDirectory(string path, bool recursive=false);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService-CreateTaskListFromDirectory(string_bool)-path'></a>
`path` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Path of the directory to search.  
  
<a name='Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService-CreateTaskListFromDirectory(string_bool)-recursive'></a>
`recursive` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, search recursively.  
  
#### Returns
[TaskList](./Microsoft-FactoryOrchestrator-Core-TaskList.md 'Microsoft.FactoryOrchestrator.Core.TaskList')  
The created TaskList  
