#### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client')
### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](./Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient(System.Net.IPAddress, int) Constructor
Creates a new FactoryOrchestratorClient instance. WARNING: Use FactoryOrchestratorUWPClient for UWP clients or your UWP app will crash!  
```csharp
public FactoryOrchestratorClient(System.Net.IPAddress host, int port=45684);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-FactoryOrchestratorClient(System-Net-IPAddress_int)-host'></a>
`host` [System.Net.IPAddress](https://docs.microsoft.com/en-us/dotnet/api/System.Net.IPAddress 'System.Net.IPAddress')  
IP address of the device running Factory Orchestrator Service. Use IPAddress.Loopback for local device.  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-FactoryOrchestratorClient(System-Net-IPAddress_int)-port'></a>
`port` [System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')  
Port to use. Factory Orchestrator Service defaults to 45684.  
  
