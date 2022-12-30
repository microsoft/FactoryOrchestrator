### [Microsoft.FactoryOrchestrator.Client](Microsoft_FactoryOrchestrator_Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.SendDirectoryToDevice(string, string, bool) Method
Copies a folder from the client to the device running Factory Orchestrator Service. Creates directories if needed.  
```csharp
public System.Threading.Tasks.Task<long> SendDirectoryToDevice(string clientDirectory, string serverDirectory, bool sendToContainer=false);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_SendDirectoryToDevice(string_string_bool)_clientDirectory'></a>
`clientDirectory` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Path on client PC to the folder to copy.
  
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_SendDirectoryToDevice(string_string_bool)_serverDirectory'></a>
`serverDirectory` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Path on device running Factory Orchestrator Service where the folder will be saved.
  
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_SendDirectoryToDevice(string_string_bool)_sendToContainer'></a>
`sendToContainer` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, copy the folder to the container running on the connected device.
  
#### Returns
[System.Threading.Tasks.Task&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task')[System.Int64](https://docs.microsoft.com/en-us/dotnet/api/System.Int64 'System.Int64')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task')  
