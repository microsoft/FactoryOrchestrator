#### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](./Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.SendFile(string, byte[], bool, bool) Method
Saves data to a file to the Service's computer. It is recommended you use FactoryOrchestratorClient::SendFileToDevice instead.  
```csharp
void SendFile(string targetFilename, byte[] fileData, bool appendFile=false, bool sendToContainer=false);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService-SendFile(string_byte--_bool_bool)-targetFilename'></a>
`targetFilename` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
The name of the file you want created on the Service's computer.  
  
<a name='Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService-SendFile(string_byte--_bool_bool)-fileData'></a>
`fileData` [System.Byte](https://docs.microsoft.com/en-us/dotnet/api/System.Byte 'System.Byte')[[]](https://docs.microsoft.com/en-us/dotnet/api/System.Array 'System.Array')  
The bytes you want saved to that file.  
  
<a name='Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService-SendFile(string_byte--_bool_bool)-appendFile'></a>
`appendFile` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, the file is appended to instead of overwritten.  
  
<a name='Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService-SendFile(string_byte--_bool_bool)-sendToContainer'></a>
`sendToContainer` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, send the file to the container running on the connected device.  
  
