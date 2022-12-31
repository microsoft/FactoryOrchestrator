### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core').[TaskBase](TaskBase.md 'Microsoft.FactoryOrchestrator.Core.TaskBase')
## TaskBase.CreateTaskFromTaskRun(TaskRun) Method
Creates a Task object from a TaskRun object.  
```csharp
public static Microsoft.FactoryOrchestrator.Core.TaskBase CreateTaskFromTaskRun(Microsoft.FactoryOrchestrator.Core.TaskRun run);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Core_TaskBase_CreateTaskFromTaskRun(Microsoft_FactoryOrchestrator_Core_TaskRun)_run'></a>
`run` [TaskRun](TaskRun.md 'Microsoft.FactoryOrchestrator.Core.TaskRun')  
The TaskRun.
  
#### Returns
[TaskBase](TaskBase.md 'Microsoft.FactoryOrchestrator.Core.TaskBase')  
a TaskBase object representing the Task defined by the Run.
#### Exceptions
[System.ComponentModel.InvalidEnumArgumentException](https://docs.microsoft.com/en-us/dotnet/api/System.ComponentModel.InvalidEnumArgumentException 'System.ComponentModel.InvalidEnumArgumentException')  
Given TaskRun has an invalid TaskType
