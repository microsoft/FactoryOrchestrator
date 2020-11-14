# Using the Factory Orchestrator client API
The Factory Orchestrator service, FactoryOrchestratorService.exe, provides a [robust API surface](./ClientLibrary/FactoryOrchestratorClientLibrary.md) for clients to interact with test devices via C# .NET, C# UWP, or PowerShell code. You can use these APIs to author advanced task orchestration code to programmatically interact with the service outside of what the app provides. Like the app, you can connect to a service running either on the same device or a service running on a remote device available over the network.

All FactoryOrchestratorClient and FactoryOrchestratorUWPClient C# (.NET and UWP) API calls are [asynchronous](https://docs.microsoft.com/dotnet/csharp/async).

You can see [the full API reference for the FactoryOrchestratorClientLibrary here](./ClientLibrary/FactoryOrchestratorClientLibrary.md). You can also see [the FactoryOrchestratorCoreLibrary reference here](./CoreLibrary/FactoryOrchestratorCoreLibrary.md). The CoreLibrary contains class definitions for objects some client APIs return, such as TaskRun instances.

Before executing other APIs, the Connect() or TryConnect() API must be called. Once the Connect() or TryConnect() API succeeds, you can use all other APIs.

```csharp
// Create client instance targeting service at desired IP Address
var client = new FactoryOrchestratorClient(new IPAddress("192.168.0.50"));

// Establish connection
await Client.Connect();

// Do things!
await Client.RunExecutable(@"%windir%\system32\ping.exe");
```

## Using FactoryOrchestratorClient in PowerShell
The FactoryOrchestratorClient PowerShell module is available in FactoryOrchestratorClient.psd1. Unlike FactoryOrchestratorClient and FactoryOrchestratorUWPClient C# classes, all calls are synchronous.

To use the PowerShell module, import FactoryOrchestratorClient.psd1 and then use the New-FactoryOrchestratorClient cmdlet to create a FactoryOrchestratorClient instance. The PowerShell instance exposes the exact same APIs as the C# classes. 

```powershell
# Import client module
Import-Module FactoryOrchestratorClient.psd1

# Create client instance targeting service at desired IP Address (127.0.0.1 == loopback)
$client = New-FactoryOrchestratorClient -IpAddress "127.0.0.1"

# Establish connection. Unlike C# FactoryOrchestratorClient, this is synchronous
$client.Connect();

# Do things!
$client.RunExecutable("$ENV:windir\system32\ping.exe")
```

## Using FactoryOrchestratorUWPClient in a UWP
If you are writing a UWP app that uses the Factory Orchestrator Client API, you must use the FactoryOrchestratorUWPClient class instead of FactoryOrchestratorClient. The FactoryOrchestratorUWPClient APIs are identical to the FactoryOrchestratorClient APIs. The FactoryOrchestratorUWPClient class is available in FactoryOrchestratorUWPClientLibrary.dll. Like the FactoryOrchestratorClient C# class, all FactoryOrchestratorUWPClient API calls are [asynchronous](https://docs.microsoft.com/dotnet/csharp/async).

## Factory Orchestrator Versioning
Factory Orchestator uses [semver](https://semver.org/) versioning. If there is a major version mismatch between the client and service your program may not work as expected and either the client program or target service should be updated so the major versions match. Major versions are checked when the client connects to the service. You can also manually check the version of the client API by:

- Manually inspecting the properties of the FactoryOrchestratorClientLibrary.dll file used by your program

    ![version number in the properties of FactoryOrchestratorClientLibrary.dll](./images/fo-version-number.png)

- Programmatically with the following code snippets:

```C#
FactoryOrchestratorClient.GetClientVersionString();
```
```powershell
Get-Module FactoryOrchestratorClient
```

## Factory Orchestrator client sample

A sample .NET Core program that communicates with the Factory Orchestrator service is available in the Factory Orchestrator GitHub repo at: https://github.com/microsoft/FactoryOrchestrator/tree/main/src/ClientSample. Copy the entire directory to your technician PC, then open ClientSample.csproj in Visual Studio 2019 or build it with the dotnet CLI SDK.

The sample shows you how to connect to a remote (or local) device running Factory Orchestrator service, copy files to that device, execute test content, and retrieve the test results from the device both using an API and by retrieving the service's log files.

### Factory Orchestrator client sample usage

Once the sample is built, create a folder on your PC with test content and a FactoryOrchestratorXML file that references the test content in the location it will execute from on the test device. Then, run the sample by calling:

```cmd
dotnet ClientSample.dll <IP Address of DUT> <Folder on technician PC with test content AND FactoryOrchestratorXML files> <Destination folder on DUT> <Destination folder on this PC to save logs>
```

The sample will then connect to the test device, copy files to that device, execute test content, and retrieve the test results from the device both using an API and by retreiving the log files. You will be able to monitor the progress of the sample in the ClientSample console window, on the DUT (if it is running the Factory Orchestrator app), and on the Factory Orchestrator app on the PC (if it is connected to the test device).