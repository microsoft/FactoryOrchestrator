#### [FactoryOrchestratorClientLibrary](./FactoryOrchestratorClientLibrary.md 'FactoryOrchestratorClientLibrary')
### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClientSync](./Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClientSync.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClientSync')
## FactoryOrchestratorClientSync.GetDirectoryFromDevice(string, string, bool) Method
Copies a folder from the device running Factory Orchestrator Service to the client. Creates directories if needed.  
```csharp
public long GetDirectoryFromDevice(string serverDirectory, string clientDirectory, bool getFromContainer=false);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClientSync-GetDirectoryFromDevice(string_string_bool)-serverDirectory'></a>
`serverDirectory` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Path on device running Factory Orchestrator Service to the folder to copy.  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClientSync-GetDirectoryFromDevice(string_string_bool)-clientDirectory'></a>
`clientDirectory` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Path on client PC where the folder will be saved.  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClientSync-GetDirectoryFromDevice(string_string_bool)-getFromContainer'></a>
`getFromContainer` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, get the file from the container running on the connected device.  
  
#### Returns
[System.Int64](https://docs.microsoft.com/en-us/dotnet/api/System.Int64 'System.Int64')  
