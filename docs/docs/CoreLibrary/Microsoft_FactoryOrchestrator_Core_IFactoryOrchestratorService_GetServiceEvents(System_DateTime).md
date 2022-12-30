### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core').[IFactoryOrchestratorService](Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService')
## IFactoryOrchestratorService.GetServiceEvents(DateTime) Method
Get all Service events since given time.  
```csharp
System.Collections.Generic.List<Microsoft.FactoryOrchestrator.Core.ServiceEvent> GetServiceEvents(System.DateTime timeLastChecked);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService_GetServiceEvents(System_DateTime)_timeLastChecked'></a>
`timeLastChecked` [System.DateTime](https://docs.microsoft.com/en-us/dotnet/api/System.DateTime 'System.DateTime')  
  
#### Returns
[System.Collections.Generic.List&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')[ServiceEvent](Microsoft_FactoryOrchestrator_Core_ServiceEvent.md 'Microsoft.FactoryOrchestrator.Core.ServiceEvent')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')  
List of Service events.
