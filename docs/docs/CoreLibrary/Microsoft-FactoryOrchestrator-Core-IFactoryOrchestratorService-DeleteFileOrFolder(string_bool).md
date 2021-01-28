#### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](./Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.DeleteFileOrFolder(string, bool) Method
Permanently deletes a file or folder. If a folder, all contents are deleted.  
```csharp
void DeleteFileOrFolder(string path, bool deleteInContainer=false);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService-DeleteFileOrFolder(string_bool)-path'></a>
`path` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
File or folder to delete  
  
<a name='Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService-DeleteFileOrFolder(string_bool)-deleteInContainer'></a>
`deleteInContainer` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, delete the file from the container running on the connected device.  
  
