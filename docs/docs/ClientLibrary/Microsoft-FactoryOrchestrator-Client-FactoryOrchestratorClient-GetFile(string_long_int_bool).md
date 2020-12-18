#### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client')
### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](./Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.GetFile(string, long, int, bool) Method
Asynchronously Gets all the data in a file on the Service's computer. It is recommended you use FactoryOrchestratorClient::GetFileFromDevice instead.  
```csharp
public System.Threading.Tasks.Task<byte[]> GetFile(string sourceFilename, long offset=-1L, int count=0, bool getFromContainer=false);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-GetFile(string_long_int_bool)-sourceFilename'></a>
`sourceFilename` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
The path to the file to retrieve.  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-GetFile(string_long_int_bool)-offset'></a>
`offset` [System.Int64](https://docs.microsoft.com/en-us/dotnet/api/System.Int64 'System.Int64')  
If -1, read the whole file. Otherwise the starting byte to read the file from.  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-GetFile(string_long_int_bool)-count'></a>
`count` [System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')  
If offset is -1 this is ignored. Otherwise, the number of bytes to read from the file.  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-GetFile(string_long_int_bool)-getFromContainer'></a>
`getFromContainer` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, get the file from the container running on the connected device.  
  
#### Returns
[System.Threading.Tasks.Task&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task')[System.Byte](https://docs.microsoft.com/en-us/dotnet/api/System.Byte 'System.Byte')[[]](https://docs.microsoft.com/en-us/dotnet/api/System.Array 'System.Array')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task-1 'System.Threading.Tasks.Task')  
The bytes in the file.  
