#### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core').[TaskBase](./Microsoft-FactoryOrchestrator-Core-TaskBase.md 'Microsoft.FactoryOrchestrator.Core.TaskBase')
## TaskBase.CreateTaskFromTaskRun(Microsoft.FactoryOrchestrator.Core.TaskRun) Method
Creates a Task object from a TaskRun object.  
```csharp
public static Microsoft.FactoryOrchestrator.Core.TaskBase CreateTaskFromTaskRun(Microsoft.FactoryOrchestrator.Core.TaskRun run);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Core-TaskBase-CreateTaskFromTaskRun(Microsoft-FactoryOrchestrator-Core-TaskRun)-run'></a>
`run` [TaskRun](./Microsoft-FactoryOrchestrator-Core-TaskRun.md 'Microsoft.FactoryOrchestrator.Core.TaskRun')  
The TaskRun.  
  
#### Returns
[TaskBase](./Microsoft-FactoryOrchestrator-Core-TaskBase.md 'Microsoft.FactoryOrchestrator.Core.TaskBase')  
a TaskBase object representing the Task defined by the Run.  
#### Exceptions
[System.ComponentModel.InvalidEnumArgumentException](https://docs.microsoft.com/en-us/dotnet/api/System.ComponentModel.InvalidEnumArgumentException 'System.ComponentModel.InvalidEnumArgumentException')  
Given TaskRun has an invalid TaskType  
