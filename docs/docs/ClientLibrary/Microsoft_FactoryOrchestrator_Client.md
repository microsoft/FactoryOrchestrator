## Microsoft.FactoryOrchestrator.Client Namespace

| Classes | |
| :--- | :--- |
| [FactoryOrchestratorClient](FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient') | An asynchronous class for Factory Orchestrator .NET clients. Use instances of this class to communicate with Factory Orchestrator Service(s).<br/>WARNING: Use FactoryOrchestratorUWPClient for UWP clients or your UWP app will crash!<br/> |
| [FactoryOrchestratorConnectionException](FactoryOrchestratorConnectionException.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorConnectionException') | A FactoryOrchestratorConnectionException describes a Factory Orchestrator Client-Service connection issue.<br/> |
| [FactoryOrchestratorVersionMismatchException](FactoryOrchestratorVersionMismatchException.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorVersionMismatchException') | A FactoryOrchestratorVersionMismatchException is thrown if the Major versions of the Client and Service are incompatable.<br/> |
| [ServerPoller](ServerPoller.md 'Microsoft.FactoryOrchestrator.Client.ServerPoller') | Factory Ochestrator uses a polling model. ServerPoller is used to create a polling thread for a given Factory Ochestrator GUID. It can optionally raise a ServerPollerEvent event via OnUpdatedObject.<br/>All Factory Orchestrator GUID types are supported.<br/> |
| [ServerPollerEventArgs](ServerPollerEventArgs.md 'Microsoft.FactoryOrchestrator.Client.ServerPollerEventArgs') | Class used to share the new object with the callee via OnUpdatedObject. <br/> |
| [ServerPollerExceptionHandlerEventArgs](ServerPollerExceptionHandlerEventArgs.md 'Microsoft.FactoryOrchestrator.Client.ServerPollerExceptionHandlerEventArgs') | Class containing the exception thrown from the latest poll operation.<br/> |

| Delegates | |
| :--- | :--- |
| [IPCClientOnConnected()](IPCClientOnConnected().md 'Microsoft.FactoryOrchestrator.Client.IPCClientOnConnected()') | Signature for event handlers.<br/> |
| [ServerPollerEventHandler(object, ServerPollerEventArgs)](ServerPollerEventHandler(object_ServerPollerEventArgs).md 'Microsoft.FactoryOrchestrator.Client.ServerPollerEventHandler(object, Microsoft.FactoryOrchestrator.Client.ServerPollerEventArgs)') | Event handler delegate for a when a new object has been retrieved from the Server.<br/> |
| [ServerPollerExceptionHandler(object, ServerPollerExceptionHandlerEventArgs)](ServerPollerExceptionHandler(object_ServerPollerExceptionHandlerEventArgs).md 'Microsoft.FactoryOrchestrator.Client.ServerPollerExceptionHandler(object, Microsoft.FactoryOrchestrator.Client.ServerPollerExceptionHandlerEventArgs)') | Event handler delegate for a when the poller hit an exception while polling.<br/> |
