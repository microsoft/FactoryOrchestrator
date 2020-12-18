#### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
## TaskStatus Enum
The status of a Task, TaskRun, or TaskList.  
```csharp
public enum TaskStatus
```
### Fields
<a name='Microsoft-FactoryOrchestrator-Core-TaskStatus-Passed'></a>
`Passed` 0  
The Task passed with no errors.  
  
<a name='Microsoft-FactoryOrchestrator-Core-TaskStatus-Failed'></a>
`Failed` 1  
The Task failed.  
  
<a name='Microsoft-FactoryOrchestrator-Core-TaskStatus-Aborted'></a>
`Aborted` 2  
The Task was cancelled.  
  
<a name='Microsoft-FactoryOrchestrator-Core-TaskStatus-Timeout'></a>
`Timeout` 3  
The Task hit its timeout and was cancelled.  
  
<a name='Microsoft-FactoryOrchestrator-Core-TaskStatus-Running'></a>
`Running` 4  
The Task is actively running.  
  
<a name='Microsoft-FactoryOrchestrator-Core-TaskStatus-NotRun'></a>
`NotRun` 5  
The Task has never been run.  
  
<a name='Microsoft-FactoryOrchestrator-Core-TaskStatus-RunPending'></a>
`RunPending` 6  
The Task is queued to run.  
  
<a name='Microsoft-FactoryOrchestrator-Core-TaskStatus-WaitingForExternalResult'></a>
`WaitingForExternalResult` 7  
The Task is waiting for its result from a client.  
  
<a name='Microsoft-FactoryOrchestrator-Core-TaskStatus-Unknown'></a>
`Unknown` 8  
The Task state is unknown, likely due to a Service error.  
  
