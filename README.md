<!-- Copyright (c) Microsoft Corporation. -->
<!-- Licensed under the MIT license. -->

# [Factory Orchestrator](https://microsoft.github.io/FactoryOrchestrator/)

 [![Build Status](https://microsoft.visualstudio.com/OneCore/_apis/build/status/FactoryOrchestrator/FO-PublicFacing-CI?branchName=main)](https://microsoft.visualstudio.com/OneCore/_build/latest?definitionId=54749&branchName=main)

Built to allow device manufacturers and developers to focus more on their validation and calibration software, and less on how to run, audit, and manage the lifecycle of their programs, Factory Orchestrator is a .NET Core cross-platform system service for organizing, executing, and logging a set of executable scripts, binaries, or ["Tasks"](https://microsoft.github.io/FactoryOrchestrator/tasks-and-tasklists/) on a system. Factory Orchestrator tracks task information, including run unique per-run results and logging; even persisting task state to allow the service to be resilient to data loss due to system failure. Factory Orchestrator also provides an optional [client app](https://microsoft.github.io/FactoryOrchestrator/use-the-factory-orchestrator-app/) for Windows and a [robust client API surface](https://microsoft.github.io/FactoryOrchestrator/use-the-factory-orchestrator-api/) for clients to monitor & interact with the service via the App or C# .NET, C# UWP, or PowerShell code. The app and and any other client can communicate with any Factory Orchestrator service running on the same system and/or over a network to a remote [device under test (DUT)](https://en.wikipedia.org/wiki/Device_under_test)!

**Learn much more about this tool and how to use it by reading the [documentation on github.io](https://microsoft.github.io/FactoryOrchestrator/).**

## **Factory Orchestrator consists of the following projects and binary releases:**

* **Microsoft.FactoryOrchestrator.Core**
 A .NET Standard library containing the core FactoryOrchestrator classes. Required in all projects. This is available as ["Microsoft.FactoryOrchestrator.Core" on NuGet](https://www.nuget.org/packages/Microsoft.FactoryOrchestrator.Core/).

* **Microsoft.FactoryOrchestrator.Client**
 .NET Standard library containing the client-side FactoryOrchestrator classes, required to interact with Microsoft.FactoryOrchestrator.Service. Also contains optional helper classes. This is available as ["Microsoft.FactoryOrchestrator.Client" on NuGet](https://www.nuget.org/packages/Microsoft.FactoryOrchestrator.Client/).

* **Microsoft.FactoryOrchestrator.UWPClient**
 UWP library containing the client-side FactoryOrchestrator classes, required to interact with Microsoft.FactoryOrchestrator.Service from a Universal Windows Platform app. This is available as ["Microsoft.FactoryOrchestrator.UWPClient" on NuGet](https://www.nuget.org/packages/Microsoft.FactoryOrchestrator.UWPClient/).

* **Microsoft.FactoryOrchestrator.PowerShell**
PowerShell module containing a synchronous client-side FactoryOrchestrator class and Cmdlet wrapper classes, designed to be used WITH PowerShell 6+. This is available as ["Microsoft.FactoryOrchestrator.Client" on PowerShell Gallery](https://www.powershellgallery.com/packages/Microsoft.FactoryOrchestrator.Client/). 

* **Microsoft.FactoryOrchestrator.Server**
 A .NET Standard library containing the server-side FactoryOrchestrator classes. Required on all FactoryOrchestrator server projects. ["Microsoft.FactoryOrchestrator.Server" on NuGet](https://www.nuget.org/packages/Microsoft.FactoryOrchestrator.Server/).

* **Microsoft.FactoryOrchestrator.Service**
 .NET Core Executable project for Microsoft.FactoryOrchestrator.Service.exe, the FactoryOrchestrator server implementation. Requires administrator access to run. This is available on [the GitHub Releases page as a .zip](https://github.com/microsoft/FactoryOrchestrator/releases).

* **Microsoft.FactoryOrchestrator.App**
 C# UWP app project for Microsoft.FactoryOrchestrator.App.exe, the UWP provides a GUI (Graphical User Interface) to manually interact with Microsoft.FactoryOrchestrator.Service. This is available on [the GitHub Releases page as a .msixbundle](https://github.com/microsoft/FactoryOrchestrator/releases).

Factory Orchestrator :green_heart: OSS.

### **OSS Projects currently forked in source:**

* **IpcServiceFramework**

    FactoryOrchestrator forks the source of [IpcServiceFramework](https://github.com/jacqueskang/IpcServiceFramework). The fork is equivalent the latest IpcServiceFramework source as of December 2020, with only a project file change to set DisableDynamicCodeGeneration to true so it can be used in .NET Native applications.

* **Pe-Utility**

    FactoryOrchestrator minimally forks a portion of the source of [Pe-Utility](https://github.com/AndresTraks/pe-utility), to build it as a .NET Standard library and reduce the code complexity for FactoryOrchestrator's use case.

**Factory Orchestrator ```src``` directory structure:**

```
FactoryOrchestrator
└──oss
└──src
    |   App
    |   ClientLibrary
    |   ClientSample
    |   CoreLibrary
    |   PowerShellLibrary
    |   ServerLibrary
    |   Service
    |   Tests
    └   UWPClientLibrary
```

## Prerequisites to build source code

### Install dependencies
Building Factory Orchestrator source requires the [NET 5.0 SDK](https://dotnet.microsoft.com/download/dotnet/5.0), the [NET Core 3.1 runtime.](https://dotnet.microsoft.com/download/dotnet/3.1/runtime/), and [PowerShell 7+](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell) (or Windows PowerShell). If you wish to build the app as well, you also need the Universal Windows Platform Development (10.0.19041.0) SDK.

The easiest way to prepare to build the solutions is using any variation of [Visual Studio 2019+ (Enterprise, Community)](https://visualstudio.microsoft.com/vs/). In the installer, make sure you click the checkboxes for .NET Core cross-platform Development, and Universal Windows Platform Development (10.0.19041.0).

You can also use [Visual Studio Code](https://code.visualstudio.com/), or any whatever editor you prefer. Visual Studio provides a sleek [command-line installer](https://docs.microsoft.com/en-us/visualstudio/install/use-command-line-parameters-to-install-visual-studio) that can be used to just deploy the necessary dependencies using their [workload component id](https://docs.microsoft.com/en-us/visualstudio/install/workload-component-id-vs-community).

### Enable developer mode on Windows

https://docs.microsoft.com/en-us/windows/uwp/get-started/enable-your-device-for-development

## Other notes

* There are two Visual Studio .sln files in the src/ folder. Use src/FactoryOrchestratorNoApp.sln if you do not need to build the Windows app or are building on Linux.
* You may see IntelliSense errors before building Microsoft.FactoryOrchestrator.Core, as that project creates Autogenerated C# files used in other projects.

## Debugging
The service will not run properly unless it is run as administrator/sudo. Set ["DisableContainerSupport" to "true"](https://microsoft.github.io/FactoryOrchestrator/service-configuration/) on Windows if you see a frequent FactoryOrchestratorContainerException.

If you need to debug the app, run _Microsoft.FactoryOrchestrator.App (Universal Windows)_ and _Microsoft.FactoryOrchestrator.Service_ in separate Visual Studio 2019+ instances. This will allow the app and the service to communicate with each other.

## Versioning

Factory Orchestrator uses [semantic versioning (semver)](https://semver.org/). All Factory Orchestrator binaries from the same build share the same version; there is no unique client or service version. In the [src/common.props](src/common.props) file, increment the:

* MAJOR version when you make incompatible API changes,
* MINOR version when you add functionality in a backwards compatible manner.
* PATCH version when you make backwards compatible bug fixes.

When the MAJOR version diverges between a Client and Service, Clients will be prevented from connecting to the Service by default. Changing the signature of any Microsoft.FactoryOrchestrator.Core class is therefore usually a MAJOR version change and should be done sparingly.

## Code of Conduct

This project has adopted the Microsoft Open Source Code of Conduct. For more information see [CODE_OF_CONDUCT.md](./CODE_OF_CONDUCT.md) or contact opencode@microsoft.com with any additional questions or comments.

## Contributing

Accepting the Contributor Licence Agreement (CLA)

This project welcomes contributions and suggestions. Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit <https://cla.opensource.microsoft.com>.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repos using our CLA.

Happy Coding!

## Reporting Security Issues

Please refer to [SECURITY.md](./SECURITY.md).

## Open Source Software Acknowledgments

[IpcServiceFramework](https://github.com/jacqueskang/IpcServiceFramework) - Jacques Kang - [MIT License](https://github.com/jacqueskang/IpcServiceFramework/blob/develop/LICENSE)

[Pe-Utility](https://github.com/AndresTraks/pe-utility) - Andres Traks - [MIT License](https://github.com/AndresTraks/pe-utility/blob/master/LICENSE)

[WindowsDevicePortalWrapper (mgurlitz .NET Standard fork)](https://github.com/mgurlitz/WindowsDevicePortalWrapper/tree/feat-standard) - Microsoft Corporation and mgurlitz - [MIT License](https://github.com/mgurlitz/WindowsDevicePortalWrapper/blob/feat-standard/License.txt)

[DefaultDocumentation](https://github.com/Doraku/DefaultDocumentation) - Paillat Laszlo - [MIT No Attribution License](https://github.com/Doraku/DefaultDocumentation/blob/master/LICENSE.md)

[net-mdns/Makaretu.Dns](https://github.com/richardschneider/net-mdns) - Richard Schneider - [MIT License](https://github.com/richardschneider/net-mdns/blob/master/LICENSE)