#### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](./Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.MoveFileOrFolder(string, string, bool) Method
Moves a file or folder to a new location.  
```csharp
void MoveFileOrFolder(string sourcePath, string destinationPath, bool moveInContainer=false);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService-MoveFileOrFolder(string_string_bool)-sourcePath'></a>
`sourcePath` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
File or folder to move  
  
<a name='Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService-MoveFileOrFolder(string_string_bool)-destinationPath'></a>
`destinationPath` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Destination path  
  
<a name='Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService-MoveFileOrFolder(string_string_bool)-moveInContainer'></a>
`moveInContainer` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, move the file from the container running on the connected device.  
  
