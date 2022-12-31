### [Microsoft.FactoryOrchestrator.Client](Microsoft_FactoryOrchestrator_Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.FactoryOrchestratorClient(IPAddress, int, string, string, X509Certificate2) Constructor
Creates a new FactoryOrchestratorClient instance. WARNING: Use FactoryOrchestratorUWPClient for UWP clients or your UWP app will crash!  
```csharp
public FactoryOrchestratorClient(System.Net.IPAddress host, int port=45684, string serverIdentity="FactoryServer", string certhash="6A3997F2957570A4E5D28D89F3EB4FA3A4E6AF9D", System.Security.Cryptography.X509Certificates.X509Certificate2 clientCertificate=null);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_FactoryOrchestratorClient(System_Net_IPAddress_int_string_string_System_Security_Cryptography_X509Certificates_X509Certificate2)_host'></a>
`host` [System.Net.IPAddress](https://docs.microsoft.com/en-us/dotnet/api/System.Net.IPAddress 'System.Net.IPAddress')  
IP address of the device running Factory Orchestrator Service. Use IPAddress.Loopback for local device.
  
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_FactoryOrchestratorClient(System_Net_IPAddress_int_string_string_System_Security_Cryptography_X509Certificates_X509Certificate2)_port'></a>
`port` [System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')  
Port to use. Factory Orchestrator Service defaults to [DefaultServerPort](./../CoreLibrary/Constants_DefaultServerPort.md 'Microsoft.FactoryOrchestrator.Core.Constants.DefaultServerPort').
  
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_FactoryOrchestratorClient(System_Net_IPAddress_int_string_string_System_Security_Cryptography_X509Certificates_X509Certificate2)_serverIdentity'></a>
`serverIdentity` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Distinguished name (CN) for the server, defaults to [DefaultServerIdentity](./../CoreLibrary/Constants_DefaultServerIdentity.md 'Microsoft.FactoryOrchestrator.Core.Constants.DefaultServerIdentity').
  
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_FactoryOrchestratorClient(System_Net_IPAddress_int_string_string_System_Security_Cryptography_X509Certificates_X509Certificate2)_certhash'></a>
`certhash` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Hash value (Certificate thumbprint) for the server certificate, defaults to [DefaultServerCertificateHash](./../CoreLibrary/Constants_DefaultServerCertificateHash.md 'Microsoft.FactoryOrchestrator.Core.Constants.DefaultServerCertificateHash')
  
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_FactoryOrchestratorClient(System_Net_IPAddress_int_string_string_System_Security_Cryptography_X509Certificates_X509Certificate2)_clientCertificate'></a>
`clientCertificate` [System.Security.Cryptography.X509Certificates.X509Certificate2](https://docs.microsoft.com/en-us/dotnet/api/System.Security.Cryptography.X509Certificates.X509Certificate2 'System.Security.Cryptography.X509Certificates.X509Certificate2')  
X509Certificate to send to the Factory Orchestrator Service for client authentication. Not required by all Factory Orchestrator Service configurations.
  
