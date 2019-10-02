# Introduction 
Factory Orchestrator

Factory Orchestrator consists of the following projects:
1) FactoryOrchestratorCoreLibrary - A .NET Standard library containing the core FactoryOrchestrator classes. Required in all projects.
2) FactoryOrchestratorServerLibrary - A .NET Standard library containing the server-side FactoryOrchestrator classes. Required on all FactoryOrchestrator server projects.
3) FactoryOrchestratorClientLibrary - A .NET Standard library containing the client-side FactoryOrchestrator classes. Has helper classes which are optional for all FactoryOrchestrator client projects.
4) FactoryOrchestratorService - A .NET Core Executable project for FactoryOrchestratorService.exe, the FactoryOrchestrator server implementation.
5) FactoryOrchestratorApp - A .NET UWP app project for FactoryOrchestratorApp.exe, the UWP used to communicate with FactoryOrchestratorService and run UI tests.

This solution also contains an example .NET executable project which interacts with FactoryOrchestratorService (ClientSample), and a currently private fork of the IpcServiceFramework by Jacques Kang.

# Usage
FactoryOS images can automatically include the FactoryOrchestrator UWP and Service via the Microsoft.FactoryTestFrameworkUWP_8wekyb3d8bbwe optional AppX and FACTORY_TEST_FRAMEWORK_SERVICE Microsoft Feature.

If it does not have it installed, or you want to install a new version of the UWP:
1) Build appx package for target hardware. DO NOT enable .NET Native in the project settings.
2) Deploy appx and dependencies via Windows Device Portal.
3) (This only works if device is in State Separation development mode or this is done offline.) Register app to start on first boot by setting HKLM\Software\Microsoft\CoreShell\FactoryOS\DefaultApp to REG_SZ with value:
"microsoft.factorytestframeworkuwp.dev_8wekyb3d8bbwe!App" for any locally built UWP
- OR - 
"microsoft.factorytestframeworkuwp_8wekyb3d8bbwe!App" for any PackageES built UWP

If it does not have it installed, or you want to install a new version of the Service:
1) Publish FactoryOrchestratorService project for the desired architecture using Visual Studio
2) Copy all files under the publish folder to FactoryOS device
3) Add firewall rules (only needed for network communication, this is done automatically for the inbox FactoryOS FactoryOrchestratorService):
netsh advfirewall firewall add rule name=FactoryOrchestratorService_tcp_in program=<Path to FactoryOrchestratorService.exe> protocol=tcp dir=in enable=yes action=allow profile=public,private,domain
netsh advfirewall firewall add rule name=FactoryOrchestratorService_tcp_out program=<Path to FactoryOrchestratorService.exe> protocol=tcp dir=out enable=yes action=allow profile=public,private,domain
4) (If replacing the existing service) Run "sc delete FactoryOrchestratorService" 
5) (The created service entry only persists if device is in State Separation development mode.) Run "FactoryOrchestratorService.exe action:install name:FactoryOrchestratorService" 
6) Run "sc start FactoryOrchestratorService"

# Sample usage
An example of the test content folder to use for this is at: \\wexfs\users\jafriedm\TestConsole

Args must be in order: <IP ADDRESS> <TEST CONTENT FOLDER WITH FACTORYOCHESTRATORXML FILE(S)> <TARGET FOLDER ON DUT> <TARGET FOLDER ON PC TO SAVE LOGS>

# Build and Test
When building FactoryOrchestratorApp locally, DevPackage.appxmanifest is used instead of Package.appxmanifest. This causes the app to have a different PFN (Microsoft.FactoryTestFrameworkUWP.DEV_8wekyb3d8bbwe) and display name (Factory Orchestrator (DEV)).
This is done since you cannot replace the PackageES-built inbox FactoryOrchestratorApp application, as it is installed to the read-only preinstalled partition.

# Contribute
Please do not submit updates to the AssemblyInfo.cs files, they are autogenerated by the build. Run the following from the root of the git repo to prevent them from being tracked by git:
git update-index --assume-unchanged FactoryOrchestratorApp/Properties/AssemblyInfo.cs
git update-index --assume-unchanged FactoryOrchestratorService/Properties/AssemblyInfo.cs

If you want to learn more about creating good readme files then refer the following [guidelines](https://www.visualstudio.com/en-us/docs/git/create-a-readme). You can also seek inspiration from the below readme files:
- [ASP.NET Core](https://github.com/aspnet/Home)
- [Visual Studio Code](https://github.com/Microsoft/vscode)
- [Chakra Core](https://github.com/Microsoft/ChakraCore)

Troubleshooting:
1.	Restart the apps (WDP -> apps manager, kill FactoryOrchestratorApp then relaunch) and service (cmdd sc stop/start FactoryOrchestratorService) or reboot
2.	Make sure UWP local loopback is enabled on the device (only needed for on-device communication) with TSHELL or SSH via CheckNetIsolation.exe LoopbackExempt -s. If it isn't set, delete HKLM\System\CurrentControlSet\Control\FactoryOrchestrator\UWPLocalLoopbackEnabled and restart FactoryOrchestratorService.
3.  Make sure the firewall rules are configured (see Usage)


# Open Source Software
IpcServiceFramework - Jacques Kang - [MIT License](https://github.com/jacqueskang/IpcServiceFramework/blob/develop/LICENSE)
DotNetCore.WindowsService - Peter Kottas - [MIT License](https://github.com/PeterKottas/DotNetCore.WindowsService/blob/master/LICENSE)