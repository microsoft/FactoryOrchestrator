### [Microsoft.FactoryOrchestrator.Client](Microsoft_FactoryOrchestrator_Client.md 'Microsoft.FactoryOrchestrator.Client')
## ServerPoller Class
Factory Ochestrator uses a polling model. ServerPoller is used to create a polling thread for a given Factory Ochestrator GUID. It can optionally raise a ServerPollerEvent event via OnUpdatedObject.  
All Factory Orchestrator GUID types are supported.  
```csharp
public class ServerPoller :
System.IDisposable
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; ServerPoller  

Implements [System.IDisposable](https://docs.microsoft.com/en-us/dotnet/api/System.IDisposable 'System.IDisposable')  

| Constructors | |
| :--- | :--- |
| [ServerPoller(Nullable&lt;Guid&gt;, Type, int, bool, int)](ServerPoller_ServerPoller(Nullable_Guid__Type_int_bool_int).md 'Microsoft.FactoryOrchestrator.Client.ServerPoller.ServerPoller(System.Nullable&lt;System.Guid&gt;, System.Type, int, bool, int)') | Create a new ServerPoller. The ServerPoller is associated with a specific FactoryOrchestratorClient and object you want to poll. The desired object is referred to by its GUID. The GUID can be NULL for TaskRun polling.<br/>If it is NULL and the guidType is TaskList, a List of TaskListSummary objects is returned.<br/> |

| Properties | |
| :--- | :--- |
| [IsPolling](ServerPoller_IsPolling.md 'Microsoft.FactoryOrchestrator.Client.ServerPoller.IsPolling') | If true, the poller is actively polling for updates.<br/> |
| [LatestObject](ServerPoller_LatestObject.md 'Microsoft.FactoryOrchestrator.Client.ServerPoller.LatestObject') | Returns the latest object retrieved from the server.<br/> |
| [OnlyRaiseOnExceptionEventForConnectionException](ServerPoller_OnlyRaiseOnExceptionEventForConnectionException.md 'Microsoft.FactoryOrchestrator.Client.ServerPoller.OnlyRaiseOnExceptionEventForConnectionException') | If true, OnException only raised when the exception is a FactoryOrchestratorConnectionException.<br/>Other exceptions are ignored!<br/> |
| [PollingGuid](ServerPoller_PollingGuid.md 'Microsoft.FactoryOrchestrator.Client.ServerPoller.PollingGuid') | The GUID of the object you are polling. Can be NULL for some scenarios.<br/> |

| Methods | |
| :--- | :--- |
| [Dispose()](ServerPoller_Dispose().md 'Microsoft.FactoryOrchestrator.Client.ServerPoller.Dispose()') | Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.<br/> |
| [Dispose(bool)](ServerPoller_Dispose(bool).md 'Microsoft.FactoryOrchestrator.Client.ServerPoller.Dispose(bool)') | Releases unmanaged and - optionally - managed resources.<br/> |
| [StartPolling(FactoryOrchestratorClient)](ServerPoller_StartPolling(FactoryOrchestratorClient).md 'Microsoft.FactoryOrchestrator.Client.ServerPoller.StartPolling(Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient)') | Starts polling the object.<br/> |
| [StopPolling()](ServerPoller_StopPolling().md 'Microsoft.FactoryOrchestrator.Client.ServerPoller.StopPolling()') | Stops polling the object.<br/> |

| Events | |
| :--- | :--- |
| [OnException](ServerPoller_OnException.md 'Microsoft.FactoryOrchestrator.Client.ServerPoller.OnException') | Event raised when a poll attempt throws an exception.<br/> |
| [OnUpdatedObject](ServerPoller_OnUpdatedObject.md 'Microsoft.FactoryOrchestrator.Client.ServerPoller.OnUpdatedObject') | Event raised when a new object is received. It is only thrown if the object has changed since last polled.<br/> |
