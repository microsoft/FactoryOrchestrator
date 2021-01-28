#### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client')
### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client')
## ServerPollerEventHandler(object, Microsoft.FactoryOrchestrator.Client.ServerPollerEventArgs) Delegate
Event handler delegate for a when a new object has been retrieved from the Server.  
```csharp
public delegate void ServerPollerEventHandler(object source, Microsoft.FactoryOrchestrator.Client.ServerPollerEventArgs e);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Client-ServerPollerEventHandler(object_Microsoft-FactoryOrchestrator-Client-ServerPollerEventArgs)-source'></a>
`source` [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object')  
The ServerPoller that retrieved the object.  
  
<a name='Microsoft-FactoryOrchestrator-Client-ServerPollerEventHandler(object_Microsoft-FactoryOrchestrator-Client-ServerPollerEventArgs)-e'></a>
`e` [ServerPollerEventArgs](./Microsoft-FactoryOrchestrator-Client-ServerPollerEventArgs.md 'Microsoft.FactoryOrchestrator.Client.ServerPollerEventArgs')  
The result of the latest poll operation.  
  
