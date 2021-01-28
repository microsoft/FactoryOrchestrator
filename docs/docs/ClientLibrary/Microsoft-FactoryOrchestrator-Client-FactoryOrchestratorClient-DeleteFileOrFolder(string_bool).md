#### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client')
### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](./Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.DeleteFileOrFolder(string, bool) Method
Asynchronously Permanently deletes a file or folder. If a folder, all contents are deleted.  
```csharp
public System.Threading.Tasks.Task DeleteFileOrFolder(string path, bool deleteInContainer=false);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-DeleteFileOrFolder(string_bool)-path'></a>
`path` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
File or folder to delete  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-DeleteFileOrFolder(string_bool)-deleteInContainer'></a>
`deleteInContainer` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, delete the file from the container running on the connected device.  
  
#### Returns
[System.Threading.Tasks.Task](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task 'System.Threading.Tasks.Task')  
