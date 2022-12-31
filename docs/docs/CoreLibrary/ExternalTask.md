### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core')
## ExternalTask Class
An ExternalTest is a task run outside of the FactoryOrchestratorServer.  
task results must be returned to the server via SetTaskRunStatus().  
```csharp
public class ExternalTask : Microsoft.FactoryOrchestrator.Core.TaskBase
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; [NotifyPropertyChangedBase](NotifyPropertyChangedBase.md 'Microsoft.FactoryOrchestrator.Core.NotifyPropertyChangedBase') &#129106; [TaskBase](TaskBase.md 'Microsoft.FactoryOrchestrator.Core.TaskBase') &#129106; ExternalTask  

Derived  
&#8627; [UWPTask](UWPTask.md 'Microsoft.FactoryOrchestrator.Core.UWPTask')  

| Constructors | |
| :--- | :--- |
| [ExternalTask(string)](ExternalTask_ExternalTask(string).md 'Microsoft.FactoryOrchestrator.Core.ExternalTask.ExternalTask(string)') | Initializes a new instance of the [ExternalTask](ExternalTask.md 'Microsoft.FactoryOrchestrator.Core.ExternalTask') class.<br/> |
| [ExternalTask(string, string, TaskType)](ExternalTask_ExternalTask(string_string_TaskType).md 'Microsoft.FactoryOrchestrator.Core.ExternalTask.ExternalTask(string, string, Microsoft.FactoryOrchestrator.Core.TaskType)') | Initializes a new instance of the [ExternalTask](ExternalTask.md 'Microsoft.FactoryOrchestrator.Core.ExternalTask') class.<br/> |

| Properties | |
| :--- | :--- |
| [Name](ExternalTask_Name.md 'Microsoft.FactoryOrchestrator.Core.ExternalTask.Name') | The friendly name of the Task.<br/> |

| Methods | |
| :--- | :--- |
| [Equals(object)](ExternalTask_Equals(object).md 'Microsoft.FactoryOrchestrator.Core.ExternalTask.Equals(object)') | Determines whether the specified [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object'), is equal to this instance.<br/> |
| [ToString()](ExternalTask_ToString().md 'Microsoft.FactoryOrchestrator.Core.ExternalTask.ToString()') | Converts to string.<br/> |
