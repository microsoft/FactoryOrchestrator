#### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client')
### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](./Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.SendDirectoryToDevice(string, string, bool) Method
Copies a folder from the client to the device running Factory Orchestrator Service. Creates directories if needed.  
```csharp
public System.Threading.Tasks.Task<long> SendDirectoryToDevice(string clientDirectory, string serverDirectory, bool sendToContainer=false);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-SendDirectoryToDevice(string_string_bool)-clientDirectory'></a>
`clientDirectory` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Path on client PC to the folder to copy.  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-SendDirectoryToDevice(string_string_bool)-serverDirectory'></a>
`serverDirectory` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Path on device running Factory Orchestrator Service where the folder will be saved.  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-SendDirectoryToDevice(string_string_bool)-sendToContainer'></a>
`sendToContainer` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, copy the folder to the container running on the connected device.  
  
#### Returns
[System.Threading.Tasks.Task&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task')[System.Int64](https://docs.microsoft.com/en-us/dotnet/api/System.Int64 'System.Int64')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task')  
