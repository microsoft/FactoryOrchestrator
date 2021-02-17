#### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](./Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.GetInstalledAppsDetailed() Method
Gets all installed apps on the OS. Requires Windows Device Portal.  
```csharp
System.Collections.Generic.List<Microsoft.FactoryOrchestrator.Core.PackageInfo> GetInstalledAppsDetailed();
```
#### Returns
[System.Collections.Generic.List&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')[PackageInfo](./Microsoft-FactoryOrchestrator-Core-PackageInfo.md 'Microsoft.FactoryOrchestrator.Core.PackageInfo')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')  
The list of apps and their information, in PackageInfo objects.  
