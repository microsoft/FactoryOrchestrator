#### [FactoryOrchestratorCoreLibrary](./FactoryOrchestratorCoreLibrary.md 'FactoryOrchestratorCoreLibrary')
### [Microsoft.FactoryOrchestrator.Core.JSONConverters](./Microsoft-FactoryOrchestrator-Core-JSONConverters.md 'Microsoft.FactoryOrchestrator.Core.JSONConverters').[TaskBaseConverter](./Microsoft-FactoryOrchestrator-Core-JSONConverters-TaskBaseConverter.md 'Microsoft.FactoryOrchestrator.Core.JSONConverters.TaskBaseConverter')
## TaskBaseConverter.ReadJson(Newtonsoft.Json.JsonReader, System.Type, object, Newtonsoft.Json.JsonSerializer) Method
Reads the JSON representation of the object.  
```csharp
public override object ReadJson(Newtonsoft.Json.JsonReader reader, System.Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer);
```
#### Parameters
<a name='Microsoft-FactoryOrchestrator-Core-JSONConverters-TaskBaseConverter-ReadJson(Newtonsoft-Json-JsonReader_System-Type_object_Newtonsoft-Json-JsonSerializer)-reader'></a>
`reader` [Newtonsoft.Json.JsonReader](https://docs.microsoft.com/en-us/dotnet/api/Newtonsoft.Json.JsonReader 'Newtonsoft.Json.JsonReader')  
The [Newtonsoft.Json.JsonReader](https://docs.microsoft.com/en-us/dotnet/api/Newtonsoft.Json.JsonReader 'Newtonsoft.Json.JsonReader') to read from.  
  
<a name='Microsoft-FactoryOrchestrator-Core-JSONConverters-TaskBaseConverter-ReadJson(Newtonsoft-Json-JsonReader_System-Type_object_Newtonsoft-Json-JsonSerializer)-objectType'></a>
`objectType` [System.Type](https://docs.microsoft.com/en-us/dotnet/api/System.Type 'System.Type')  
Type of the object.  
  
<a name='Microsoft-FactoryOrchestrator-Core-JSONConverters-TaskBaseConverter-ReadJson(Newtonsoft-Json-JsonReader_System-Type_object_Newtonsoft-Json-JsonSerializer)-existingValue'></a>
`existingValue` [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object')  
The existing value of object being read.  
  
<a name='Microsoft-FactoryOrchestrator-Core-JSONConverters-TaskBaseConverter-ReadJson(Newtonsoft-Json-JsonReader_System-Type_object_Newtonsoft-Json-JsonSerializer)-serializer'></a>
`serializer` [Newtonsoft.Json.JsonSerializer](https://docs.microsoft.com/en-us/dotnet/api/Newtonsoft.Json.JsonSerializer 'Newtonsoft.Json.JsonSerializer')  
The calling serializer.  
  
#### Returns
[System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object')  
The object value.  
#### Exceptions
[FactoryOrchestratorException](./Microsoft-FactoryOrchestrator-Core-FactoryOrchestratorException.md 'Microsoft.FactoryOrchestrator.Core.FactoryOrchestratorException')  
Trying to deserialize an unknown task type!  
