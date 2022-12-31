### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core').[UWPTask](UWPTask.md 'Microsoft.FactoryOrchestrator.Core.UWPTask')
## UWPTask.TerminateOnCompleted Property
Gets or sets a value indicating whether this UWP app is terminated when the Task is completed (Passed or Failed). If AutoPassedIfLaunched is `true`, this value is ignored.  
```csharp
public bool TerminateOnCompleted { get; set; }
```
#### Property Value
[System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')
If `true`, the app is automatically terminated when the TaskRun is completed.  
