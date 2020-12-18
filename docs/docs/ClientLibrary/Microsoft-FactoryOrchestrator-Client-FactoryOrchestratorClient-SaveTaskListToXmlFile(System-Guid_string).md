#### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client')
### [Microsoft.FactoryOrchestrator.Client](./Microsoft-FactoryOrchestrator-Client.md 'Microsoft.FactoryOrchestrator.Client').[FactoryOrchestratorClient](./Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient.md 'Microsoft.FactoryOrchestrator.Client.FactoryOrchestratorClient')
## FactoryOrchestratorClient.SaveTaskListToXmlFile(System.Guid, string) Method
Asynchronously Saves a TaskList to a FactoryOrchestratorXML file.  
```csharp
public System.Threading.Tasks.Task SaveTaskListToXmlFile(System.Guid guid, string filename);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-SaveTaskListToXmlFile(System-Guid_string)-guid'></a>
`guid` [System.Guid](https://docs.microsoft.com/en-us/dotnet/api/System.Guid 'System.Guid')  
The GUID of the TaskList you wish to save.  
  
<a name='Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-SaveTaskListToXmlFile(System-Guid_string)-filename'></a>
`filename` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')  
The path to the FactoryOrchestratorXML file that will be created.  
  
#### Returns
[System.Threading.Tasks.Task](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.Task 'System.Threading.Tasks.Task')  
true on success  
