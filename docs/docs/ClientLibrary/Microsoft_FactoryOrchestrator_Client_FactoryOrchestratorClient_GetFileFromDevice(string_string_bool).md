### [Microsoft.FactoryOrchestrator.Client](Microsoft_FactoryOrchestrator_Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.GetFileFromDevice(string, string, bool) Method
Copies a file from the device running Factory Orchestrator Service to the client. Creates directories if needed.  
```csharp
public System.Threading.Tasks.Task<long> GetFileFromDevice(string serverFilename, string clientFilename, bool getFromContainer=false);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_GetFileFromDevice(string_string_bool)_serverFilename'></a>
`serverFilename` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Path on running Factory Orchestrator Service to the file to copy.
  
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_GetFileFromDevice(string_string_bool)_clientFilename'></a>
`clientFilename` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Path on client PC where the file will be saved.
  
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_GetFileFromDevice(string_string_bool)_getFromContainer'></a>
`getFromContainer` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, get the file from the container running on the connected device.
  
#### Returns
[System.Threading.Tasks.Task&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task')[System.Int64](https://docs.microsoft.com/en-us/dotnet/api/System.Int64 'System.Int64')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task')  
