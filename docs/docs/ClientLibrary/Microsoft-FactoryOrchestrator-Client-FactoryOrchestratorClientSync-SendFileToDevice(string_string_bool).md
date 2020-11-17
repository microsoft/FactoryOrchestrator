#### [FactoryOrchestratorClientLibrary](./FactoryOrchestratorClientLibrary.md 'FactoryOrchestratorClientLibrary')
### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClientSync](./Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClientSync.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClientSync')
## FactoryOrchestratorClientSync.SendFileToDevice(string, string, bool) Method
Copies a file from the client to the device running Factory Orchestrator Service. Creates directories if needed.  
```csharp
public long SendFileToDevice(string clientFilename, string serverFilename, bool sendToContainer=false);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClientSync-SendFileToDevice(string_string_bool)-clientFilename'></a>
`clientFilename` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Path on client PC to the file to copy.  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClientSync-SendFileToDevice(string_string_bool)-serverFilename'></a>
`serverFilename` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Path on device running Factory Orchestrator Service where the file will be saved.  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClientSync-SendFileToDevice(string_string_bool)-sendToContainer'></a>
`sendToContainer` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, send the file to the container running on the connected device.  
  
#### Returns
[System.Int64](https://docs.microsoft.com/en-us/dotnet/api/System.Int64 'System.Int64')  
