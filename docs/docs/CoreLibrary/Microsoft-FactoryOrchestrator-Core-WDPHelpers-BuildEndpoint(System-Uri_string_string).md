#### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core').[WDPHelpers](./Microsoft-FactoryOrchestrator-Core-WDPHelpers.md 'Microsoft.FactoryOrchestrator.Core.WDPHelpers')
## WDPHelpers.BuildEndpoint(System.Uri, string, string) Method
Constructs a fully formed REST API endpoint uri.  
```csharp
private static System.Uri BuildEndpoint(System.Uri baseUri, string path, string payload=null);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Core-WDPHelpers-BuildEndpoint(System-Uri_string_string)-baseUri'></a>
`baseUri` [System.Uri](https://docs.microsoft.com/en-us/dotnet/api/System.Uri 'System.Uri')  
The base uri (typically, just scheme and authority).  
  
<a name='Microsoft-FactoryOrchestrator-Core-WDPHelpers-BuildEndpoint(System-Uri_string_string)-path'></a>
`path` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
The path to the REST API method (ex: api/control/restart).  
  
<a name='Microsoft-FactoryOrchestrator-Core-WDPHelpers-BuildEndpoint(System-Uri_string_string)-payload'></a>
`payload` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Parameterized data required by the REST API.  
  
#### Returns
[System.Uri](https://docs.microsoft.com/en-us/dotnet/api/System.Uri 'System.Uri')  
Uri object containing the complete path and query string required to issue the REST API call.  
