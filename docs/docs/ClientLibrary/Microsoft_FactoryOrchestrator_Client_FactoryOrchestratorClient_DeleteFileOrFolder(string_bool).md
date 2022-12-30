### [Microsoft.FactoryOrchestrator.Client](Microsoft_FactoryOrchestrator_Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.DeleteFileOrFolder(string, bool) Method
Asynchronously Permanently deletes a file or folder. If a folder, all contents are deleted.  
```csharp
public System.Threading.Tasks.Task DeleteFileOrFolder(string path, bool deleteInContainer=false);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_DeleteFileOrFolder(string_bool)_path'></a>
`path` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
File or folder to delete
  
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_DeleteFileOrFolder(string_bool)_deleteInContainer'></a>
`deleteInContainer` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, delete the file from the container running on the connected device.
  
#### Returns
[System.Threading.Tasks.Task](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task 'System.Threading.Tasks.Task')  
