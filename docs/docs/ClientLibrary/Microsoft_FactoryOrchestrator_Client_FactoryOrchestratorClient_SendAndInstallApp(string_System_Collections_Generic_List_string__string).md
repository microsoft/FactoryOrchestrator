### [Microsoft.FactoryOrchestrator.Client](Microsoft_FactoryOrchestrator_Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.SendAndInstallApp(string, List&lt;string&gt;, string) Method
Copies an app package to the Service and installs it. Requires Windows Device Portal.  
If the app package is already on the Service's computer, use InstallApp() instead.  
```csharp
public System.Threading.Tasks.Task SendAndInstallApp(string appFilename, System.Collections.Generic.List<string> dependentPackages=null, string certificateFile=null);
```
#### Parameters
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_SendAndInstallApp(string_System_Collections_Generic_List_string__string)_appFilename'></a>
`appFilename` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Path on the Client's computer to the app package (.appx, .appxbundle, .msix, .msixbundle).
  
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_SendAndInstallApp(string_System_Collections_Generic_List_string__string)_dependentPackages'></a>
`dependentPackages` [System.Collections.Generic.List&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')[System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.List-1 'System.Collections.Generic.List')  
List of paths on the Client's computer to the app's dependent packages.
  
<a name='Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_SendAndInstallApp(string_System_Collections_Generic_List_string__string)_certificateFile'></a>
`certificateFile` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
Path on the Client's computer to the app's certificate file, if needed. Microsoft Store signed apps do not need a certificate.
  
#### Returns
[System.Threading.Tasks.Task](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task 'System.Threading.Tasks.Task')  
