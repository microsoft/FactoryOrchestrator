#### [FactoryOrchestratorCoreLibrary](./FactoryOrchestratorCoreLibrary.md 'FactoryOrchestratorCoreLibrary')
### [Microsoft.FactoryOrchestrator.Core.JSONConverters](./Microsoft-FactoryOrchestrator-Core-JSONConverters.md 'Microsoft.FactoryOrchestrator.Core.JSONConverters').[TaskBaseConverter](./Microsoft-FactoryOrchestrator-Core-JSONConverters-TaskBaseConverter.md 'Microsoft.FactoryOrchestrator.Core.JSONConverters.TaskBaseConverter')
## TaskBaseConverter.WriteJson(Newtonsoft.Json.JsonWriter, object, Newtonsoft.Json.JsonSerializer) Method
Writes the JSON representation of the object.  
```csharp
public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Core-JSONConverters-TaskBaseConverter-WriteJson(Newtonsoft-Json-JsonWriter_object_Newtonsoft-Json-JsonSerializer)-writer'></a>
`writer` [Newtonsoft.Json.JsonWriter](https://docs.microsoft.com/en-us/dotnet/api/Newtonsoft.Json.JsonWriter 'Newtonsoft.Json.JsonWriter')  
The [Newtonsoft.Json.JsonWriter](https://docs.microsoft.com/en-us/dotnet/api/Newtonsoft.Json.JsonWriter 'Newtonsoft.Json.JsonWriter') to write to.  
  
<a name='Microsoft-FactoryOrchestrator-Core-JSONConverters-TaskBaseConverter-WriteJson(Newtonsoft-Json-JsonWriter_object_Newtonsoft-Json-JsonSerializer)-value'></a>
`value` [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object')  
The value.  
  
<a name='Microsoft-FactoryOrchestrator-Core-JSONConverters-TaskBaseConverter-WriteJson(Newtonsoft-Json-JsonWriter_object_Newtonsoft-Json-JsonSerializer)-serializer'></a>
`serializer` [Newtonsoft.Json.JsonSerializer](https://docs.microsoft.com/en-us/dotnet/api/Newtonsoft.Json.JsonSerializer 'Newtonsoft.Json.JsonSerializer')  
The calling serializer.  
  
#### Exceptions
[FactoryOrchestratorException](./Microsoft-FactoryOrchestrator-Core-FactoryOrchestratorException.md 'Microsoft.FactoryOrchestrator.Core.FactoryOrchestratorException')  
Trying to serialize an unknown task type  
