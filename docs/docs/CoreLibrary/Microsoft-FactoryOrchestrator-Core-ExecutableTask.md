#### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
## ExecutableTask Class
An ExecutableTask is an .exe binary that is run by the FactoryOrchestratorServer. The exit code of the process determines if the task passed or failed.  
0 == PASS, all others == FAIL.  
```csharp
public class ExecutableTask : Microsoft.FactoryOrchestrator.Core.TaskBase
```
Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; [Microsoft.FactoryOrchestrator.Core.NotifyPropertyChangedBase](./../../CoreLibrary/Microsoft-FactoryOrchestrator-Core-NotifyPropertyChangedBase 'Microsoft.FactoryOrchestrator.Core.NotifyPropertyChangedBase') &#129106; [TaskBase](./Microsoft-FactoryOrchestrator-Core-TaskBase.md 'Microsoft.FactoryOrchestrator.Core.TaskBase') &#129106; ExecutableTask  

Derived  
&#8627; [BatchFileTask](./Microsoft-FactoryOrchestrator-Core-BatchFileTask.md 'Microsoft.FactoryOrchestrator.Core.BatchFileTask')  
&#8627; [PowerShellTask](./Microsoft-FactoryOrchestrator-Core-PowerShellTask.md 'Microsoft.FactoryOrchestrator.Core.PowerShellTask')  
&#8627; [TAEFTest](./Microsoft-FactoryOrchestrator-Core-TAEFTest.md 'Microsoft.FactoryOrchestrator.Core.TAEFTest')  
### Constructors
- [ExecutableTask(string)](./Microsoft-FactoryOrchestrator-Core-ExecutableTask-ExecutableTask(string).md 'Microsoft.FactoryOrchestrator.Core.ExecutableTask.ExecutableTask(string)')
- [ExecutableTask(string, Microsoft.FactoryOrchestrator.Core.TaskType)](./Microsoft-FactoryOrchestrator-Core-ExecutableTask-ExecutableTask(string_Microsoft-FactoryOrchestrator-Core-TaskType).md 'Microsoft.FactoryOrchestrator.Core.ExecutableTask.ExecutableTask(string, Microsoft.FactoryOrchestrator.Core.TaskType)')
### Properties
- [BackgroundTask](./Microsoft-FactoryOrchestrator-Core-ExecutableTask-BackgroundTask.md 'Microsoft.FactoryOrchestrator.Core.ExecutableTask.BackgroundTask')
- [Name](./Microsoft-FactoryOrchestrator-Core-ExecutableTask-Name.md 'Microsoft.FactoryOrchestrator.Core.ExecutableTask.Name')
### Methods
- [Equals(object)](./Microsoft-FactoryOrchestrator-Core-ExecutableTask-Equals(object).md 'Microsoft.FactoryOrchestrator.Core.ExecutableTask.Equals(object)')
- [ToString()](./Microsoft-FactoryOrchestrator-Core-ExecutableTask-ToString().md 'Microsoft.FactoryOrchestrator.Core.ExecutableTask.ToString()')
