#### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
### [Microsoft.FactoryOrchestrator.Core.JSONConverters](./Microsoft-FactoryOrchestrator-Core-JSONConverters.md 'Microsoft.FactoryOrchestrator.Core.JSONConverters')
## NoConverter Class
NoConverter class is used children of abstract classes (ex: ExecutableTask), to prevent infinite loop.  
All serialization is done by the Abstract class converter (TaskBaseConverter)  
```csharp
public class NoConverter : JsonConverter
```
Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; [Newtonsoft.Json.JsonConverter](https://docs.microsoft.com/en-us/dotnet/api/Newtonsoft.Json.JsonConverter 'Newtonsoft.Json.JsonConverter') &#129106; NoConverter  
### Properties
- [CanRead](./Microsoft-FactoryOrchestrator-Core-JSONConverters-NoConverter-CanRead.md 'Microsoft.FactoryOrchestrator.Core.JSONConverters.NoConverter.CanRead')
- [CanWrite](./Microsoft-FactoryOrchestrator-Core-JSONConverters-NoConverter-CanWrite.md 'Microsoft.FactoryOrchestrator.Core.JSONConverters.NoConverter.CanWrite')
### Methods
- [CanConvert(System.Type)](./Microsoft-FactoryOrchestrator-Core-JSONConverters-NoConverter-CanConvert(System-Type).md 'Microsoft.FactoryOrchestrator.Core.JSONConverters.NoConverter.CanConvert(System.Type)')
- [ReadJson(Newtonsoft.Json.JsonReader, System.Type, object, Newtonsoft.Json.JsonSerializer)](./Microsoft-FactoryOrchestrator-Core-JSONConverters-NoConverter-ReadJson(Newtonsoft-Json-JsonReader_System-Type_object_Newtonsoft-Json-JsonSerializer).md 'Microsoft.FactoryOrchestrator.Core.JSONConverters.NoConverter.ReadJson(Newtonsoft.Json.JsonReader, System.Type, object, Newtonsoft.Json.JsonSerializer)')
- [WriteJson(Newtonsoft.Json.JsonWriter, object, Newtonsoft.Json.JsonSerializer)](./Microsoft-FactoryOrchestrator-Core-JSONConverters-NoConverter-WriteJson(Newtonsoft-Json-JsonWriter_object_Newtonsoft-Json-JsonSerializer).md 'Microsoft.FactoryOrchestrator.Core.JSONConverters.NoConverter.WriteJson(Newtonsoft.Json.JsonWriter, object, Newtonsoft.Json.JsonSerializer)')
