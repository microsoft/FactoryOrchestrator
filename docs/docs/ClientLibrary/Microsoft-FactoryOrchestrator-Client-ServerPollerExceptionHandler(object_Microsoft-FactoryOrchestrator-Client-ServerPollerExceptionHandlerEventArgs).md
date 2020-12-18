#### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client')
### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client')
## ServerPollerExceptionHandler(object, Microsoft.FactoryOrchestrator.Client.ServerPollerExceptionHandlerEventArgs) Delegate
Event handler delegate for a when the poller hit an exception while polling.  
```csharp
public delegate void ServerPollerExceptionHandler(object source, Microsoft.FactoryOrchestrator.Client.ServerPollerExceptionHandlerEventArgs e);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Client-ServerPollerExceptionHandler(object_Microsoft-FactoryOrchestrator-Client-ServerPollerExceptionHandlerEventArgs)-source'></a>
`source` [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object')  
The ServerPoller that retrieved the object.  
  
<a name='Microsoft-FactoryOrchestrator-Client-ServerPollerExceptionHandler(object_Microsoft-FactoryOrchestrator-Client-ServerPollerExceptionHandlerEventArgs)-e'></a>
`e` [ServerPollerExceptionHandlerEventArgs](./Microsoft-FactoryOrchestrator-Client-ServerPollerExceptionHandlerEventArgs.md 'Microsoft.FactoryOrchestrator.Client.ServerPollerExceptionHandlerEventArgs')  
The exception from the latest poll operation.  
  
