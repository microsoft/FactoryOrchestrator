#### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client')
### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](./Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.CreateTaskListFromDirectory(string, bool) Method
Asynchronously Creates a new TaskList by finding all .exe, .cmd, .bat, .ps1, and TAEF files in a given folder.  
```csharp
public System.Threading.Tasks.Task<Microsoft.FactoryOrchestrator.Core.TaskList> CreateTaskListFromDirectory(string path, bool recursive=false);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-CreateTaskListFromDirectory(string_bool)-path'></a>
`path` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Path of the directory to search.  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-CreateTaskListFromDirectory(string_bool)-recursive'></a>
`recursive` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, search recursively.  
  
#### Returns
[System.Threading.Tasks.Task&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task')[Microsoft.FactoryOrchestrator.Core.TaskList](./../../CoreLibrary/Microsoft-FactoryOrchestrator-Core-TaskList 'Microsoft.FactoryOrchestrator.Core.TaskList')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task')  
The created TaskList  
