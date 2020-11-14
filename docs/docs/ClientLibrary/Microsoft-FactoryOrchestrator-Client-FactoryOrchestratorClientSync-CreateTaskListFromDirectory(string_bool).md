#### [FactoryOrchestratorClientLibrary](./FactoryOrchestratorClientLibrary.md 'FactoryOrchestratorClientLibrary')
### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClientSync](./Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClientSync.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClientSync')
## FactoryOrchestratorClientSync.CreateTaskListFromDirectory(string, bool) Method
Creates a new TaskList by finding all .exe, .cmd, .bat, .ps1, and TAEF files in a given folder.  
```csharp
public Microsoft.FactoryOrchestrator.Core.TaskList CreateTaskListFromDirectory(string path, bool recursive=false);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClientSync-CreateTaskListFromDirectory(string_bool)-path'></a>
`path` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Path of the directory to search.  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClientSync-CreateTaskListFromDirectory(string_bool)-recursive'></a>
`recursive` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, search recursively.  
  
#### Returns
[Microsoft.FactoryOrchestrator.Core.TaskList](https://docs.microsoft.com/en-us/dotnet/api/Microsoft.FactoryOrchestrator.Core.TaskList 'Microsoft.FactoryOrchestrator.Core.TaskList')  
The created TaskList  
