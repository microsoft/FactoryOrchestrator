#### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
## CommandLineTask Class
An CommandLineTask is a .cmd, .bat, or .sh script. This is known as a Batch file on Windows and a Shell script on Linux. The exit code of the script determines if the task passed or failed.  
0 == PASS, all others == FAIL.  
```csharp
public class CommandLineTask : Microsoft.FactoryOrchestrator.Core.ExecutableTask
```
Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; [Microsoft.FactoryOrchestrator.Core.NotifyPropertyChangedBase](./../../CoreLibrary/Microsoft-FactoryOrchestrator-Core-NotifyPropertyChangedBase 'Microsoft.FactoryOrchestrator.Core.NotifyPropertyChangedBase') &#129106; [TaskBase](./Microsoft-FactoryOrchestrator-Core-TaskBase.md 'Microsoft.FactoryOrchestrator.Core.TaskBase') &#129106; [ExecutableTask](./Microsoft-FactoryOrchestrator-Core-ExecutableTask.md 'Microsoft.FactoryOrchestrator.Core.ExecutableTask') &#129106; CommandLineTask  
### Constructors
- [CommandLineTask(string)](./Microsoft-FactoryOrchestrator-Core-CommandLineTask-CommandLineTask(string).md 'Microsoft.FactoryOrchestrator.Core.CommandLineTask.CommandLineTask(string)')
### Properties
- [Name](./Microsoft-FactoryOrchestrator-Core-CommandLineTask-Name.md 'Microsoft.FactoryOrchestrator.Core.CommandLineTask.Name')
### Methods
- [Equals(object)](./Microsoft-FactoryOrchestrator-Core-CommandLineTask-Equals(object).md 'Microsoft.FactoryOrchestrator.Core.CommandLineTask.Equals(object)')
