### [Microsoft.FactoryOrchestrator.Client](Microsoft_FactoryOrchestrator_Client.md 'Microsoft.FactoryOrchestrator.Client')
## ServerPollerExceptionHandler(object, ServerPollerExceptionHandlerEventArgs) Delegate
Event handler delegate for a when the poller hit an exception while polling.  
```csharp
public delegate void ServerPollerExceptionHandler(object source, Microsoft.FactoryOrchestrator.Client.ServerPollerExceptionHandlerEventArgs e);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Client_ServerPollerExceptionHandler(object_Microsoft_FactoryOrchestrator_Client_ServerPollerExceptionHandlerEventArgs)_source'></a>
`source` [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object')  
The ServerPoller that retrieved the object.
  
<a name='Microsoft_FactoryOrchestrator_Client_ServerPollerExceptionHandler(object_Microsoft_FactoryOrchestrator_Client_ServerPollerExceptionHandlerEventArgs)_e'></a>
`e` [ServerPollerExceptionHandlerEventArgs](Microsoft_FactoryOrchestrator_Client_ServerPollerExceptionHandlerEventArgs.md 'Microsoft.FactoryOrchestrator.Client.ServerPollerExceptionHandlerEventArgs')  
The exception from the latest poll operation.
  
