### [Microsoft.FactoryOrchestrator.Client](Microsoft_FactoryOrchestrator_Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.SendFileToDevice(string, string, bool) Method
Copies a file from the client to the device running Factory Orchestrator Service. Creates directories if needed.  
```csharp
public System.Threading.Tasks.Task<long> SendFileToDevice(string clientFilename, string serverFilename, bool sendToContainer=false);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_SendFileToDevice(string_string_bool)_clientFilename'></a>
`clientFilename` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Path on client PC to the file to copy.
  
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_SendFileToDevice(string_string_bool)_serverFilename'></a>
`serverFilename` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Path on device running Factory Orchestrator Service where the file will be saved.
  
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_SendFileToDevice(string_string_bool)_sendToContainer'></a>
`sendToContainer` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, send the file to the container running on the connected device.
  
#### Returns
[System.Threading.Tasks.Task&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task')[System.Int64](https://docs.microsoft.com/en-us/dotnet/api/System.Int64 'System.Int64')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task')  
