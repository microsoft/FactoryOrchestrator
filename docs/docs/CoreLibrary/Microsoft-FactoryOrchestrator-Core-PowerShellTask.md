#### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
## PowerShellTask Class
An PowerShellTask is a PowerShell Core .ps1 script that is run by the FactoryOrchestratorServer. The exit code of the script determines if the task passed or failed.  
0 == PASS, all others == FAIL.  
```csharp
public class PowerShellTask : Microsoft.FactoryOrchestrator.Core.ExecutableTask
```
Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; [Microsoft.FactoryOrchestrator.Core.NotifyPropertyChangedBase](./../../CoreLibrary/Microsoft-FactoryOrchestrator-Core-NotifyPropertyChangedBase 'Microsoft.FactoryOrchestrator.Core.NotifyPropertyChangedBase') &#129106; [TaskBase](./Microsoft-FactoryOrchestrator-Core-TaskBase.md 'Microsoft.FactoryOrchestrator.Core.TaskBase') &#129106; [ExecutableTask](./Microsoft-FactoryOrchestrator-Core-ExecutableTask.md 'Microsoft.FactoryOrchestrator.Core.ExecutableTask') &#129106; PowerShellTask  
### Constructors
- [PowerShellTask(string)](./Microsoft-FactoryOrchestrator-Core-PowerShellTask-PowerShellTask(string).md 'Microsoft.FactoryOrchestrator.Core.PowerShellTask.PowerShellTask(string)')
### Properties
- [Name](./Microsoft-FactoryOrchestrator-Core-PowerShellTask-Name.md 'Microsoft.FactoryOrchestrator.Core.PowerShellTask.Name')
### Methods
- [Equals(object)](./Microsoft-FactoryOrchestrator-Core-PowerShellTask-Equals(object).md 'Microsoft.FactoryOrchestrator.Core.PowerShellTask.Equals(object)')
