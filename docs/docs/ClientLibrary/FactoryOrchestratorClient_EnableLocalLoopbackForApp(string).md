### [Microsoft.FactoryOrchestrator.Client](Microsoft_FactoryOrchestrator_Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.EnableLocalLoopbackForApp(string) Method
Asynchronously Enables local loopback on the given UWP app. Local loopback is enabled permanently for this app, persisting through reboots.  
```csharp
public System.Threading.Tasks.Task EnableLocalLoopbackForApp(string aumid);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_EnableLocalLoopbackForApp(string)_aumid'></a>
`aumid` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
The Application User Model ID (AUMID) of the app to enable local loopback on.
  
#### Returns
[System.Threading.Tasks.Task](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task 'System.Threading.Tasks.Task')  
