#### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](./Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.GetFile(string, long, int, bool) Method
Gets all the data in a file on the Service's computer. It is recommended you use FactoryOrchestratorClient::GetFileFromDevice instead.  
```csharp
byte[] GetFile(string sourceFilename, long offset=-1L, int count=0, bool getFromContainer=false);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService-GetFile(string_long_int_bool)-sourceFilename'></a>
`sourceFilename` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
The path to the file to retrieve.  
  
<a name='Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService-GetFile(string_long_int_bool)-offset'></a>
`offset` [System.Int64](https://docs.microsoft.com/en-us/dotnet/api/System.Int64 'System.Int64')  
If -1, read the whole file. Otherwise the starting byte to read the file from.  
  
<a name='Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService-GetFile(string_long_int_bool)-count'></a>
`count` [System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')  
If offset is -1 this is ignored. Otherwise, the number of bytes to read from the file.  
  
<a name='Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService-GetFile(string_long_int_bool)-getFromContainer'></a>
`getFromContainer` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, get the file from the container running on the connected device.  
  
#### Returns
[System.Byte](https://docs.microsoft.com/en-us/dotnet/api/System.Byte 'System.Byte')[[]](https://docs.microsoft.com/en-us/dotnet/api/System.Array 'System.Array')  
The bytes in the file.  
