#### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](./Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.EnumerateDirectories(string, bool, bool) Method
Returns a list of all directories in a given folder.  
```csharp
System.Collections.Generic.List<string> EnumerateDirectories(string path, bool recursive=false, bool inContainer=false);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService-EnumerateDirectories(string_bool_bool)-path'></a>
`path` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
The folder to search.  
  
<a name='Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService-EnumerateDirectories(string_bool_bool)-recursive'></a>
`recursive` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, search recursively.  
  
<a name='Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService-EnumerateDirectories(string_bool_bool)-inContainer'></a>
`inContainer` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, look for directories in the container running on the connected device.  
  
#### Returns
[System.Collections.Generic.List&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')[System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')  
  
