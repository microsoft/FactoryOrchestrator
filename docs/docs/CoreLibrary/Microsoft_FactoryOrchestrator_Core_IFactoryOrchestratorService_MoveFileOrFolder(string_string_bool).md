### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.MoveFileOrFolder(string, string, bool) Method
Moves a file or folder to a new location.  
```csharp
void MoveFileOrFolder(string sourcePath, string destinationPath, bool moveInContainer=false);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService_MoveFileOrFolder(string_string_bool)_sourcePath'></a>
`sourcePath` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
File or folder to move
  
<a name='Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService_MoveFileOrFolder(string_string_bool)_destinationPath'></a>
`destinationPath` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Destination path
  
<a name='Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService_MoveFileOrFolder(string_string_bool)_moveInContainer'></a>
`moveInContainer` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, move the file from the container running on the connected device.
  
