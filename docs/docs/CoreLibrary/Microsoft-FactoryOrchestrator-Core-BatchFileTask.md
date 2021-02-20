#### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
## BatchFileTask Class
An BatchFile is a .cmd or .bat script that is run by the FactoryOrchestratorServer. The exit code of the script determines if the task passed or failed.  
0 == PASS, all others == FAIL.  
```csharp
public class BatchFileTask : Microsoft.FactoryOrchestrator.Core.ExecutableTask
```
Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; [Microsoft.FactoryOrchestrator.Core.NotifyPropertyChangedBase](./../../CoreLibrary/Microsoft-FactoryOrchestrator-Core-NotifyPropertyChangedBase 'Microsoft.FactoryOrchestrator.Core.NotifyPropertyChangedBase') &#129106; [TaskBase](./Microsoft-FactoryOrchestrator-Core-TaskBase.md 'Microsoft.FactoryOrchestrator.Core.TaskBase') &#129106; [ExecutableTask](./Microsoft-FactoryOrchestrator-Core-ExecutableTask.md 'Microsoft.FactoryOrchestrator.Core.ExecutableTask') &#129106; BatchFileTask  
### Constructors
- [BatchFileTask(string)](./Microsoft-FactoryOrchestrator-Core-BatchFileTask-BatchFileTask(string).md 'Microsoft.FactoryOrchestrator.Core.BatchFileTask.BatchFileTask(string)')
### Properties
- [Name](./Microsoft-FactoryOrchestrator-Core-BatchFileTask-Name.md 'Microsoft.FactoryOrchestrator.Core.BatchFileTask.Name')
### Methods
- [Equals(object)](./Microsoft-FactoryOrchestrator-Core-BatchFileTask-Equals(object).md 'Microsoft.FactoryOrchestrator.Core.BatchFileTask.Equals(object)')
