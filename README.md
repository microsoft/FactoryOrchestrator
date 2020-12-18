# [Factory Orchestrator](https://microsoft.github.io/FactoryOrchestrator/)

 [![Build Status](https://microsoft.visualstudio.com/OneCore/_apis/build/status/FactoryOrchestrator/FO-PublicFacing-CI?branchName=main)](https://microsoft.visualstudio.com/OneCore/_build/latest?definitionId=54749&branchName=main)

Factory Orchestrator provides a simple and reliable way to run and manage factory line validation and fault analysis workflows. Beyond the factory floor Factory Orchestrator can be during os and hardware development to support various developer inner-loop and diagnostics activities.

Learn more about this tool and how to use it by reading the [documentation here](https://microsoft.github.io/FactoryOrchestrator/).

The [getting started](https://microsoft.github.io/FactoryOrchestrator/get-started-with-factory-orchestrator/) page is a great place to go once you've built the binaries for the project.

## **Factory Orchestrator consists of the following projects:**

* **Microsoft.FactoryOrchestrator.Core**
 A .NET Standard library containing the core FactoryOrchestrator classes. Required in all projects.

* **Microsoft.FactoryOrchestrator.Server**
 A .NET Standard library containing the server-side FactoryOrchestrator classes. Required on all FactoryOrchestrator server projects.

* **Microsoft.FactoryOrchestrator.Service**
 .NET Core Executable project for Microsoft.FactoryOrchestrator.Service.exe, the FactoryOrchestrator server implementation. Requires administrator access to run.

* **Microsoft.FactoryOrchestrator.Client**
 .NET Standard library containing the client-side FactoryOrchestrator classes, required to interact with Microsoft.FactoryOrchestrator.Service. Also contains optional helper classes.

* **FactoryOrchestratorPowerShellLibrary**
 .NET Standard library containing a synchronous client-side FactoryOrchestrator class and Cmdlet wrapper classes, designed to be used via PowerShell.

* **Microsoft.FactoryOrchestrator.App**
 C# UWP app project for Microsoft.FactoryOrchestrator.App.exe, the UWP provides a GUI (Graphical User Interface) to manually interact with Microsoft.FactoryOrchestrator.Service.

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
    |   ServerLibrary
    |   Service
    |   Tests
    └   UWPClientLibrary
```

## Prerequisites

### Install dependencies

The easiest way to open the project is using any variation of [Visual Studio 2019+ (Enterprise, Community)](https://visualstudio.microsoft.com/vs/). In the installer, make sure you click the checkboxes for .NET Core cross-platform Development, and Universal Windows Platform Development (10.0.17763.0).

You can also use [Visual Studio Code](https://code.visualstudio.com/), or any whatever editor you prefer. Visual Studio provides a sleek [command-line installer](https://docs.microsoft.com/en-us/visualstudio/install/use-command-line-parameters-to-install-visual-studio) that can be used to just deploy the necessary dependencies using their [workload component id](https://docs.microsoft.com/en-us/visualstudio/install/workload-component-id-vs-community).

### Enable developer mode on Windows

https://docs.microsoft.com/en-us/windows/uwp/get-started/enable-your-device-for-development

### Addressing Unsigned Powershell Scripts
FactoryOrchestrator contains a series of unsigned powershell scripts. Windows security measures prevent unsigned scripts from executing. In order to develop on FactoryOrchestrator, you need to do one of two things.

1. Self-sign the scripts

   More information on how to do this can be found here: https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_signing?view=powershell-7

2. Set the Execution Policy to Unrestricted
   
    This method is not recommended. Setting the Execution Policy to Unresricted allows any powershell script to run. 

    Documentation on Execution Policy:
https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.security/set-executionpolicy?view=powershell-7

## Other notes
* When building the source code, keep in mind that certain actions only occur when the build is run as part of an Azure DevOps Pipeline or when run in the "Release" configuration locally. For example, [DocFX documentation](https://dotnet.github.io/docfx/) for the Core and Client Libraries is only generated when the build is run in in one of those two modes. These actions are skipped in Debug builds to increase inner-dev-loop speed.

* You may see IntelliSense errors before building Microsoft.FactoryOrchestrator.Core, as that project creates Autogenerated C# files used in other projects.

## Debugging

Run _Microsoft.FactoryOrchestrator.App (Universal Windows)_ and _Microsoft.FactoryOrchestrator.Service_ in separate Visual Studio 2019+ instances. This will allow the app and the service to communicate with each other.

## Versioning

Factory Orchestrator uses [semantic versioning (semver)](https://semver.org/). All Factory Orchestrator binaries from the same build share the same version; there is no unique client or service version. In the [src/common.props](src/common.props) file, increment the:

* MAJOR version when you make incompatible API changes,
* MINOR version when you add functionality in a backwards compatible manner.
* PATCH version when you make backwards compatible bug fixes.

When the MAJOR version diverges between a Client and Service, Clients will be prevented from connecting to the Service by default. Changing the signiture of any Microsoft.FactoryOrchestrator.Core class is therefore usually a MAJOR version change and should be done sparingly.

## Code of Conduct

 This project has adopted the Microsoft Open Source Code of Conduct. For more information see the Code of Conduct FAQ or contact opencode@microsoft.com with any additional questions or comments.

## Contributing

 Accepting the Contributor Licence Agreement (CLA)

 This project welcomes contributions and suggestions. Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit <https://cla.opensource.microsoft.com>.

 When you submit a pull request, a CLA bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repos using our CLA.

Happy Coding!

## Reporting Security Issues

Please refer to [SECURITY.md](./SECURITY.md).

## Open Source Software Acknowledgments

[IpcServiceFramework](https://github.com/jacqueskang/IpcServiceFramework) - Jacques Kang - [MIT License](https://github.com/jacqueskang/IpcServiceFramework/blob/develop/LICENSE)

[DotNetCore.WindowsService](https://github.com/PeterKottas/DotNetCore.WindowsService) - Peter Kottas - [MIT License](https://github.com/PeterKottas/DotNetCore.WindowsService/blob/master/LICENSE)

[Pe-Utility](https://github.com/AndresTraks/pe-utility) - Andres Traks - [MIT License](https://github.com/AndresTraks/pe-utility/blob/master/LICENSE)

[WindowsDevicePortalWrapper (mgurlitz .NET Standard fork)](https://github.com/mgurlitz/WindowsDevicePortalWrapper/tree/feat-standard) - Microsoft Corporation and mgurlitz - [MIT License](https://github.com/mgurlitz/WindowsDevicePortalWrapper/blob/feat-standard/License.txt)
