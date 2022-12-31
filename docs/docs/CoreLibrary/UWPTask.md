### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core')
## UWPTask Class
A UWPTest is a UWP task run by the FactoryOrchestrator.App client. These are used for UI.  
task results must be returned to the server via SetTaskRunStatus().  
```csharp
public class UWPTask : Microsoft.FactoryOrchestrator.Core.ExternalTask
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; [NotifyPropertyChangedBase](NotifyPropertyChangedBase.md 'Microsoft.FactoryOrchestrator.Core.NotifyPropertyChangedBase') &#129106; [TaskBase](TaskBase.md 'Microsoft.FactoryOrchestrator.Core.TaskBase') &#129106; [ExternalTask](ExternalTask.md 'Microsoft.FactoryOrchestrator.Core.ExternalTask') &#129106; UWPTask  

| Constructors | |
| :--- | :--- |
| [UWPTask(string)](UWPTask_UWPTask(string).md 'Microsoft.FactoryOrchestrator.Core.UWPTask.UWPTask(string)') | Initializes a new instance of the [UWPTask](UWPTask.md 'Microsoft.FactoryOrchestrator.Core.UWPTask') class.<br/> |
| [UWPTask(string, string)](UWPTask_UWPTask(string_string).md 'Microsoft.FactoryOrchestrator.Core.UWPTask.UWPTask(string, string)') | Initializes a new instance of the [UWPTask](UWPTask.md 'Microsoft.FactoryOrchestrator.Core.UWPTask') class.<br/> |

| Properties | |
| :--- | :--- |
| [AutoPassedIfLaunched](UWPTask_AutoPassedIfLaunched.md 'Microsoft.FactoryOrchestrator.Core.UWPTask.AutoPassedIfLaunched') | Gets or sets a value indicating whether this Task is automatically marked as Passed if the app is launched.<br/> |
| [Name](UWPTask_Name.md 'Microsoft.FactoryOrchestrator.Core.UWPTask.Name') | The friendly name of the Task.<br/> |
| [TerminateOnCompleted](UWPTask_TerminateOnCompleted.md 'Microsoft.FactoryOrchestrator.Core.UWPTask.TerminateOnCompleted') | Gets or sets a value indicating whether this UWP app is terminated when the Task is completed (Passed or Failed). If AutoPassedIfLaunched is `true`, this value is ignored.<br/> |

| Methods | |
| :--- | :--- |
| [Equals(object)](UWPTask_Equals(object).md 'Microsoft.FactoryOrchestrator.Core.UWPTask.Equals(object)') | Determines whether the specified [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object'), is equal to this instance.<br/> |
