### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core')
## PowerShellTask Class
An PowerShellTask is a PowerShell Core .ps1 script that is run by the FactoryOrchestratorServer. The exit code of the script determines if the task passed or failed.  
0 == PASS, all others == FAIL.  
```csharp
public class PowerShellTask : Microsoft.FactoryOrchestrator.Core.ExecutableTask
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; [NotifyPropertyChangedBase](NotifyPropertyChangedBase.md 'Microsoft.FactoryOrchestrator.Core.NotifyPropertyChangedBase') &#129106; [TaskBase](TaskBase.md 'Microsoft.FactoryOrchestrator.Core.TaskBase') &#129106; [ExecutableTask](ExecutableTask.md 'Microsoft.FactoryOrchestrator.Core.ExecutableTask') &#129106; PowerShellTask  

| Constructors | |
| :--- | :--- |
| [PowerShellTask(string)](PowerShellTask_PowerShellTask(string).md 'Microsoft.FactoryOrchestrator.Core.PowerShellTask.PowerShellTask(string)') | Initializes a new instance of the [PowerShellTask](PowerShellTask.md 'Microsoft.FactoryOrchestrator.Core.PowerShellTask') class.<br/> |

| Properties | |
| :--- | :--- |
| [Name](PowerShellTask_Name.md 'Microsoft.FactoryOrchestrator.Core.PowerShellTask.Name') | The friendly name of the Task.<br/> |

| Methods | |
| :--- | :--- |
| [Equals(object)](PowerShellTask_Equals(object).md 'Microsoft.FactoryOrchestrator.Core.PowerShellTask.Equals(object)') | Determines whether the specified [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object'), is equal to this instance.<br/> |
