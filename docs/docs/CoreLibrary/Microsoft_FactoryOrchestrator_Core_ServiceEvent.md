### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core')
## ServiceEvent Class
A class containing information about a specific Factory Orchestrator Service event.  
```csharp
public class ServiceEvent
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; ServiceEvent  

| Constructors | |
| :--- | :--- |
| [ServiceEvent()](Microsoft_FactoryOrchestrator_Core_ServiceEvent_ServiceEvent().md 'Microsoft.FactoryOrchestrator.Core.ServiceEvent.ServiceEvent()') | JSON Constructor.<br/> |
| [ServiceEvent(ServiceEventType, Nullable&lt;Guid&gt;, string)](Microsoft_FactoryOrchestrator_Core_ServiceEvent_ServiceEvent(Microsoft_FactoryOrchestrator_Core_ServiceEventType_System_Nullable_System_Guid__string).md 'Microsoft.FactoryOrchestrator.Core.ServiceEvent.ServiceEvent(Microsoft.FactoryOrchestrator.Core.ServiceEventType, System.Nullable&lt;System.Guid&gt;, string)') | Create a new ServiceEvent.<br/> |

| Properties | |
| :--- | :--- |
| [EventIndex](Microsoft_FactoryOrchestrator_Core_ServiceEvent_EventIndex.md 'Microsoft.FactoryOrchestrator.Core.ServiceEvent.EventIndex') | The unique index of the event. Strictly increasing in order.<br/> |
| [EventTime](Microsoft_FactoryOrchestrator_Core_ServiceEvent_EventTime.md 'Microsoft.FactoryOrchestrator.Core.ServiceEvent.EventTime') | The time of the event.<br/> |
| [Guid](Microsoft_FactoryOrchestrator_Core_ServiceEvent_Guid.md 'Microsoft.FactoryOrchestrator.Core.ServiceEvent.Guid') | If not NULL, the object GUID associated with the event.<br/> |
| [Message](Microsoft_FactoryOrchestrator_Core_ServiceEvent_Message.md 'Microsoft.FactoryOrchestrator.Core.ServiceEvent.Message') | The message in the event.<br/> |
| [ServiceEventType](Microsoft_FactoryOrchestrator_Core_ServiceEvent_ServiceEventType.md 'Microsoft.FactoryOrchestrator.Core.ServiceEvent.ServiceEventType') | The type of the event.<br/> |
