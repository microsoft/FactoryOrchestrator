#### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client')
### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](./Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient(System.Net.IPAddress, int, string, string) Constructor
Creates a new FactoryOrchestratorClient instance. WARNING: Use FactoryOrchestratorUWPClient for UWP clients or your UWP app will crash!  
```csharp
public FactoryOrchestratorClient(System.Net.IPAddress host, int port=45684, string serverIdentity="FactoryServer", string certhash="E8BF0011168803E6F4AF15C9AFE8C9C12F368C8F");
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-FactoryOrchestratorClient(System-Net-IPAddress_int_string_string)-host'></a>
`host` [System.Net.IPAddress](https://docs.microsoft.com/en-us/dotnet/api/System.Net.IPAddress 'System.Net.IPAddress')  
IP address of the device running Factory Orchestrator Service. Use IPAddress.Loopback for local device.  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-FactoryOrchestratorClient(System-Net-IPAddress_int_string_string)-port'></a>
`port` [System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')  
Port to use. Factory Orchestrator Service defaults to 45684.  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-FactoryOrchestratorClient(System-Net-IPAddress_int_string_string)-serverIdentity'></a>
`serverIdentity` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Distinguished name for the server defaults to FactoryServer.  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-FactoryOrchestratorClient(System-Net-IPAddress_int_string_string)-certhash'></a>
`certhash` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Hash value for the server certificate defaults to E8BF0011168803E6F4AF15C9AFE8C9C12F368C8F.  
  
