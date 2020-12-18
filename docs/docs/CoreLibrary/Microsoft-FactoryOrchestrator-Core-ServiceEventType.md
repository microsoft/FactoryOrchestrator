#### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
## ServiceEventType Enum
The type of Factory Orchestrator Service event.  
```csharp
public enum ServiceEventType
```
### Fields
<a name='Microsoft-FactoryOrchestrator-Core-ServiceEventType-DoneWaitingForExternalTaskRun'></a>
`DoneWaitingForExternalTaskRun` 0  
External TaskRun is completed.  
  
<a name='Microsoft-FactoryOrchestrator-Core-ServiceEventType-WaitingForExternalTaskRun'></a>
`WaitingForExternalTaskRun` 1  
External TaskRun is waiting on an external action.  
  
<a name='Microsoft-FactoryOrchestrator-Core-ServiceEventType-WaitingForContainerTaskRun'></a>
`WaitingForContainerTaskRun` 2  
TaskRun is waiting to be run by the container.  
  
<a name='Microsoft-FactoryOrchestrator-Core-ServiceEventType-ServiceError'></a>
`ServiceError` 3  
The Factory Orchestrator Service threw an exception.  
  
<a name='Microsoft-FactoryOrchestrator-Core-ServiceEventType-ServiceStart'></a>
`ServiceStart` 4  
The Factory Orchestrator Service is starting. It can now communicate with clients, but boot tasks may not be complete.  
  
<a name='Microsoft-FactoryOrchestrator-Core-ServiceEventType-BootTasksComplete'></a>
`BootTasksComplete` 5  
The Factory Orchestrator Service is fully started. Boot tasks are completed.  
  
<a name='Microsoft-FactoryOrchestrator-Core-ServiceEventType-ContainerConnected'></a>
`ContainerConnected` 6  
The Factory Orchestrator Service is connected to a container also running a compatible version of Factory Orchestrator Service.  
  
<a name='Microsoft-FactoryOrchestrator-Core-ServiceEventType-ContainerDisconnected'></a>
`ContainerDisconnected` 7  
The Factory Orchestrator Service is disconnected from a container also running a compatible version of Factory Orchestrator Service.  
  
<a name='Microsoft-FactoryOrchestrator-Core-ServiceEventType-ContainerDisabled'></a>
`ContainerDisabled` 8  
The Factory Orchestrator Service container support is disabled by policy.  
  
<a name='Microsoft-FactoryOrchestrator-Core-ServiceEventType-NetworkAccessDisabled'></a>
`NetworkAccessDisabled` 9  
The Factory Orchestrator Service network access is disabled by policy.  
  
<a name='Microsoft-FactoryOrchestrator-Core-ServiceEventType-Unknown'></a>
`Unknown` 2147483647  
An unknown Factory Orchestrator Service event occurred.  
  
