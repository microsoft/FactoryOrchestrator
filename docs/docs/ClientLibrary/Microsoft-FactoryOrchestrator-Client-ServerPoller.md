#### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client')
### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client')
## ServerPoller Class
Factory Ochestrator uses a polling model. ServerPoller is used to create a polling thread for a given Factory Ochestrator GUID. It can optionally raise a ServerPollerEvent event via OnUpdatedObject.  
All Factory Orchestrator GUID types are supported.  
```csharp
public class ServerPoller :
System.IDisposable
```
Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; ServerPoller  

Implements [System.IDisposable](https://docs.microsoft.com/en-us/dotnet/api/System.IDisposable 'System.IDisposable')  
### Constructors
- [ServerPoller(System.Nullable&lt;System.Guid&gt;, System.Type, int, bool, int)](./Microsoft-FactoryOrchestrator-Client-ServerPoller-ServerPoller(System-Nullable-System-Guid-_System-Type_int_bool_int).md 'Microsoft.FactoryOrchestrator.Client.ServerPoller.ServerPoller(System.Nullable&lt;System.Guid&gt;, System.Type, int, bool, int)')
### Properties
- [IsPolling](./Microsoft-FactoryOrchestrator-Client-ServerPoller-IsPolling.md 'Microsoft.FactoryOrchestrator.Client.ServerPoller.IsPolling')
- [LatestObject](./Microsoft-FactoryOrchestrator-Client-ServerPoller-LatestObject.md 'Microsoft.FactoryOrchestrator.Client.ServerPoller.LatestObject')
- [OnlyRaiseOnExceptionEventForConnectionException](./Microsoft-FactoryOrchestrator-Client-ServerPoller-OnlyRaiseOnExceptionEventForConnectionException.md 'Microsoft.FactoryOrchestrator.Client.ServerPoller.OnlyRaiseOnExceptionEventForConnectionException')
- [PollingGuid](./Microsoft-FactoryOrchestrator-Client-ServerPoller-PollingGuid.md 'Microsoft.FactoryOrchestrator.Client.ServerPoller.PollingGuid')
### Methods
- [Dispose()](./Microsoft-FactoryOrchestrator-Client-ServerPoller-Dispose().md 'Microsoft.FactoryOrchestrator.Client.ServerPoller.Dispose()')
- [Dispose(bool)](./Microsoft-FactoryOrchestrator-Client-ServerPoller-Dispose(bool).md 'Microsoft.FactoryOrchestrator.Client.ServerPoller.Dispose(bool)')
- [StartPolling(Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient)](./Microsoft-FactoryOrchestrator-Client-ServerPoller-StartPolling(Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient).md 'Microsoft.FactoryOrchestrator.Client.ServerPoller.StartPolling(Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient)')
- [StopPolling()](./Microsoft-FactoryOrchestrator-Client-ServerPoller-StopPolling().md 'Microsoft.FactoryOrchestrator.Client.ServerPoller.StopPolling()')
### Events
- [OnException](./Microsoft-FactoryOrchestrator-Client-ServerPoller-OnException.md 'Microsoft.FactoryOrchestrator.Client.ServerPoller.OnException')
- [OnUpdatedObject](./Microsoft-FactoryOrchestrator-Client-ServerPoller-OnUpdatedObject.md 'Microsoft.FactoryOrchestrator.Client.ServerPoller.OnUpdatedObject')
