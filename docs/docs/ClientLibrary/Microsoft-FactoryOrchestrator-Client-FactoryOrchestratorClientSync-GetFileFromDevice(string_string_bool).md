#### [FactoryOrchestratorClientLibrary](./FactoryOrchestratorClientLibrary.md 'FactoryOrchestratorClientLibrary')
### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClientSync](./Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClientSync.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClientSync')
## FactoryOrchestratorClientSync.GetFileFromDevice(string, string, bool) Method
Copies a file from the device running Factory Orchestrator Service to the client. Creates directories if needed.  
```csharp
public long GetFileFromDevice(string serverFilename, string clientFilename, bool getFromContainer=false);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClientSync-GetFileFromDevice(string_string_bool)-serverFilename'></a>
`serverFilename` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Path on running Factory Orchestrator Service to the file to copy.  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClientSync-GetFileFromDevice(string_string_bool)-clientFilename'></a>
`clientFilename` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Path on client PC where the file will be saved.  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClientSync-GetFileFromDevice(string_string_bool)-getFromContainer'></a>
`getFromContainer` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, get the file from the container running on the connected device.  
  
#### Returns
[System.Int64](https://docs.microsoft.com/en-us/dotnet/api/System.Int64 'System.Int64')  
