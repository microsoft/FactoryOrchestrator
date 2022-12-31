### [Microsoft.FactoryOrchestrator.Client](Microsoft_FactoryOrchestrator_Client.md 'Microsoft.FactoryOrchestrator.Client')
## ServerPollerEventHandler(object, ServerPollerEventArgs) Delegate
Event handler delegate for a when a new object has been retrieved from the Server.  
```csharp
public delegate void ServerPollerEventHandler(object source, Microsoft.FactoryOrchestrator.Client.ServerPollerEventArgs e);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Client_ServerPollerEventHandler(object_Microsoft_FactoryOrchestrator_Client_ServerPollerEventArgs)_source'></a>
`source` [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object')  
The ServerPoller that retrieved the object.
  
<a name='Microsoft_FactoryOrchestrator_Client_ServerPollerEventHandler(object_Microsoft_FactoryOrchestrator_Client_ServerPollerEventArgs)_e'></a>
`e` [ServerPollerEventArgs](ServerPollerEventArgs.md 'Microsoft.FactoryOrchestrator.Client.ServerPollerEventArgs')  
The result of the latest poll operation.
  
