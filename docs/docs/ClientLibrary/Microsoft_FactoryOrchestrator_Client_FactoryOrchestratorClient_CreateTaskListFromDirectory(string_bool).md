### [Microsoft.FactoryOrchestrator.Client](Microsoft_FactoryOrchestrator_Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.CreateTaskListFromDirectory(string, bool) Method
Asynchronously Creates a new TaskList by finding all .exe, .cmd, .bat, .ps1, and TAEF files in a given folder.  
```csharp
public System.Threading.Tasks.Task<Microsoft.FactoryOrchestrator.Core.TaskList> CreateTaskListFromDirectory(string path, bool recursive=false);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_CreateTaskListFromDirectory(string_bool)_path'></a>
`path` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Path of the directory to search.
  
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_CreateTaskListFromDirectory(string_bool)_recursive'></a>
`recursive` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, search recursively.
  
#### Returns
[System.Threading.Tasks.Task&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task')[TaskList](./../CoreLibrary/Microsoft_FactoryOrchestrator_Core_TaskList.md 'Microsoft.FactoryOrchestrator.Core.TaskList')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task')  
The created TaskList
