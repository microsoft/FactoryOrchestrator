### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.EnableLocalLoopbackForApp(string) Method
Enables local loopback on the given UWP app. Local loopback is enabled permanently for this app, persisting through reboots.  
```csharp
void EnableLocalLoopbackForApp(string aumid);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService_EnableLocalLoopbackForApp(string)_aumid'></a>
`aumid` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
The Application User Model ID (AUMID) of the app to enable local loopback on.
  
