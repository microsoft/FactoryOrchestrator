### [Microsoft.FactoryOrchestrator.Client](Microsoft_FactoryOrchestrator_Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.SetLogFolder(string, bool) Method
Asynchronously Sets the log folder path used by Factory Orchestrator.  
```csharp
public System.Threading.Tasks.Task SetLogFolder(string path, bool moveExistingLogs);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_SetLogFolder(string_bool)_path'></a>
`path` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Path to the desired folder.
  
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_SetLogFolder(string_bool)_moveExistingLogs'></a>
`moveExistingLogs` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, existing logs are moved to the new location.
  
#### Returns
[System.Threading.Tasks.Task](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task 'System.Threading.Tasks.Task')  
