### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.EnumerateFiles(string, bool, bool) Method
Returns a list of all files in a given folder.  
```csharp
System.Collections.Generic.List<string> EnumerateFiles(string path, bool recursive=false, bool inContainer=false);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService_EnumerateFiles(string_bool_bool)_path'></a>
`path` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
The folder to search.
  
<a name='Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService_EnumerateFiles(string_bool_bool)_recursive'></a>
`recursive` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, search recursively.
  
<a name='Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService_EnumerateFiles(string_bool_bool)_inContainer'></a>
`inContainer` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, look for files in the container running on the connected device.
  
#### Returns
[System.Collections.Generic.List&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')[System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')  
