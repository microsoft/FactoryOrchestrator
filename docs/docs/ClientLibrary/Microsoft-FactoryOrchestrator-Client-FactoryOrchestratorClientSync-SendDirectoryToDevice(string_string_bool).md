#### [FactoryOrchestratorClientLibrary](./FactoryOrchestratorClientLibrary.md 'FactoryOrchestratorClientLibrary')
### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClientSync](./Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClientSync.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClientSync')
## FactoryOrchestratorClientSync.SendDirectoryToDevice(string, string, bool) Method
Copies a folder from the client to the device running Factory Orchestrator Service. Creates directories if needed.  
```csharp
public long SendDirectoryToDevice(string clientDirectory, string serverDirectory, bool sendToContainer=false);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClientSync-SendDirectoryToDevice(string_string_bool)-clientDirectory'></a>
`clientDirectory` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Path on client PC to the folder to copy.  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClientSync-SendDirectoryToDevice(string_string_bool)-serverDirectory'></a>
`serverDirectory` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Path on device running Factory Orchestrator Service where the folder will be saved.  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClientSync-SendDirectoryToDevice(string_string_bool)-sendToContainer'></a>
`sendToContainer` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, copy the folder to the container running on the connected device.  
  
#### Returns
[System.Int64](https://docs.microsoft.com/en-us/dotnet/api/System.Int64 'System.Int64')  
