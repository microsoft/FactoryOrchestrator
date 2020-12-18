#### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core').[UWPTask](./Microsoft-FactoryOrchestrator-Core-UWPTask.md 'Microsoft.FactoryOrchestrator.Core.UWPTask')
## UWPTask.AutoPassedIfLaunched Property
Gets or sets a value indicating whether this Task is automatically marked as Passed if the app is launched.  
```csharp
public bool AutoPassedIfLaunched { get; set; }
```
#### Property Value
[System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')  
If `true`, if this UWP app is successfully invoked, the TaskRun is marked as passed; otherwise, if `false`, the TaskRun must be manually passed via UpdateTaskRun().  
