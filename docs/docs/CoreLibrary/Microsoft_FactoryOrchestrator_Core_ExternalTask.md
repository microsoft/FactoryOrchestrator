### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core')
## ExternalTask Class
An ExternalTest is a task run outside of the FactoryOrchestratorServer.  
task results must be returned to the server via SetTaskRunStatus().  
```csharp
public class ExternalTask : Microsoft.FactoryOrchestrator.Core.TaskBase
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; [NotifyPropertyChangedBase](Microsoft_FactoryOrchestrator_Core_NotifyPropertyChangedBase.md 'Microsoft.FactoryOrchestrator.Core.NotifyPropertyChangedBase') &#129106; [TaskBase](Microsoft_FactoryOrchestrator_Core_TaskBase.md 'Microsoft.FactoryOrchestrator.Core.TaskBase') &#129106; ExternalTask  

Derived  
&#8627; [UWPTask](Microsoft_FactoryOrchestrator_Core_UWPTask.md 'Microsoft.FactoryOrchestrator.Core.UWPTask')  

| Constructors | |
| :--- | :--- |
| [ExternalTask(string)](Microsoft_FactoryOrchestrator_Core_ExternalTask_ExternalTask(string).md 'Microsoft.FactoryOrchestrator.Core.ExternalTask.ExternalTask(string)') | Initializes a new instance of the [ExternalTask](Microsoft_FactoryOrchestrator_Core_ExternalTask.md 'Microsoft.FactoryOrchestrator.Core.ExternalTask') class.<br/> |
| [ExternalTask(string, string, TaskType)](Microsoft_FactoryOrchestrator_Core_ExternalTask_ExternalTask(string_string_Microsoft_FactoryOrchestrator_Core_TaskType).md 'Microsoft.FactoryOrchestrator.Core.ExternalTask.ExternalTask(string, string, Microsoft.FactoryOrchestrator.Core.TaskType)') | Initializes a new instance of the [ExternalTask](Microsoft_FactoryOrchestrator_Core_ExternalTask.md 'Microsoft.FactoryOrchestrator.Core.ExternalTask') class.<br/> |

| Properties | |
| :--- | :--- |
| [Name](Microsoft_FactoryOrchestrator_Core_ExternalTask_Name.md 'Microsoft.FactoryOrchestrator.Core.ExternalTask.Name') | The friendly name of the Task.<br/> |

| Methods | |
| :--- | :--- |
| [Equals(object)](Microsoft_FactoryOrchestrator_Core_ExternalTask_Equals(object).md 'Microsoft.FactoryOrchestrator.Core.ExternalTask.Equals(object)') | Determines whether the specified [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object'), is equal to this instance.<br/> |
| [ToString()](Microsoft_FactoryOrchestrator_Core_ExternalTask_ToString().md 'Microsoft.FactoryOrchestrator.Core.ExternalTask.ToString()') | Converts to string.<br/> |
