# Factory Orchestrator

| CI Build   |
|----|
|  [![Build Status](https://microsoft.visualstudio.com/OneCore/_apis/build/status/FactoryOrchestrator/FO-PublicFacing-CI?branchName=main)](https://microsoft.visualstudio.com/OneCore/_build/latest?definitionId=54749&branchName=main)  |

## Introduction

Welcome to the source repository for Factory Orchestrator. If you want to learn more this tool and how to use it then take a look at our [documentation here](https://microsoft.github.io/FactoryOrchestrator/). The [getting started](https://microsoft.github.io/FactoryOrchestrator/get-started-with-factory-orchestrator/) page would be a great place to go once you've built the binaries for the project.

### **Factory Orchestrator consists of the following projects:**

* **FactoryOrchestratorCoreLibrary**
 A .NET Standard library containing the core FactoryOrchestrator classes. Required in all projects.

* **FactoryOrchestratorServerLibrary**
 A .NET Standard library containing the server-side FactoryOrchestrator classes. Required on all FactoryOrchestrator server projects.

* **FactoryOrchestratorService**
 .NET Core Executable project for FactoryOrchestratorService.exe, the FactoryOrchestrator server implementation. Requires administrator access to run.

* **FactoryOrchestratorClientLibrary**
 .NET Standard library containing the client-side FactoryOrchestrator classes, required to interact with FactoryOrchestratorService. Also contains optional helper classes.

* **FactoryOrchestratorApp**
 C# UWP app project for FactoryOrchestratorApp.exe, the UWP provides a GUI (Graphical User Interface) to manually interact with FactoryOrchestratorService.

Factory Orchestrator :green_heart: OSS.

### **OSS Projects currently consumes as source:**

* **IpcServiceFramework**

FactoryOrchestrator forks the source of [IpcServiceFramework](https://github.com/jacqueskang/IpcServiceFramework). At this time, we are actively working on integrating our version of IpcServiceFramework with the [official IpcServiceFramework repo](https://github.com/jacqueskang/IpcServiceFramework), removing the need for this fork.

* **Pe-Utility**

FactoryOrchestrator minimally forks a portion of the source of [Pe-Utility](https://github.com/AndresTraks/pe-utility), to build it as a .NET Standard library and reduce the code complexity for FactoryOrchestrator's use case.

**Factory Orchestrator src directory structure:

```
FactoryOrchestrator
└──Src
    |   oss
    |   ServerLibrary
    |   Service
    |   Tests
    └   UWPClientLibrary
```

## Developer Documentation

### Setup

1. Accept Contributor Licence Agreement (CLA)
 This project welcomes contributions and suggestions. Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit <https://cla.opensource.microsoft.com>.

 When you submit a pull request, a CLA bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repos using our CLA.

 This project has adopted the Microsoft Open Source Code of Conduct. For more information see the Code of Conduct FAQ or contact opencode@microsoft.com with any additional questions or comments.
2. Install Visual Studio
 Any variation of [Visual Studio 2019+ (Enterprise, Community)](https://visualstudio.microsoft.com/vs/) is fine. In the installer, make sure you click the checkboxes for .NET Core cross-platform Development, and Universal Windows Platform Development (10.0.17763.0).

 Other development environments such as [Visual Studio Code](https://code.visualstudio.com/) are acceptable if you are not making FactoryOrchestratorApp changes.
3. Clone the repository
4. Enable developer mode on Windows
 <https://docs.microsoft.com/en-us/windows/uwp/get-started/enable-your-device-for-development>
5. Address Unsigned Powershell Scripts
   FactoryOrchestrator contains a series of unsigned powershell scripts. Windows security measures prevent unsigned scripts from executing. In order to develop on FactoryOrchestrator, you need to do one of two things.
    1. Self-sign
    More information on how to do this can be found here: <https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_signing?view=powershell-7>
    2. Set the Execution Policy to Unrestricted
    This method is not recommended. Setting the Execution Policy to Unresricted allows any powershell script to run.
    Documentation on Execution Policy:
    <https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.security/set-executionpolicy?view=powershell-7>

### Building

When building the source code, keep in mind that certain actions only occur when the build is run as part of an Azure DevOps Pipeline or when run in the "Release" configuration locally. For example, [DocFX documentation](https://dotnet.github.io/docfx/) for the Core and Client Libraries is only generated when the build is run in in one of those two modes. These actions are skipped in Debug builds to increase inner-dev-loop speed.

### Debugging

Run _FactoryOrchestratorApp (Universal Windows)_ and _FactoryOrchestratorService_ in separate Visual Studio 2019+ instances. This will allow the app and the service to communicate with each other.
Even if you are coding for just the app, you generally need to run the service so the app has something to connect to. The service must be run as administrator.

At the moment, running either the app or service under a debugger will cause exceptions to be thrown related to certain registry keys (ex: GetOEMVersionString()), but all exceptions are caught and will not result in a crash. This is because they try to access registry keys specific to a Microsoft internal product. You can routinely skip over these exceptions when they occur or disable them.

## Versioning

Factory Orchestrator uses a slightly modified form of [semver versioning](https://semver.org/). All Factory Orchestrator binaries from the same build share the same version; there is no unique client or service version. In the [src/custom.props](src/custom.props) file, increment the:

* MAJOR version when you make incompatible API changes,
* MINOR version when you add functionality in a backwards compatible manner.

The PATCH version is automatically set by the build, and is based on the [date/time the build is run](build/SetSourceVersion.ps1). The PATCH version is only changed when the build is run as part of an Azure Pipeline or when run in the "Release" configuration locally.

When the MAJOR version diverges between a Client and Service, Clients will be prevented from connecting to the Service by default. Changing the signiture of any FactoryOrchestratorCoreLibrary class is therefore usually a MAJOR version change and should be done sparingly.

Happy Coding!

## Reporting Security Issues

Please refer to [SECURITY.md](./SECURITY.md).

## Open Source Software Acknowledgments

[IpcServiceFramework](https://github.com/jacqueskang/IpcServiceFramework) - Jacques Kang - [MIT License](https://github.com/jacqueskang/IpcServiceFramework/blob/develop/LICENSE)

[DotNetCore.WindowsService](https://github.com/PeterKottas/DotNetCore.WindowsService) - Peter Kottas - [MIT License](https://github.com/PeterKottas/DotNetCore.WindowsService/blob/master/LICENSE)

[Pe-Utility](https://github.com/AndresTraks/pe-utility) - Andres Traks - [MIT License](https://github.com/AndresTraks/pe-utility/blob/master/LICENSE)

[WindowsDevicePortalWrapper (mgurlitz .NET Standard fork)](https://github.com/mgurlitz/WindowsDevicePortalWrapper/tree/feat-standard) - Microsoft Corporation and mgurlitz - [MIT License](https://github.com/mgurlitz/WindowsDevicePortalWrapper/blob/feat-standard/License.txt)
