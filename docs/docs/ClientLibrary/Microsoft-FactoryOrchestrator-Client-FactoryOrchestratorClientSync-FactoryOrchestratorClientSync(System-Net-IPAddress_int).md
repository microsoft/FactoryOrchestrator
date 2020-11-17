#### [FactoryOrchestratorClientLibrary](./FactoryOrchestratorClientLibrary.md 'FactoryOrchestratorClientLibrary')
### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClientSync](./Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClientSync.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClientSync')
## FactoryOrchestratorClientSync(System.Net.IPAddress, int) Constructor
Creates a new FactoryOrchestratorSyncClient instance. Most .NET clients are recommended to use FactoryOrchestratorClient instead which is fully asynchronous.  
```csharp
public FactoryOrchestratorClientSync(System.Net.IPAddress host, int port=45684);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClientSync-FactoryOrchestratorClientSync(System-Net-IPAddress_int)-host'></a>
`host` [System.Net.IPAddress](https://docs.microsoft.com/en-us/dotnet/api/System.Net.IPAddress 'System.Net.IPAddress')  
IP address of the device running Factory Orchestrator Service. Use IPAddress.Loopback for local device.  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClientSync-FactoryOrchestratorClientSync(System-Net-IPAddress_int)-port'></a>
`port` [System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')  
Port to use. Factory Orchestrator Service defaults to 45684.  
  
