#### [FactoryOrchestratorCoreLibrary](./FactoryOrchestratorCoreLibrary.md 'FactoryOrchestratorCoreLibrary')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core').[HttpMultipartFileContent](./Microsoft-FactoryOrchestrator-Core-HttpMultipartFileContent.md 'Microsoft.FactoryOrchestrator.Core.HttpMultipartFileContent')
## HttpMultipartFileContent.GetFileHeader(System.IO.FileInfo) Method
Gets the file header for the transfer  
```csharp
private static byte[] GetFileHeader(System.IO.FileInfo info);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Core-HttpMultipartFileContent-GetFileHeader(System-IO-FileInfo)-info'></a>
`info` [System.IO.FileInfo](https://docs.microsoft.com/en-us/dotnet/api/System.IO.FileInfo 'System.IO.FileInfo')  
Information about the file  
  
#### Returns
[System.Byte](https://docs.microsoft.com/en-us/dotnet/api/System.Byte 'System.Byte')[[]](https://docs.microsoft.com/en-us/dotnet/api/System.Array 'System.Array')  
A byte array with the file header information  
