# Factory Orchestrator Client API Overview
The Factory Orchestrator service, Microsoft.FactoryOrchestrator.Service.exe, provides a [robust API surface](../ClientLibrary/Microsoft-FactoryOrchestrator-Client) for clients to interact with test devices via C# .NET, C# UWP, or PowerShell code. You can use these APIs to author advanced task orchestration code to programmatically interact with the service outside of what the app provides. Like the app, you can connect to a service running either on the same device or a service running on a remote device available over the network.

All FactoryOrchestratorClient and FactoryOrchestratorUWPClient C# (.NET and UWP) API calls are [asynchronous](https://docs.microsoft.com/dotnet/csharp/async).

You can see [the full API reference for the Microsoft.FactoryOrchestrator.Client here](../ClientLibrary/Microsoft-FactoryOrchestrator-Client). You can also see [the Microsoft.FactoryOrchestrator.Core reference here](../CoreLibrary/Microsoft-FactoryOrchestrator-Core). The CoreLibrary contains class definitions for objects some client APIs return, such as TaskRun instances.

**See [Factory Orchestrator API usage samples](../factory-orchestrator-client-usage-samples) for code snippets that show how to perform various activities using the Factory Orchestrator client APIs.**

## Using the Factory Orchestrator client API in C# .NET
The recommended method to use the Factory Orchestrator C# client library in your .NET code is by adding a reference to the [Microsoft.FactoryOrchestrator.Client NuGet package](https://www.nuget.org/packages/Microsoft.FactoryOrchestrator.Client/) in your .NET project.

Before executing other APIs, the Connect() or TryConnect() API must be called. Once the Connect() or TryConnect() API succeeds, you can use all other APIs. All calls are asynchronous.

```csharp
// Create client instance targeting service at desired IP Address
var client = new FactoryOrchestratorClient(new IPAddress("192.168.0.50"));

// Establish connection.
await client.Connect();

// Do things!
await client.RunExecutable(@"%windir%\system32\ping.exe");
```

## Using FactoryOrchestratorClient in PowerShell
The FactoryOrchestratorClient PowerShell module is available on [PowerShell Gallery as Microsoft.FactoryOrchestrator.Client](https://www.powershellgallery.com/packages/Microsoft.FactoryOrchestrator.Client/). Currently, the module is only supported on PowerShell 6+.

To use the PowerShell module, install Microsoft.FactoryOrchestrator.Client and then use the New-FactoryOrchestratorClient cmdlet to create a [FactoryOrchestratorClient](.\ClientLibrary\Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient.md) instance. The PowerShell FactoryOrchestratorClient instance returned by New-FactoryOrchestratorClient has the exact same methods as the C# FactoryOrchestratorClient class. **However, unlike FactoryOrchestratorClient and FactoryOrchestratorUWPClient C# classes, all calls are synchronous.**

Other supported cmdlets are: New-FactoryOrchestratorTask, New-FactoryOrchestratorTaskList, and New-FactoryOrchestratorServerPoller. They return new [Task](../CoreLibrary/Microsoft-FactoryOrchestrator-Core-TaskBase.md), [TaskList](../CoreLibrary/Microsoft-FactoryOrchestrator-Core-TaskList.md), and [ServerPoller](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-ServerPoller.md) objects respectively.

Below is a sample PowerShell script showing how you can use these cmdlets:
```powershell

# Install client module
Install-Module -Name Microsoft.FactoryOrchestrator.Client

# Create client instance targeting service at desired IP Address (127.0.0.1 == loopback)
$client = New-FactoryOrchestratorClient -IpAddress "127.0.0.1"

# Establish connection. Unlike C# FactoryOrchestratorClient, this is synchronous
$client.Connect();

# Do things!
$client.RunExecutable("$env:windir\system32\ping.exe");
```

## Using FactoryOrchestratorUWPClient in a UWP
The recommended method to use the Factory Orchestrator C# UWP client library in your .NET code is by adding a reference to the [Microsoft.FactoryOrchestrator.UWPClient NuGet package](https://www.nuget.org/packages/Microsoft.FactoryOrchestrator.UWPClient/) in your UWP project.

If you are writing a UWP app that uses the Factory Orchestrator Client API, you must use the FactoryOrchestratorUWPClient class instead of FactoryOrchestratorClient. The FactoryOrchestratorUWPClient APIs are identical to the FactoryOrchestratorClient APIs. Like the FactoryOrchestratorClient C# class, all FactoryOrchestratorUWPClient API calls are [asynchronous](https://docs.microsoft.com/dotnet/csharp/async).

## Factory Orchestrator Versioning
Factory Orchestator uses [semver](https://semver.org/) versioning. If there is a major version mismatch between the client and service your program may not work as expected and either the client program or target service should be updated so the major versions match. Major versions are checked when the client connects to the service via the Connect() API. You can also manually check the version of the client API by:

- Manually inspecting the properties of the Microsoft.FactoryOrchestrator.Client.dll file used by your program

    ![version number in the properties of Microsoft.FactoryOrchestrator.Client.dll](./images/fo-version-number.png)

- Programmatically with the following code snippets:

```C#
FactoryOrchestratorClient.GetClientVersionString();
```
```powershell
Get-Module FactoryOrchestratorClient
```