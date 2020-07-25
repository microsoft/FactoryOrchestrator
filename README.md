| CI Build   | 
|----|
|  [![Build Status](https://microsoft.visualstudio.com/OneCore/_apis/build/status/FactoryOrchestrator/FO-PublicFacing-CI?branchName=master)](https://microsoft.visualstudio.com/OneCore/_build/latest?definitionId=54749&branchName=main)  |   

# Introduction 
Factory Orchestrator is a tool for built for Original Equipment Maufacturers to aid in the manufacturing of Windows devices.
Factory Orchestrator consists of the following projects:
## FactoryOrchestratorCoreLibrary 
.NET Standard library containing the core FactoryOrchestrator classes. Required in all projects.
## FactoryOrchestratorServerLibrary
A .NET Standard library containing the server-side FactoryOrchestrator classes. Required on all FactoryOrchestrator server projects.
## FactoryOrchestratorService 
.NET Core Executable project for FactoryOrchestratorService.exe, the FactoryOrchestrator server implementation. Requires administrator access to run.
## FactoryOrchestratorClientLibrary 
.NET Standard library containing the client-side FactoryOrchestrator classes, required to interact with FactoryOrchestratorService. Also contains optional helper classess.
## FactoryOrchestratorApp 
C# UWP app project for FactoryOrchestratorApp.exe, the UWP used to manually interact with FactoryOrchestratorService via GUI.
## Open Source Components
### IpcServiceFramework
FactoryOrchestrator forks the source of [IpcServiceFramework](https://github.com/jacqueskang/IpcServiceFramework). At this time, we are actively working on integrating our version of IpcServiceFramework with the [official IpcServiceFramework repo](https://github.com/jacqueskang/IpcServiceFramework), removing the need for this fork.

# Contributing

## 1. Accept Contributor Licence Agreement (CLA)
This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## 2. Prerequisite steps
### Install Visual Studio 
Any variation of [Visual Studio 2019+ (Enterprise, Community)](https://visualstudio.microsoft.com/vs/) is fine. In the installer, make sure you click the checkboxes for .NET Core cross-platform Development, and Universal Windows Platform Development (10.0.17763.0). 

Visual Studio Code is acceptible if you only are making server/service changes.

### Clone the repository

### Enable developer mode on Windows 
https://docs.microsoft.com/en-us/windows/uwp/get-started/enable-your-device-for-development

### Address Unsigned Powershell Scripts
FactoryOrchestrator contains a series of unsigned powershell scripts. Windows security measures prevent unsigned scripts from executing. In order to develop on FactoryOrchestrator, you need to do one of two things.

#### Self-sign
More information on how to do this can be found here: https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_signing?view=powershell-7

#### Set the Execution Policy to Unrestricted
This method is not recommended. Setting the Execution Policy to Unresricted allows any powershell script to run. 
Documentation on Execution Policy:
https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.security/set-executionpolicy?view=powershell-7

## 3. Building
When building the source code, keep in mind that certain actions only occur when the build is run as part of an Azure DevOps Pipeline or when run in the "Release" configuration locally. For example, [DocFX documentation](https://dotnet.github.io/docfx/) for the Core and Client Libraries is only generated when the build is run in in one of those two modes. These actions are skipped in Debug builds to increase inner-dev-loop speed.

## 4. Debugging
Run _FactoryOrchestratorApp (Universal Windows)_ and _FactoryOrchestratorService_ in separate Visual Studio 2019+ instances. This will allow the app and the service to communicate with each other. 
Even if you are coding for just the app, you generally need to run the service so the app has something to connect to. The service must be run as administrator.

At the moment, running either the app or service under a debugger will cause exceptions to be thrown related to certain registry keys (ex: GetOEMVersionString()), but all exceptions are caught and will not result in a crash. This is because they try to access registry keys specific to a Microsoft internal product. You can routinely skip over these exceptions when they occur or disable them.

## 5. Versioning
Factory Orchestrator uses a slightly modified form of [semver versioning](https://semver.org/). All Factory Orchestrator binaries from the same build share the same version; there is no unique client or service version. In the [src/custom.props](src/custom.props) file, increment the:

- MAJOR version when you make incompatible API changes,
- MINOR version when you add functionality in a backwards compatible manner.

The PATCH version is automatically set by the build, and is based on the date/time the build is run. The PATCH version is only changed when the build is run as part of an Azure DevOps Pipeline or when run in the "Release" configuration locally.

When the MAJOR version diverges between a Client and Service, Clients will be prevented from connecting to the Service by default. Changing the signiture of any FactoryOrchestratorCoreLibrary class is therefore usually a MAJOR version change and should be done sparingly.


Happy Coding!

# Reporting Security Issues
Please refer to [SECURITY.md](./SECURITY.md).

# License
Copyright (c) Microsoft Corporation. All rights reserved.

Licensed under the [MIT License](./LICENSE).

# Open Source Software Acknowledgments
[IpcServiceFramework](https://github.com/jacqueskang/IpcServiceFramework) - Jacques Kang - [MIT License](https://github.com/jacqueskang/IpcServiceFramework/blob/develop/LICENSE)

[DotNetCore.WindowsService](https://github.com/PeterKottas/DotNetCore.WindowsService) - Peter Kottas - [MIT License](https://github.com/PeterKottas/DotNetCore.WindowsService/blob/master/LICENSE)