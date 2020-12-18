#### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client')
### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](./Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.SendFile(string, byte[], bool, bool) Method
Asynchronously Saves data to a file to the Service's computer. It is recommended you use FactoryOrchestratorClient::SendFileToDevice instead.  
```csharp
public System.Threading.Tasks.Task SendFile(string targetFilename, byte[] fileData, bool appendFile=false, bool sendToContainer=false);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-SendFile(string_byte--_bool_bool)-targetFilename'></a>
`targetFilename` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
The name of the file you want created on the Service's computer.  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-SendFile(string_byte--_bool_bool)-fileData'></a>
`fileData` [System.Byte](https://docs.microsoft.com/en-us/dotnet/api/System.Byte 'System.Byte')[[]](https://docs.microsoft.com/en-us/dotnet/api/System.Array 'System.Array')  
The bytes you want saved to that file.  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-SendFile(string_byte--_bool_bool)-appendFile'></a>
`appendFile` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, the file is appended to instead of overwritten.  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-SendFile(string_byte--_bool_bool)-sendToContainer'></a>
`sendToContainer` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, send the file to the container running on the connected device.  
  
#### Returns
[System.Threading.Tasks.Task](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task 'System.Threading.Tasks.Task')  
true if the file was sucessfully created.  
