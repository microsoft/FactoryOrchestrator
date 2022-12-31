### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core')
## FactoryOrchestratorXML Class
This class is used to save and load TaskLists from an XML file.  
```csharp
public class FactoryOrchestratorXML
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; FactoryOrchestratorXML  

| Constructors | |
| :--- | :--- |
| [FactoryOrchestratorXML()](FactoryOrchestratorXML_FactoryOrchestratorXML().md 'Microsoft.FactoryOrchestrator.Core.FactoryOrchestratorXML.FactoryOrchestratorXML()') | Constructor.<br/> |

| Properties | |
| :--- | :--- |
| [TaskLists](FactoryOrchestratorXML_TaskLists.md 'Microsoft.FactoryOrchestrator.Core.FactoryOrchestratorXML.TaskLists') | The TaskLists in the XML file.<br/> |

| Methods | |
| :--- | :--- |
| [Load(string)](FactoryOrchestratorXML_Load(string).md 'Microsoft.FactoryOrchestrator.Core.FactoryOrchestratorXML.Load(string)') | Loads the TaskLists in a FactoryOrchestratorXML file.<br/> |
| [PostDeserialize()](FactoryOrchestratorXML_PostDeserialize().md 'Microsoft.FactoryOrchestrator.Core.FactoryOrchestratorXML.PostDeserialize()') | Create Guids for any imported task or tasklist that is missing one.<br/>Create Tests dictionary.<br/> |
| [Save(string)](FactoryOrchestratorXML_Save(string).md 'Microsoft.FactoryOrchestrator.Core.FactoryOrchestratorXML.Save(string)') | Saves a FactoryOrchestratorXML object to the given file. The file is overwritten if it exists.<br/> |
