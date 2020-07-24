| CI Build   | 
|----|
|  [![Build Status](https://microsoft.visualstudio.com/OneCore/_apis/build/status/FactoryOrchestrator/FO-PublicFacing-CI?branchName=master)](https://microsoft.visualstudio.com/OneCore/_build/latest?definitionId=54749&branchName=main)  |   

# Introduction 
Factory Orchestrator is a tool for built for Original Equipment Maufacturers to aid in the manufacturing of Windows devices.
Factory Orchestrator consists of the following projects
## 1. FactoryOrchestratorCoreLibrary 
.NET Standard library containing the core FactoryOrchestrator classes. Required in all projects.
## 2.	FactoryOrchestratorServerLibrary
A .NET Standard library containing the server-side FactoryOrchestrator classes. Required on all FactoryOrchestrator server projects.
## 3.	FactoryOrchestratorClientLibrary 
.NET Standard library containing the client-side FactoryOrchestrator classes. Has helper classes which are optional for all FactoryOrchestrator client projects.
## 4.	FactoryOrchestratorService 
.NET Core Executable project for FactoryOrchestratorService.exe, the FactoryOrchestrator server implementation.
## 5.	FactoryOrchestratorApp 
.NET UWP app project for FactoryOrchestratorApp.exe, the UWP used to communicate with FactoryOrchestratorService and run UI tests.
## Open Source Component
### IpcServiceFramework
https://github.com/jacqueskang/IpcServiceFramework
FactoryOrchestrator forks the source of IpcServiceFramework. At this time, we are actively working on integrating our version of IpcServiceFramework the official IpcServiceFramework repo, removing the need for this fork.

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
Any variation of Visual Studio (Enterprise, Community) is fine. 
https://visualstudio.microsoft.com/vs/
In the installer, make sure you click the checkboxes for .NET Dekstop Development, and Universal Windows Platform Development. 

### Clone the repository

### Enable developer mode on Windows 
https://docs.microsoft.com/en-us/windows/uwp/get-started/enable-your-device-for-development

### Address Unsigned Powershell Scripts
FactoryOrchestrator contains a series of unsigned powershell scripts. Windows security measures prevent unsigned scripts from executing. In order to develop on FactoryOrchestrator, you need to do one of two things.

#### Self-sign
More information on how to do this can be found here: https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_signing?view=powershell-7

#### Set the Execution Policy to Unrestricted
This method is not recommended, but is doable. Setting the Execution Policy to Unresricted allows any powershell script to run. 
Documentation on Execution Policy:
https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.security/set-executionpolicy?view=powershell-7

## 3. When Developing
Run _FactoryOrchestratorApp (Universal Windows)_ and _FactoryOrchestratorService_ in separate Visual Studio App instances. This will allow the app and the service to communicate with each other. 
Even if you are coding for just the app, you need to run the service so the app has something to connect to.

At the moment, running either piece will cause exceptions to be thrown. This is because they try to access resources specific to FactoryOS (FactoryOrchestrator's intended OS). You can routinely skip over these exceptions or disable them. 

Happy Coding!

# Open Source Software We Use
IpcServiceFramework - Jacques Kang - [MIT License](https://github.com/jacqueskang/IpcServiceFramework/blob/develop/LICENSE)
DotNetCore.WindowsService - Peter Kottas - [MIT License](https://github.com/PeterKottas/DotNetCore.WindowsService/blob/master/LICENSE)

