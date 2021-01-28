#### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core').[TaskList](./Microsoft-FactoryOrchestrator-Core-TaskList.md 'Microsoft.FactoryOrchestrator.Core.TaskList')
## TaskList.ValidateTaskList() Method
Validates the TaskList is compliant with the XSD schema and other requirements.  
Will assign a random Guid to any Task missing one.  
```csharp
public void ValidateTaskList();
```
#### Exceptions
[System.Xml.Schema.XmlSchemaValidationException](https://docs.microsoft.com/en-us/dotnet/api/System.Xml.Schema.XmlSchemaValidationException 'System.Xml.Schema.XmlSchemaValidationException')  
If the TaskList has an issue  
