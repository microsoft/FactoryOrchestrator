### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core')
## ExecutableTask Class
An ExecutableTask is an .exe binary that is run by the FactoryOrchestratorServer. The exit code of the process determines if the task passed or failed.  
0 == PASS, all others == FAIL.  
```csharp
public class ExecutableTask : Microsoft.FactoryOrchestrator.Core.TaskBase
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; [NotifyPropertyChangedBase](NotifyPropertyChangedBase.md 'Microsoft.FactoryOrchestrator.Core.NotifyPropertyChangedBase') &#129106; [TaskBase](TaskBase.md 'Microsoft.FactoryOrchestrator.Core.TaskBase') &#129106; ExecutableTask  

Derived  
&#8627; [BatchFileTask](BatchFileTask.md 'Microsoft.FactoryOrchestrator.Core.BatchFileTask')  
&#8627; [PowerShellTask](PowerShellTask.md 'Microsoft.FactoryOrchestrator.Core.PowerShellTask')  
&#8627; [TAEFTest](TAEFTest.md 'Microsoft.FactoryOrchestrator.Core.TAEFTest')  

| Constructors | |
| :--- | :--- |
| [ExecutableTask(string)](ExecutableTask_ExecutableTask(string).md 'Microsoft.FactoryOrchestrator.Core.ExecutableTask.ExecutableTask(string)') | Initializes a new instance of the [ExecutableTask](ExecutableTask.md 'Microsoft.FactoryOrchestrator.Core.ExecutableTask') class.<br/> |
| [ExecutableTask(string, TaskType)](ExecutableTask_ExecutableTask(string_TaskType).md 'Microsoft.FactoryOrchestrator.Core.ExecutableTask.ExecutableTask(string, Microsoft.FactoryOrchestrator.Core.TaskType)') | Initializes a new instance of the [ExecutableTask](ExecutableTask.md 'Microsoft.FactoryOrchestrator.Core.ExecutableTask') class.<br/> |

| Properties | |
| :--- | :--- |
| [BackgroundTask](ExecutableTask_BackgroundTask.md 'Microsoft.FactoryOrchestrator.Core.ExecutableTask.BackgroundTask') | Denotes if this Task is run as a background task.<br/> |
| [Name](ExecutableTask_Name.md 'Microsoft.FactoryOrchestrator.Core.ExecutableTask.Name') | The friendly name of the Task.<br/> |

| Methods | |
| :--- | :--- |
| [Equals(object)](ExecutableTask_Equals(object).md 'Microsoft.FactoryOrchestrator.Core.ExecutableTask.Equals(object)') | Determines whether the specified [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object'), is equal to this instance.<br/> |
| [ToString()](ExecutableTask_ToString().md 'Microsoft.FactoryOrchestrator.Core.ExecutableTask.ToString()') | Converts to string.<br/> |
