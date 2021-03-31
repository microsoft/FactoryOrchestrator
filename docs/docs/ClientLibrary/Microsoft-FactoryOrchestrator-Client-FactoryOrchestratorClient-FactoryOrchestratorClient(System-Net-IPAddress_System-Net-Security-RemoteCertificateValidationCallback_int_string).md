#### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client')
### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](./Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient(System.Net.IPAddress, System.Net.Security.RemoteCertificateValidationCallback, int, string) Constructor
Creates a new FactoryOrchestratorClient instance. WARNING: Use FactoryOrchestratorUWPClient for UWP clients or your UWP app will crash!  
```csharp
public FactoryOrchestratorClient(System.Net.IPAddress host, System.Net.Security.RemoteCertificateValidationCallback certificateValidationCallback, int port=45684, string serverIdentity="FactoryServer");
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-FactoryOrchestratorClient(System-Net-IPAddress_System-Net-Security-RemoteCertificateValidationCallback_int_string)-host'></a>
`host` [System.Net.IPAddress](https://docs.microsoft.com/en-us/dotnet/api/System.Net.IPAddress 'System.Net.IPAddress')  
IP address of the device running Factory Orchestrator Service. Use IPAddress.Loopback for local device.  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-FactoryOrchestratorClient(System-Net-IPAddress_System-Net-Security-RemoteCertificateValidationCallback_int_string)-certificateValidationCallback'></a>
`certificateValidationCallback` [System.Net.Security.RemoteCertificateValidationCallback](https://docs.microsoft.com/en-us/dotnet/api/System.Net.Security.RemoteCertificateValidationCallback 'System.Net.Security.RemoteCertificateValidationCallback')  
A System.Net.Security.RemoteCertificateValidationCallback delegate responsible for validating the server certificate.  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-FactoryOrchestratorClient(System-Net-IPAddress_System-Net-Security-RemoteCertificateValidationCallback_int_string)-port'></a>
`port` [System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')  
Port to use. Factory Orchestrator Service defaults to 45684.  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-FactoryOrchestratorClient(System-Net-IPAddress_System-Net-Security-RemoteCertificateValidationCallback_int_string)-serverIdentity'></a>
`serverIdentity` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Distinguished name for the server defaults to FactoryServer.  
  
