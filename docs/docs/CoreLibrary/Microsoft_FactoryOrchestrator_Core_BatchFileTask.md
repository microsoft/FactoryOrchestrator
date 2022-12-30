### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core')
## BatchFileTask Class
An BatchFile is a .cmd or .bat script that is run by the FactoryOrchestratorServer. The exit code of the script determines if the task passed or failed.  
0 == PASS, all others == FAIL.  
```csharp
public class BatchFileTask : Microsoft.FactoryOrchestrator.Core.ExecutableTask
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; [NotifyPropertyChangedBase](Microsoft_FactoryOrchestrator_Core_NotifyPropertyChangedBase.md 'Microsoft.FactoryOrchestrator.Core.NotifyPropertyChangedBase') &#129106; [TaskBase](Microsoft_FactoryOrchestrator_Core_TaskBase.md 'Microsoft.FactoryOrchestrator.Core.TaskBase') &#129106; [ExecutableTask](Microsoft_FactoryOrchestrator_Core_ExecutableTask.md 'Microsoft.FactoryOrchestrator.Core.ExecutableTask') &#129106; BatchFileTask  

Derived  
&#8627; [CommandLineTask](Microsoft_FactoryOrchestrator_Core_CommandLineTask.md 'Microsoft.FactoryOrchestrator.Core.CommandLineTask')  

| Constructors | |
| :--- | :--- |
| [BatchFileTask(string)](Microsoft_FactoryOrchestrator_Core_BatchFileTask_BatchFileTask(string).md 'Microsoft.FactoryOrchestrator.Core.BatchFileTask.BatchFileTask(string)') | Initializes a new instance of the [BatchFileTask](Microsoft_FactoryOrchestrator_Core_BatchFileTask.md 'Microsoft.FactoryOrchestrator.Core.BatchFileTask') class.<br/> |

| Properties | |
| :--- | :--- |
| [Name](Microsoft_FactoryOrchestrator_Core_BatchFileTask_Name.md 'Microsoft.FactoryOrchestrator.Core.BatchFileTask.Name') | The friendly name of the Task.<br/> |

| Methods | |
| :--- | :--- |
| [Equals(object)](Microsoft_FactoryOrchestrator_Core_BatchFileTask_Equals(object).md 'Microsoft.FactoryOrchestrator.Core.BatchFileTask.Equals(object)') | Determines whether the specified [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object'), is equal to this instance.<br/> |
