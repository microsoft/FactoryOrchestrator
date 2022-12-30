### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core')
## TaskStatus Enum
The status of a Task, TaskRun, or TaskList.  
```csharp
public enum TaskStatus

```
#### Fields
<a name='Microsoft_FactoryOrchestrator_Core_TaskStatus_Aborted'></a>
`Aborted` 2  
The Task was cancelled.  
  
<a name='Microsoft_FactoryOrchestrator_Core_TaskStatus_Failed'></a>
`Failed` 1  
The Task failed.  
  
<a name='Microsoft_FactoryOrchestrator_Core_TaskStatus_NotRun'></a>
`NotRun` 5  
The Task has never been run.  
  
<a name='Microsoft_FactoryOrchestrator_Core_TaskStatus_Passed'></a>
`Passed` 0  
The Task passed with no errors.  
  
<a name='Microsoft_FactoryOrchestrator_Core_TaskStatus_Running'></a>
`Running` 4  
The Task is actively running.  
  
<a name='Microsoft_FactoryOrchestrator_Core_TaskStatus_RunPending'></a>
`RunPending` 6  
The Task is queued to run.  
  
<a name='Microsoft_FactoryOrchestrator_Core_TaskStatus_Timeout'></a>
`Timeout` 3  
The Task hit its timeout and was cancelled.  
  
<a name='Microsoft_FactoryOrchestrator_Core_TaskStatus_Unknown'></a>
`Unknown` 2147483647  
The Task state is unknown, likely due to a Service error.  
  
<a name='Microsoft_FactoryOrchestrator_Core_TaskStatus_WaitingForExternalResult'></a>
`WaitingForExternalResult` 7  
The Task is waiting for its result from a client.  
  
