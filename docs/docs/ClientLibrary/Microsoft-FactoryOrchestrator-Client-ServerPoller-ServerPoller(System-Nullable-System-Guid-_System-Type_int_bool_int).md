#### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client')
### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client').[ServerPoller](./Microsoft-FactoryOrchestrator-Client-ServerPoller.md 'Microsoft.FactoryOrchestrator.Client.ServerPoller')
## ServerPoller(System.Nullable&lt;System.Guid&gt;, System.Type, int, bool, int) Constructor
Create a new ServerPoller. The ServerPoller is associated with a specific FactoryOrchestratorClient and object you want to poll. The desired object is referred to by its GUID. The GUID can be NULL for TaskRun polling.  
If it is NULL and the guidType is TaskList, a List of TaskListSummary objects is returned.  
```csharp
public ServerPoller(System.Nullable<System.Guid> guidToPoll, System.Type guidType, int pollingIntervalMs=500, bool adaptiveInterval=true, int maxAdaptiveModifier=3);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Client-ServerPoller-ServerPoller(System-Nullable-System-Guid-_System-Type_int_bool_int)-guidToPoll'></a>
`guidToPoll` [System.Nullable&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable')[System.Guid](https://docs.microsoft.com/en-us/dotnet/api/System.Guid 'System.Guid')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable')  
GUID of the object you want to poll  
  
<a name='Microsoft-FactoryOrchestrator-Client-ServerPoller-ServerPoller(System-Nullable-System-Guid-_System-Type_int_bool_int)-guidType'></a>
`guidType` [System.Type](https://docs.microsoft.com/en-us/dotnet/api/System.Type 'System.Type')  
The type of object that GuidToPoll is  
  
<a name='Microsoft-FactoryOrchestrator-Client-ServerPoller-ServerPoller(System-Nullable-System-Guid-_System-Type_int_bool_int)-pollingIntervalMs'></a>
`pollingIntervalMs` [System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')  
How frequently the polling should be done, in milliseconds. Defaults to 500ms.  
  
<a name='Microsoft-FactoryOrchestrator-Client-ServerPoller-ServerPoller(System-Nullable-System-Guid-_System-Type_int_bool_int)-adaptiveInterval'></a>
`adaptiveInterval` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If true, automatically adjust the polling interval for best performance. Defaults to true.  
  
<a name='Microsoft-FactoryOrchestrator-Client-ServerPoller-ServerPoller(System-Nullable-System-Guid-_System-Type_int_bool_int)-maxAdaptiveModifier'></a>
`maxAdaptiveModifier` [System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')  
If adaptiveInterval is set, this defines the maximum multiplier/divisor that will be applied to the polling interval. For example, if maxAdaptiveModifier=2 and pollingIntervalMs=100, the object would be polled at a rate between 50ms to 200ms. Defaults to 5.  
  
