#### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core').[WDPHelpers](./Microsoft-FactoryOrchestrator-Core-WDPHelpers.md 'Microsoft.FactoryOrchestrator.Core.WDPHelpers')
## WDPHelpers.CloseAppWithWDP(string, string) Method
Closes a running app package application with Windows Device Portal.  
```csharp
public static System.Threading.Tasks.Task CloseAppWithWDP(string app, string ipAddress="localhost");
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Core-WDPHelpers-CloseAppWithWDP(string_string)-app'></a>
`app` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
The app package to exit .  
  
<a name='Microsoft-FactoryOrchestrator-Core-WDPHelpers-CloseAppWithWDP(string_string)-ipAddress'></a>
`ipAddress` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
The ip address of the device to exit the app on.  
  
#### Returns
[System.Threading.Tasks.Task](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task 'System.Threading.Tasks.Task')  
#### Exceptions
[System.ArgumentException](https://docs.microsoft.com/en-us/dotnet/api/System.ArgumentException 'System.ArgumentException')  
  
