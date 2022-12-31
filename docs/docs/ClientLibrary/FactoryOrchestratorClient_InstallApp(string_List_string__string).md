### [Microsoft.FactoryOrchestrator.Client](Microsoft_FactoryOrchestrator_Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.InstallApp(string, List&lt;string&gt;, string) Method
Asynchronously Installs an app package on the Service's computer. The app package must already be on the Service's computer. Requires Windows Device Portal.  
If the app package is not on the Service's computer already, use SendAndInstallApp() to copy and install it instead.  
```csharp
public System.Threading.Tasks.Task InstallApp(string appPackagePath, System.Collections.Generic.List<string> dependentPackages=null, string certificateFile=null);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_InstallApp(string_System_Collections_Generic_List_string__string)_appPackagePath'></a>
`appPackagePath` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Path on the Service's computer to the app package (.appx, .appxbundle, .msix, .msixbundle).
  
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_InstallApp(string_System_Collections_Generic_List_string__string)_dependentPackages'></a>
`dependentPackages` [System.Collections.Generic.List&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')[System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')  
List of paths on the Service's computer to the app's dependent packages.
  
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_InstallApp(string_System_Collections_Generic_List_string__string)_certificateFile'></a>
`certificateFile` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Path on the Service's computer to the app's certificate file, if needed. Microsoft Store signed apps do not need a certificate.
  
#### Returns
[System.Threading.Tasks.Task](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task 'System.Threading.Tasks.Task')  
