### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core')
## ServiceEvent Class
A class containing information about a specific Factory Orchestrator Service event.  
```csharp
public class ServiceEvent
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; ServiceEvent  

| Constructors | |
| :--- | :--- |
| [ServiceEvent()](ServiceEvent_ServiceEvent().md 'Microsoft.FactoryOrchestrator.Core.ServiceEvent.ServiceEvent()') | JSON Constructor.<br/> |
| [ServiceEvent(ServiceEventType, Nullable&lt;Guid&gt;, string)](ServiceEvent_ServiceEvent(ServiceEventType_Nullable_Guid__string).md 'Microsoft.FactoryOrchestrator.Core.ServiceEvent.ServiceEvent(Microsoft.FactoryOrchestrator.Core.ServiceEventType, System.Nullable&lt;System.Guid&gt;, string)') | Create a new ServiceEvent.<br/> |

| Properties | |
| :--- | :--- |
| [EventIndex](ServiceEvent_EventIndex.md 'Microsoft.FactoryOrchestrator.Core.ServiceEvent.EventIndex') | The unique index of the event. Strictly increasing in order.<br/> |
| [EventTime](ServiceEvent_EventTime.md 'Microsoft.FactoryOrchestrator.Core.ServiceEvent.EventTime') | The time of the event.<br/> |
| [Guid](ServiceEvent_Guid.md 'Microsoft.FactoryOrchestrator.Core.ServiceEvent.Guid') | If not NULL, the object GUID associated with the event.<br/> |
| [Message](ServiceEvent_Message.md 'Microsoft.FactoryOrchestrator.Core.ServiceEvent.Message') | The message in the event.<br/> |
| [ServiceEventType](ServiceEvent_ServiceEventType.md 'Microsoft.FactoryOrchestrator.Core.ServiceEvent.ServiceEventType') | The type of the event.<br/> |
