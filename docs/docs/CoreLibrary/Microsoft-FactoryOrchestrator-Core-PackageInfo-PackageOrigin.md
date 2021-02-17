#### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core')
### [Microsoft.FactoryOrchestrator.Core](./Microsoft-FactoryOrchestrator-Core.md 'Microsoft.FactoryOrchestrator.Core').[PackageInfo](./Microsoft-FactoryOrchestrator-Core-PackageInfo.md 'Microsoft.FactoryOrchestrator.Core.PackageInfo')
## PackageInfo.PackageOrigin Property
Gets package origin, a measure of how the app was installed.   
PackageOrigin_Unknown            = 0,  
PackageOrigin_Unsigned           = 1,  
PackageOrigin_Inbox              = 2,  
PackageOrigin_Store              = 3,  
PackageOrigin_DeveloperUnsigned  = 4,  
PackageOrigin_DeveloperSigned    = 5,  
PackageOrigin_LineOfBusiness     = 6  
```csharp
public int PackageOrigin { get; set; }
```
#### Property Value
[System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')  
