#### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](./Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.GetServiceEvents(ulong) Method
Get all Service events since given index.  
```csharp
System.Collections.Generic.List<Microsoft.FactoryOrchestrator.Core.ServiceEvent> GetServiceEvents(ulong lastEventIndex);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService-GetServiceEvents(ulong)-lastEventIndex'></a>
`lastEventIndex` [System.UInt64](https://docs.microsoft.com/en-us/dotnet/api/System.UInt64 'System.UInt64')  
  
  
#### Returns
[System.Collections.Generic.List&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')[ServiceEvent](./Microsoft-FactoryOrchestrator-Core-ServiceEvent.md 'Microsoft.FactoryOrchestrator.Core.ServiceEvent')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')  
List of Service events.  
