### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core')
## FactoryOrchestratorException Class
A generic Factory Orchestrator exception.  
```csharp
public class FactoryOrchestratorException : System.Exception
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; [System.Exception](https://docs.microsoft.com/en-us/dotnet/api/System.Exception 'System.Exception') &#129106; FactoryOrchestratorException  

Derived  
&#8627; [FactoryOrchestratorContainerException](FactoryOrchestratorContainerException.md 'Microsoft.FactoryOrchestrator.Core.FactoryOrchestratorContainerException')  
&#8627; [FactoryOrchestratorTaskListRunningException](FactoryOrchestratorTaskListRunningException.md 'Microsoft.FactoryOrchestrator.Core.FactoryOrchestratorTaskListRunningException')  
&#8627; [FactoryOrchestratorUnkownGuidException](FactoryOrchestratorUnkownGuidException.md 'Microsoft.FactoryOrchestrator.Core.FactoryOrchestratorUnkownGuidException')  
&#8627; [FactoryOrchestratorXmlException](FactoryOrchestratorXmlException.md 'Microsoft.FactoryOrchestrator.Core.FactoryOrchestratorXmlException')  

| Constructors | |
| :--- | :--- |
| [FactoryOrchestratorException()](FactoryOrchestratorException_FactoryOrchestratorException().md 'Microsoft.FactoryOrchestrator.Core.FactoryOrchestratorException.FactoryOrchestratorException()') | Initializes a new instance of the [FactoryOrchestratorException](FactoryOrchestratorException.md 'Microsoft.FactoryOrchestrator.Core.FactoryOrchestratorException') class.<br/> |
| [FactoryOrchestratorException(string)](FactoryOrchestratorException_FactoryOrchestratorException(string).md 'Microsoft.FactoryOrchestrator.Core.FactoryOrchestratorException.FactoryOrchestratorException(string)') | Initializes a new instance of the [FactoryOrchestratorException](FactoryOrchestratorException.md 'Microsoft.FactoryOrchestrator.Core.FactoryOrchestratorException') class.<br/> |
| [FactoryOrchestratorException(string, Exception)](FactoryOrchestratorException_FactoryOrchestratorException(string_Exception).md 'Microsoft.FactoryOrchestrator.Core.FactoryOrchestratorException.FactoryOrchestratorException(string, System.Exception)') | Initializes a new instance of the [FactoryOrchestratorException](FactoryOrchestratorException.md 'Microsoft.FactoryOrchestrator.Core.FactoryOrchestratorException') class.<br/> |
| [FactoryOrchestratorException(string, Nullable&lt;Guid&gt;, Exception)](FactoryOrchestratorException_FactoryOrchestratorException(string_Nullable_Guid__Exception).md 'Microsoft.FactoryOrchestrator.Core.FactoryOrchestratorException.FactoryOrchestratorException(string, System.Nullable&lt;System.Guid&gt;, System.Exception)') | Constructor.<br/> |

| Properties | |
| :--- | :--- |
| [Guid](FactoryOrchestratorException_Guid.md 'Microsoft.FactoryOrchestrator.Core.FactoryOrchestratorException.Guid') | The GUID this Exception relates to. NULL if it is not related to a specific object.<br/> |
