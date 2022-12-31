### [Microsoft.FactoryOrchestrator.Core](Microsoft_FactoryOrchestrator_Core.md 'Microsoft.FactoryOrchestrator.Core')
## PackageInfo Class
object representing the package information  
```csharp
public class PackageInfo
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; PackageInfo  

| Properties | |
| :--- | :--- |
| [AppId](PackageInfo_AppId.md 'Microsoft.FactoryOrchestrator.Core.PackageInfo.AppId') | Gets package relative Id<br/> |
| [FamilyName](PackageInfo_FamilyName.md 'Microsoft.FactoryOrchestrator.Core.PackageInfo.FamilyName') | Gets package family name<br/> |
| [FullName](PackageInfo_FullName.md 'Microsoft.FactoryOrchestrator.Core.PackageInfo.FullName') | Gets package full name<br/> |
| [Name](PackageInfo_Name.md 'Microsoft.FactoryOrchestrator.Core.PackageInfo.Name') | Gets package name<br/> |
| [PackageOrigin](PackageInfo_PackageOrigin.md 'Microsoft.FactoryOrchestrator.Core.PackageInfo.PackageOrigin') | Gets package origin, a measure of how the app was installed. <br/>PackageOrigin_Unknown            = 0,<br/>PackageOrigin_Unsigned           = 1,<br/>PackageOrigin_Inbox              = 2,<br/>PackageOrigin_Store              = 3,<br/>PackageOrigin_DeveloperUnsigned  = 4,<br/>PackageOrigin_DeveloperSigned    = 5,<br/>PackageOrigin_LineOfBusiness     = 6<br/> |
| [Publisher](PackageInfo_Publisher.md 'Microsoft.FactoryOrchestrator.Core.PackageInfo.Publisher') | Gets package publisher<br/> |
| [Version](PackageInfo_Version.md 'Microsoft.FactoryOrchestrator.Core.PackageInfo.Version') | Gets package version<br/> |

| Methods | |
| :--- | :--- |
| [IsSideloaded()](PackageInfo_IsSideloaded().md 'Microsoft.FactoryOrchestrator.Core.PackageInfo.IsSideloaded()') | Helper method to determine if the app was sideloaded and therefore can be used with e.g. GetFolderContentsAsync<br/> |
| [ToString()](PackageInfo_ToString().md 'Microsoft.FactoryOrchestrator.Core.PackageInfo.ToString()') | Get a string representation of the package<br/> |