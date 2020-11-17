#### [FactoryOrchestratorCoreLibrary](./FactoryOrchestratorCoreLibrary.md 'FactoryOrchestratorCoreLibrary')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core').[WDPHelpers](./Microsoft-FactoryOrchestrator-Core-WDPHelpers.md 'Microsoft.FactoryOrchestrator.Core.WDPHelpers')
## WDPHelpers.InstallAppWithWDP(string, System.Collections.Generic.List&lt;string&gt;, string, string) Method
Installs an app package application with Windows Device Portal.  
```csharp
public static System.Threading.Tasks.Task InstallAppWithWDP(string appFilePath, System.Collections.Generic.List<string> dependentAppsFilePaths, string certFilePath, string ipAddress="localhost");
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Core-WDPHelpers-InstallAppWithWDP(string_System-Collections-Generic-List-string-_string_string)-appFilePath'></a>
`appFilePath` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
The app package file path.  
  
<a name='Microsoft-FactoryOrchestrator-Core-WDPHelpers-InstallAppWithWDP(string_System-Collections-Generic-List-string-_string_string)-dependentAppsFilePaths'></a>
`dependentAppsFilePaths` [System.Collections.Generic.List&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')[System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')  
The dependent app packages file paths.  
  
<a name='Microsoft-FactoryOrchestrator-Core-WDPHelpers-InstallAppWithWDP(string_System-Collections-Generic-List-string-_string_string)-certFilePath'></a>
`certFilePath` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
The certificate file path.  
  
<a name='Microsoft-FactoryOrchestrator-Core-WDPHelpers-InstallAppWithWDP(string_System-Collections-Generic-List-string-_string_string)-ipAddress'></a>
`ipAddress` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
The ip address of the device to install the app on.  
  
#### Returns
[System.Threading.Tasks.Task](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task 'System.Threading.Tasks.Task')  
#### Exceptions
[System.IO.FileNotFoundException](https://docs.microsoft.com/en-us/dotnet/api/System.IO.FileNotFoundException 'System.IO.FileNotFoundException')  
  
