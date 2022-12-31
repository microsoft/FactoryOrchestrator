### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core')
## ServiceEventType Enum
The type of Factory Orchestrator Service event.  
```csharp
public enum ServiceEventType

```
#### Fields
<a name='Microsoft_FactoryOrchestrator_Core_ServiceEventType_BootTasksComplete'></a>
`BootTasksComplete` 5  
The Factory Orchestrator Service is fully started. Boot tasks are completed.  
  
<a name='Microsoft_FactoryOrchestrator_Core_ServiceEventType_ContainerConnected'></a>
`ContainerConnected` 6  
The Factory Orchestrator Service is connected to a container also running a compatible version of Factory Orchestrator Service.  
  
<a name='Microsoft_FactoryOrchestrator_Core_ServiceEventType_ContainerDisabled'></a>
`ContainerDisabled` 8  
The Factory Orchestrator Service container support is disabled by policy.  
  
<a name='Microsoft_FactoryOrchestrator_Core_ServiceEventType_ContainerDisconnected'></a>
`ContainerDisconnected` 7  
The Factory Orchestrator Service is disconnected from a container also running a compatible version of Factory Orchestrator Service.  
  
<a name='Microsoft_FactoryOrchestrator_Core_ServiceEventType_ContainerServiceError'></a>
`ContainerServiceError` 11  
The Factory Orchestrator Service inside a connected container threw an exception.  
  
<a name='Microsoft_FactoryOrchestrator_Core_ServiceEventType_ContainerTaskRunRedirectedToRunAsRDUser'></a>
`ContainerTaskRunRedirectedToRunAsRDUser` 12  
The Factory Orchestrator Service inside a connected container has a TaskRun with GUI and it was redirected to RunAsRDUser.exe. It will not run until a remote user is logged in.  
  
<a name='Microsoft_FactoryOrchestrator_Core_ServiceEventType_DoneWaitingForExternalTaskRun'></a>
`DoneWaitingForExternalTaskRun` 0  
External TaskRun is completed.  
  
<a name='Microsoft_FactoryOrchestrator_Core_ServiceEventType_NetworkAccessDisabled'></a>
`NetworkAccessDisabled` 9  
The Factory Orchestrator Service network access is disabled by policy.  
  
<a name='Microsoft_FactoryOrchestrator_Core_ServiceEventType_ServiceError'></a>
`ServiceError` 3  
The Factory Orchestrator Service threw an exception.  
  
<a name='Microsoft_FactoryOrchestrator_Core_ServiceEventType_ServiceStart'></a>
`ServiceStart` 4  
The Factory Orchestrator Service is starting. It can now communicate with clients, but boot tasks may not be complete.  
  
<a name='Microsoft_FactoryOrchestrator_Core_ServiceEventType_TaskRunRedirectedToRunAsRDUser'></a>
`TaskRunRedirectedToRunAsRDUser` 10  
The TaskRun has GUI and was redirected to RunAsRDUser.exe. It will not run until a remote user is logged in.  
  
<a name='Microsoft_FactoryOrchestrator_Core_ServiceEventType_Unknown'></a>
`Unknown` 2147483647  
An unknown Factory Orchestrator Service event occurred.  
  
<a name='Microsoft_FactoryOrchestrator_Core_ServiceEventType_WaitingForContainerTaskRun'></a>
`WaitingForContainerTaskRun` 2  
TaskRun is waiting to be run by the container.  
  
<a name='Microsoft_FactoryOrchestrator_Core_ServiceEventType_WaitingForExternalTaskRun'></a>
`WaitingForExternalTaskRun` 1  
External TaskRun is waiting on an external action.  
  
