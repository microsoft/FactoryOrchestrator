#### [FactoryOrchestratorCoreLibrary](./FactoryOrchestratorCoreLibrary.md 'FactoryOrchestratorCoreLibrary')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core').[WDPHelpers](./Microsoft-FactoryOrchestrator-Core-WDPHelpers.md 'Microsoft.FactoryOrchestrator.Core.WDPHelpers')
## WDPHelpers.CreateAppInstallEndpointAndBoundaryString(string, string, System.Uri, string) Method
Builds the application installation Uri and generates a unique boundary string for the multipart form data.  
```csharp
private static void CreateAppInstallEndpointAndBoundaryString(string packageName, string ipAddress, out System.Uri uri, out string boundaryString);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Core-WDPHelpers-CreateAppInstallEndpointAndBoundaryString(string_string_System-Uri_string)-packageName'></a>
`packageName` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
The name of the application package.  
  
<a name='Microsoft-FactoryOrchestrator-Core-WDPHelpers-CreateAppInstallEndpointAndBoundaryString(string_string_System-Uri_string)-ipAddress'></a>
`ipAddress` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
The ip address of the device to install the app on  
  
<a name='Microsoft-FactoryOrchestrator-Core-WDPHelpers-CreateAppInstallEndpointAndBoundaryString(string_string_System-Uri_string)-uri'></a>
`uri` [System.Uri](https://docs.microsoft.com/en-us/dotnet/api/System.Uri 'System.Uri')  
The endpoint for the install request.  
  
<a name='Microsoft-FactoryOrchestrator-Core-WDPHelpers-CreateAppInstallEndpointAndBoundaryString(string_string_System-Uri_string)-boundaryString'></a>
`boundaryString` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Unique string used to separate the parts of the multipart form data.  
  
