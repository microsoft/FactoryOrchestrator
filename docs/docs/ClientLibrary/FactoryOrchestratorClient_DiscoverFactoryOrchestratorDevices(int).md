### [Microsoft.FactoryOrchestrator.Client](Microsoft_FactoryOrchestrator_Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.DiscoverFactoryOrchestratorDevices(int) Method
Uses DNS-SD to find all Factory Orchestrator services on your local network.  
```csharp
public static System.Collections.Generic.List<Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient> DiscoverFactoryOrchestratorDevices(int secondsToWait=5);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_DiscoverFactoryOrchestratorDevices(int)_secondsToWait'></a>
`secondsToWait` [System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')  
Number of seconds to wait for services to respond
  
#### Returns
[System.Collections.Generic.List&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')[FactoryOrchestratorClient](FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')  
