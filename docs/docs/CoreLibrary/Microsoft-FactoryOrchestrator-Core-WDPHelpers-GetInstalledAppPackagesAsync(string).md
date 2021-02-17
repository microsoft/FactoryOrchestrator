#### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core').[WDPHelpers](./Microsoft-FactoryOrchestrator-Core-WDPHelpers.md 'Microsoft.FactoryOrchestrator.Core.WDPHelpers')
## WDPHelpers.GetInstalledAppPackagesAsync(string) Method
Gets the collection of applications installed on the device.  
```csharp
public static System.Threading.Tasks.Task<Microsoft.FactoryOrchestrator.Core.AppPackages> GetInstalledAppPackagesAsync(string ipAddress="localhost");
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Core-WDPHelpers-GetInstalledAppPackagesAsync(string)-ipAddress'></a>
`ipAddress` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
The ip address of the device to query.  
  
#### Returns
[System.Threading.Tasks.Task&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task')[AppPackages](./Microsoft-FactoryOrchestrator-Core-AppPackages.md 'Microsoft.FactoryOrchestrator.Core.AppPackages')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task')  
AppPackages object containing the list of installed application packages.  
