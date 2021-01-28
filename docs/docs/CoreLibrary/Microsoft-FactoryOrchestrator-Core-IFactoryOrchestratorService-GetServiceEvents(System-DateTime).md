#### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](./Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.GetServiceEvents(System.DateTime) Method
Get all Service events since given time.  
```csharp
System.Collections.Generic.List<Microsoft.FactoryOrchestrator.Core.ServiceEvent> GetServiceEvents(System.DateTime timeLastChecked);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Core-IFactoryOrchestratorService-GetServiceEvents(System-DateTime)-timeLastChecked'></a>
`timeLastChecked` [System.DateTime](https://docs.microsoft.com/en-us/dotnet/api/System.DateTime 'System.DateTime')  
  
  
#### Returns
[System.Collections.Generic.List&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')[ServiceEvent](./Microsoft-FactoryOrchestrator-Core-ServiceEvent.md 'Microsoft.FactoryOrchestrator.Core.ServiceEvent')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')  
List of Service events.  
