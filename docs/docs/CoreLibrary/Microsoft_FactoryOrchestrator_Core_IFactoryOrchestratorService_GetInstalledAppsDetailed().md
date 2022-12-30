### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.GetInstalledAppsDetailed() Method
Gets all installed apps on the OS. Requires Windows Device Portal.  
```csharp
System.Collections.Generic.List<Microsoft.FactoryOrchestrator.Core.PackageInfo> GetInstalledAppsDetailed();
```
#### Returns
[System.Collections.Generic.List&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')[PackageInfo](Microsoft_FactoryOrchestrator_Core_PackageInfo.md 'Microsoft.FactoryOrchestrator.Core.PackageInfo')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')  
The list of apps and their information, in PackageInfo objects.
