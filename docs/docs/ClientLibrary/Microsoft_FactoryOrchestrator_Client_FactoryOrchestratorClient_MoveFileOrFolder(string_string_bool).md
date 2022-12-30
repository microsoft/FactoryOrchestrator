### [Microsoft.FactoryOrchestrator.Client](Microsoft_FactoryOrchestrator_Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.MoveFileOrFolder(string, string, bool) Method
Asynchronously Moves a file or folder to a new location.  
```csharp
public System.Threading.Tasks.Task MoveFileOrFolder(string sourcePath, string destinationPath, bool moveInContainer=false);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_MoveFileOrFolder(string_string_bool)_sourcePath'></a>
`sourcePath` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
File or folder to move
  
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_MoveFileOrFolder(string_string_bool)_destinationPath'></a>
`destinationPath` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Destination path
  
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_MoveFileOrFolder(string_string_bool)_moveInContainer'></a>
`moveInContainer` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, move the file from the container running on the connected device.
  
#### Returns
[System.Threading.Tasks.Task](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task 'System.Threading.Tasks.Task')  
