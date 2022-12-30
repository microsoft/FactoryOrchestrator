### [Microsoft.FactoryOrchestrator.Client](Microsoft_FactoryOrchestrator_Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.TerminateApp(string) Method
Asynchronously Exits a UWP app. Requires Windows Device Portal.  
```csharp
public System.Threading.Tasks.Task TerminateApp(string aumid);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_TerminateApp(string)_aumid'></a>
`aumid` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
The Application User Model ID (AUMID) of the app to exit.
  
#### Returns
[System.Threading.Tasks.Task](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task 'System.Threading.Tasks.Task')  
