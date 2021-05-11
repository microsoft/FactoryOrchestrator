#### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client')
### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](./Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.DiscoverFactoryOrchestratorDevices(int, string, string) Method
Uses DNS-SD to find all Factory Orchestrator services on your local network.  
```csharp
public static System.Collections.Generic.List<Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient> DiscoverFactoryOrchestratorDevices(int secondsToWait=5, string serverIdentity="FactoryServer", string certhash="E8BF0011168803E6F4AF15C9AFE8C9C12F368C8F");
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-DiscoverFactoryOrchestratorDevices(int_string_string)-secondsToWait'></a>
`secondsToWait` [System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')  
Number of seconds to wait for services to respond  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-DiscoverFactoryOrchestratorDevices(int_string_string)-serverIdentity'></a>
`serverIdentity` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
The service certificate identity to use  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-DiscoverFactoryOrchestratorDevices(int_string_string)-certhash'></a>
`certhash` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
The service certificate hash to use  
  
#### Returns
[System.Collections.Generic.List&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')[FactoryOrchestratorClient](./Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')  
List of FactoryOrchestratorClient representing all discovered clients  
