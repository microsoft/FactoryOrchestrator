<!-- Copyright (c) Microsoft Corporation. -->
<!-- Licensed under the MIT license. -->

# Factory Orchestrator Client API Samples
This page describes how to perform a variety of tasks with the Factory Orchestrator client APIs, using C# code snippets. There is also a complete C# client sample you can use as a starting point if you prefer.

All C# code snippets assume you have:
```csharp
using Microsoft.FactoryOrchestrator.Core;
using Microsoft.FactoryOrchestrator.Client;
```
defined at the top of your .cs file.

Remember:

- All C# client APIs are asynchronous, but all PowerShell APIs are synchronous.
- To use the C# client APIs in a UWP app, use an instance of FactoryOrchestratorUWPClient instead of [FactoryOrchestratorClient](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-FactoryOrchestratorClient%28System-Net-IPAddress_int%29/).
- 'device' is used below to refer to the PC running the Factory Orchestrator service you connect to with the client. This could be the same PC as the client.

# Factory Orchestrator .NET client sample
A sample .NET Core program that communicates with the Factory Orchestrator service is available in the Factory Orchestrator GitHub repo at: [https://github.com/microsoft/FactoryOrchestrator/tree/main/src/ClientSample](https://github.com/microsoft/FactoryOrchestrator/tree/main/src/ClientSample). You can build it with Visual Studio 2019 or the .NET Core 3.1+ SDK.

The sample shows you how to connect to a remote (or local) device running Factory Orchestrator service, copy test files to that device, sideload UWP apps, execute test content, and retrieve the test results from the device.

## Factory Orchestrator client sample usage
Once the sample is built, create a folder on your PC with test content and a [FactoryOrchestratorXML](../tasks-and-tasklists/#author-and-manage-factory-orchestrator-tasklists) file that references the test content in the location it will execute from on the test device. Then, run the sample by calling:

```cmd
dotnet ClientSample.dll <IP Address of DUT> <Folder on technician PC with test content AND FactoryOrchestratorXML files> <Destination folder on DUT> <Destination folder on this PC to save logs>
```

The sample will then connect to the test device, copy test files to that device, sideload UWP apps, execute test content, and retrieve the test results from the device. You will be able to monitor the progress of the sample in the console, on the DUT (if it is running the Factory Orchestrator app), and on the Factory Orchestrator app on your PC (if it is connected to the test device).

# Establishing a connection to the service
Before executing any other commands, the [Connect](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-Connect%28bool%29/)() API must be called. [Connect](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-Connect%28bool%29/)() verifies the target service is running and has a compatible version with your client. [Connect](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-Connect%28bool%29/)() throws an exeption if the connection fails. Alternately, you can use [TryConnect](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-TryConnect%28bool%29/)() which returns false if the connection fails instead.

## Connect to the service running on the local device (loopback)
```csharp
var client = new FactoryOrchestratorClient(IPAddress.Loopback);
await client.Connect();
```

```powershell
# 127.0.0.1 == loopback
$client = New-FactoryOrchestratorClient -IpAddress "127.0.0.1"
$client.Connect();
```

## Connect to the service running on a remote device
C#:
```csharp
var client = new FactoryOrchestratorClient(IPAddress.Parse("192.168.0.100"));
await client.Connect();
```

PowerShell 7+:
```powershell
$client = New-FactoryOrchestratorClient -IpAddress "192.168.0.100"
$client.Connect();
```


# Task, [TaskList](../CoreLibrary/Microsoft-FactoryOrchestrator-Core-TaskList/), & [TaskRun](../CoreLibrary/Microsoft-FactoryOrchestrator-Core-TaskRun/) creation, interaction, & manipulation
## Load [TaskList](../CoreLibrary/Microsoft-FactoryOrchestrator-Core-TaskList/)s from a file, run the loaded TaskLists to completion. Then get the generated [TaskRun](../CoreLibrary/Microsoft-FactoryOrchestrator-Core-TaskRun/) log files.
[TaskList](../CoreLibrary/Microsoft-FactoryOrchestrator-Core-TaskList/)s defined in [FactoryOrchestratorXML](../tasks-and-tasklists/#author-and-manage-factory-orchestrator-tasklists) files are a great way to organize a set of operations you want Factory Orchestrator to execute. This example shows how to load [TaskList](../CoreLibrary/Microsoft-FactoryOrchestrator-Core-TaskList/)(s) from a [FactoryOrchestratorXML](../tasks-and-tasklists/#author-and-manage-factory-orchestrator-tasklists) file and then run the loaded TaskLists using [LoadTaskListsFromXmlFile](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-LoadTaskListsFromXmlFile%28string%29/) and [RunTaskList](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-RunTaskList%28System-Guid_int%29/). It also prints high-level info about the [TaskList](../CoreLibrary/Microsoft-FactoryOrchestrator-Core-TaskList/) statues with [GetTaskListSummaries](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-GetTaskListSummaries%28%29/). Depending on the attributes defined for each [TaskList](../CoreLibrary/Microsoft-FactoryOrchestrator-Core-TaskList/) they may or may not run in parallel.

```csharp
// Assumes you have an existing FactoryOrchestratorXML file on the filesystem of the service at (string) FactoryOrchestratorXmlPath.
// See "Copy a file or folder from client to device" or "Factory Orchestrator .NET client sample" for details on how to copy files to the service from a client.
var taskListGuids = await client.LoadTaskListsFromXmlFile(FactoryOrchestratorXmlPath);
foreach (var taskListGuid in taskListGuids)
{
    await client.RunTaskList(taskListGuid);
}

// Wait for all TaskLists to complete.
while ((await client.GetTaskListSummaries()).Any(x => x.IsRunningOrPending))
{
    await Task.Delay(2000);
}

// Copy all TaskRun logs to client-side device.
await client.GetDirectoryFromDevice(await client.GetLogFolder(), @"C:\some_folder_on_client_for_logs");
```

## Modify an existing [TaskList](../CoreLibrary/Microsoft-FactoryOrchestrator-Core-TaskList/)
This example shows how to modify an existing [TaskList](../CoreLibrary/Microsoft-FactoryOrchestrator-Core-TaskList/) using [QueryTaskList](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-QueryTaskList%28System-Guid%29/) and [UpdateTaskList](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-UpdateTaskList%28Microsoft-FactoryOrchestrator-Core-TaskList%29/).
```csharp
// Assumes you have an existing TaskList in the service with a GUID of 34d0534b-ed09-46c2-b6bb-73cef9574944.
var taskListGuid = new Guid("34d0534b-ed09-46c2-b6bb-73cef9574944");
var taskList = await client.QueryTaskList(taskListGuid);

// Add new Tasks to the TaskList
taskList.Tasks.Add(new PowerShellTask(@"C:\scripts\pwshScriptRunLast.ps1")); // Will run last
taskList.Tasks.Insert(0, new ExecutableTask(@"C:\exes\runMeFirst.exe")); // Will run first

// Find an existing Task in the TaskList using LINQ and edit it
taskList.Tasks.Where(x => x.Path == @"C:\exes\someExistingExeTask.exe").First().Arguments += " --aNewArgument";

// Modify TaskList properties to not allow parallel Task and TaskList execution.
taskList.RunInParallel = false;
taskList.AllowOtherTaskListsToRun = false;

// Update the TaskList on the service.
await client.UpdateTaskList(taskList);
```
## Get all existing TaskLists and print out information about each [TaskList](../CoreLibrary/Microsoft-FactoryOrchestrator-Core-TaskList/).
This example uses the [GetTaskListSummaries](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-GetTaskListSummaries%28%29/) and [QueryTaskList](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-QueryTaskList%28System-Guid%29/) methods print out information about running [TaskList](../CoreLibrary/Microsoft-FactoryOrchestrator-Core-TaskList/) instances.

```csharp
var summaries = await client.GetTaskListSummaries();
foreach (var summary in summaries)
{
    Console.WriteLine($"TaskList with Name {summary.Name} and GUID {summary.Guid} is currently {summary.Status}...");

    // If the TaskList is running, let's get some more details about the Tasks within it
    if (summary.Status == TaskStatus.Running)
    {
        var taskList = await client.QueryTaskList(summary.Guid);
        foreach (var task in taskList.Tasks.Where(x => x.LatestTaskRunStatus == TaskStatus.Running))
        {
            Console.WriteLine($"    A {task.Type} Task with Name {task.Name} and GUID {task.Guid} has been running for {DateTime.Now - task.LatestTaskRunTimeStarted} seconds...");
        }
    }
}
```

## Stop executing an existing & running [TaskList](../CoreLibrary/Microsoft-FactoryOrchestrator-Core-TaskList/)
```csharp
// Assumes you have an existing TaskList in the service with a GUID of 34d0534b-ed09-46c2-b6bb-73cef9574944.
await client.AbortTaskList(new Guid("34d0534b-ed09-46c2-b6bb-73cef9574944"));
```

## Check for service events. Use WaitingForExternalTaskRun to handle every [ExternalTask](../CoreLibrary/Microsoft-FactoryOrchestrator-Core-ExternalTask/) requiring manual completion.
This example uses the GetServiceEvents method to check if an [ExternalTask](../CoreLibrary/Microsoft-FactoryOrchestrator-Core-ExternalTask/) needs manual completion. If so, it uses [QueryTaskRun](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-QueryTaskRun%28System-Guid%29/) and [UpdateTaskRun](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-UpdateTaskRun%28Microsoft-FactoryOrchestrator-Core-TaskRun%29/) to complete the [ExternalTask](../CoreLibrary/Microsoft-FactoryOrchestrator-Core-ExternalTask/).

```csharp
ulong lastEvent = 0;

// Loop, checking for new events
while (true)
{
    var events = await client.GetServiceEvents(lastEvent);
    foreach (var serviceEvent in events)
    {
        // update lastEvent so GetServiceEvents(lastEvent) will not return this event (or any event before it) again.
        lastEvent = serviceEvent.EventIndex;

        // log event to console.
        Console.WriteLine($"New service event, seen at {serviceEvent.EventTime}: {serviceEvent.Message}");

        if (serviceEvent.ServiceEventType == ServiceEventType.WaitingForExternalTaskRun)
        {
            // The service is waiting for a client to complete this TaskRun, let's do it!
            // Get the TaskRun object the event is referring to.
            var taskRun = await client.QueryTaskRun((Guid)serviceEvent.Guid);
            // Mark the TaskRun as Passed.
            taskRun.TaskStatus = TaskStatus.Passed;
            taskRun.TaskOutput.Add("Good job, this TaskRun is passed!");
            // Update the service with the now completed TaskRun.
            await client.UpdateTaskRun(taskRun);
        }
    }

// Wait 5 seconds before checking for new events
await Task.Delay(5000);
}
```

## Run a program to completion and print output to console
This example uses the [RunExecutable](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-RunExecutable%28string_string_string_bool%29/)() method to run a program outside of a [TaskList](../CoreLibrary/Microsoft-FactoryOrchestrator-Core-TaskList/). It then uses [QueryTaskRun](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-QueryTaskRun%28System-Guid%29/) to monitor the program's status.

```csharp
// Start the program
var taskRun = await client.RunExecutable(@"%windir%\system32\ping.exe","www.bing.com");

// Wait for program to complete
while (!taskRun.TaskRunComplete)
{
    await Task.Delay(1000);
    taskRun = await client.QueryTaskRun(taskRun.Guid);
}

// Program has completed.
Console.WriteLine($"Program exited with code {taskRun.ExitCode} at {taskRun.TimeFinished}.");
Console.WriteLine($"Program output:");
foreach (var line in taskRun.TaskOutput)
{
    Console.WriteLine(line);
}
```

```powershell
# Start the program
$taskRun = $client.RunExecutable("$env:windir\system32\ping.exe", "www.bing.com");

while (-not $taskRun.TaskRunComplete)
{
    Start-Sleep -seconds 1
    $taskRun = $client.QueryTaskRun($taskRun.Guid);
}

# Program has completed.
Write-Host "Program exited with code $($taskRun.ExitCode) at $($taskRun.TimeFinished)."
foreach ($line in $($taskRun.TaskOutput))
{
    Write-Host $line
}
```

# Using [ServerPoller](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-ServerPoller-ServerPoller%28System-Nullable-System-Guid-_System-Type_int_bool_int%29/) instances to monitor [TaskList](../CoreLibrary/Microsoft-FactoryOrchestrator-Core-TaskList/) & [TaskRun](../CoreLibrary/Microsoft-FactoryOrchestrator-Core-TaskRun/) execution asynchronously with C# events
The [ServerPoller](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-ServerPoller-ServerPoller%28System-Nullable-System-Guid-_System-Type_int_bool_int%29/) class is used to automatically poll the service for updates, and generates an [OnUpdatedObject](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-ServerPoller-OnUpdatedObject/) event only when the chosen [TaskList](../CoreLibrary/Microsoft-FactoryOrchestrator-Core-TaskList/) or [TaskRun](../CoreLibrary/Microsoft-FactoryOrchestrator-Core-TaskRun/) object you are polling updates. It can also be used to query all TaskLists for their high-level status. [ServerPoller](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-ServerPoller-ServerPoller%28System-Nullable-System-Guid-_System-Type_int_bool_int%29/) objects are a good choice to asynchronously update your UI or console output.

This example uses the ServerPoller's [StartPolling](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-ServerPoller-StartPolling%28Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient%29/) method and [OnUpdatedObject](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-ServerPoller-OnUpdatedObject/) event to monitor TaskList and [TaskRun](../CoreLibrary/Microsoft-FactoryOrchestrator-Core-TaskRun/) objects.

```csharp
object consoleLock = new object();

// Pollers should have Dispose() called on them when no longer needed.
ServerPoller taskListSummaryPoller = new ServerPoller(null, typeof(TaskList));
// Assumes you have an existing TaskList in the service with a GUID of 34d0534b-ed09-46c2-b6bb-73cef9574944.
ServerPoller taskListPoller = new ServerPoller(new Guid("34d0534b-ed09-46c2-b6bb-73cef9574944"), typeof(TaskList));
// Assumes you have an existing TaskRun in the service with a GUID of e7e316eb-696c-433b-957a-bfcada75c81c
ServerPoller taskRunPoller = new ServerPoller(new Guid("e7e316eb-696c-433b-957a-bfcada75c81c"), typeof(TaskRun));
int lastOutputIndex = 0;

async void StartPollers()
{
    var client = new FactoryOrchestratorClient(IPAddress.Parse("192.168.0.100"));
    await client.Connect();
    
    // If GUID is null and type is TaskList, ServerPoller returns a List<TaskListSummary> object.
    taskListSummaryPoller.OnUpdatedObject += OnUpdatedTaskListSummaries;
    // If GUID is NOT null and type is TaskList, ServerPoller returns a TaskList object.
    taskListPoller.OnUpdatedObject += OnUpdatedTaskList;
    // If GUID is NOT null and type is TaskRun, ServerPoller returns a TaskRun object.
    taskRunPoller.OnUpdatedObject += OnUpdatedTaskRun;


    // StartPolling takes the FactoryOrchestratorClient you want to use for polling as an argument
    taskListSummaryPoller.StartPolling(client);
    taskListPoller.StartPolling(client);
    taskRunPoller.StartPolling(client);
}

void OnUpdatedTaskListSummaries(object source, ServerPollerEventArgs e)
{
    // New TaskListSummaries are ready, let's print them out
    if (e.Result != null)
    {
        List<TaskListSummary> updatedSummaries = (List<TaskListSummary>)e.Result;
        lock (consoleLock)
        {
            Console.WriteLine("--- Latest TaskLists Status ---");
            foreach (var summary in updatedSummaries)
            {
                Console.WriteLine(summary); // Uses TaskListSummary.ToString() to print readable output
            }
        }
    }
}

void OnUpdatedTaskList(object source, ServerPollerEventArgs e)
{
    // New TaskList data for 34d0534b-ed09-46c2-b6bb-73cef9574944 is ready, let's print it out
    if (e.Result != null)
    {
        TaskList updatedTaskList = (TaskList)e.Result;
        lock (consoleLock)
        {
            Console.WriteLine("--- Latest Status of TaskList 34d0534b-ed09-46c2-b6bb-73cef9574944 ---");
            foreach (var task in updatedTaskList.Tasks)
            {
                Console.WriteLine($"Task {task.Name} is currently {task.LatestTaskRunStatus}.");
            }
        }
    }
}

void OnUpdatedTaskRun(object source, ServerPollerEventArgs e)
{
    // New TaskRun data for e7e316eb-696c-433b-957a-bfcada75c81c is ready, let's print it out
    if (e.Result != null)
    {
        TaskRun updatedTaskRun = (TaskRun)e.Result;
        lock (consoleLock)
        {
            if (lastOutputIndex < updatedTaskRun.TaskOutput.Count)
            {
                // We have new TaskRun output. Write it to console.
                Console.WriteLine("--- Latest output of TaskRun e7e316eb-696c-433b-957a-bfcada75c81c ---");
                while (lastOutputIndex < updatedTaskRun.TaskOutput.Count)
                {
                    Console.WriteLine(updatedTaskRun.TaskOutput[lastOutputIndex++]);
                }
            }
        }

        if (updatedTaskRun.TaskRunComplete)
        {
            // Stop polling, run is complete and will never have new data
            taskRunPoller.StopPolling();
        }
    }
}
```

# Install an app, enable local loopback on the installed app, and then launch it (Windows Only & requires Windows Device Portal is running)
This example uses [SendAndInstallApp](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-SendAndInstallApp%28string_System-Collections-Generic-List-string-_string%29/), [EnableLocalLoopbackForApp](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-EnableLocalLoopbackForApp%28string%29/), and [RunApp](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-RunApp%28string%29/) to install a UWP app on the device, enable local loopback on the app (enabling it to talk to a localhost Factory Orchestrator service), and launch the app.

```csharp
// Assumes appFolder is a "standard" Visual Studio 2019 published app package with an appx/msix, a .cer certificate, and a dependencies folder.
// appFolder is on the client device, NOT on the service. SendAndInstallApp copies it to the service-side device.
var files = Directory.EnumerateFiles(appFolder);
var app = files.Where(x => x.EndsWith(".appx", StringComparison.InvariantCultureIgnoreCase) || x.EndsWith(".appxbundle", StringComparison.InvariantCultureIgnoreCase) || x.EndsWith(".msixbundle", StringComparison.InvariantCultureIgnoreCase) || x.EndsWith(".msix", StringComparison.InvariantCultureIgnoreCase)).First();
var cert = files.Where(x => x.EndsWith(".cer", StringComparison.InvariantCultureIgnoreCase)).First();
var interiorDirs = Directory.EnumerateDirectories(appFolder);
List<string> depsForApp = null;

if (interiorDirs.Count() > 0 && interiorDirs.Any(x => x.EndsWith(@"\dependencies", StringComparison.InvariantCultureIgnoreCase)))
{
    // App directory has 'dependencies' folder, see if it contains dependent apps
    depsForApp = await Directory.EnumerateFiles(Path.Combine(appFolder, "dependencies"), "*", SearchOption.AllDirectories).ToList();
    if (depsForApp.Count > 0)
    {
        depsForApp = depsForApp.Where(x => x.EndsWith(".appx", StringComparison.InvariantCultureIgnoreCase) || x.EndsWith(".appxbundle", StringComparison.InvariantCultureIgnoreCase) || x.EndsWith(".msixbundle", StringComparison.InvariantCultureIgnoreCase) || x.EndsWith(".msix", StringComparison.InvariantCultureIgnoreCase)).ToList();

        if (depsForApp.Count == 0)
        {
            depsForApp = null;
        }
    }
}

// We have found the app, dependencies, and certificate on the client-side PC. SendAndInstallApp will copy the files to the service-side device and install the app using Windows Device Portal.
await client.SendAndInstallApp(app, depsForApp, cert);
// App installed sucessfully. Enable local loopback & then run it!
// Assumes newky installed app AUMID is "Contoso.TestApp_8wekyb3d8bbwe".
await client.EnableLocalLoopbackForApp("Contoso.TestApp_8wekyb3d8bbwe");
await client.RunApp("Contoso.TestApp_8wekyb3d8bbwe");
```

# System information
The following examples show how to get information about the hardware and software of the device the service is running on.
## Get OS and Factory Orchestrator build versions
This example uses [GetOSVersionString](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-GetOSVersionString%28%29/), [GetOEMVersionString](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-GetOEMVersionString%28%29/), [GetServiceVersionString](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-GetServiceVersionString%28%29/), and [GetClientVersionString](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-GetClientVersionString%28%29/) to get various system & Factory Orchestrator information.

```csharp
// GetOSVersionString() & GetOEMVersionString() are Windows only
var osbuild = await client.GetOSVersionString();
var oembuild = await client.GetOEMVersionString();
var servicebuild = await client.GetServiceVersionString();
var clientbuild = await client.GetClientVersionString();
```

## Get network adapter info with [GetIpAddressesAndNicNames](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-GetIpAddressesAndNicNames%28%29/)
```csharp
var networkinfo = await client.GetIpAddressesAndNicNames();
Console.WriteLine($"The following networks are present on {client.IpAddress}:");
foreach (var network in networkinfo)
{
    Console.WriteLine($"IP is {network.Item1}. NIC name is {network.Item2}");
}
```

## Get installed UWP apps (Windows only) with [GetInstalledApps](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-GetInstalledApps%28%29/)
```csharp
var appAUMIDs = await client.GetInstalledApps();
Console.WriteLine($"The following UWPs are installed on {client.IpAddress}:");
foreach (var aumid in appAUMIDs)
{
    Console.WriteLine(aumid);
}
```

# System interaction
## Reboot device with [RebootDevice](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-RebootDevice%28uint%29/)
```csharp
await client.RebootDevice();
```

## Shutdown device with [ShutdownDevice](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-ShutdownDevice%28uint%29/)
```csharp
await client.ShutdownDevice();
```

# File system interactions
The following examples show how to perform file system operations on the device the service is running on.
## List files & folders on device with EnumerateDirectories and EnumerateFiles
```csharp
// List all folders under %windir% recursively
var dirsRecursive = await client.EnumerateDirectories(@"%windir%", true);
// List all all files under %windir% (only, not recursive)
var filesNonRecursve = await client.EnumerateFiles(@"%windir%", false);
```

## Copy a file or folder from device to client with [GetFileFromDevice](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-GetFileFromDevice%28string_string_bool%29/) or [GetDirectoryFromDevice](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-GetDirectoryFromDevice%28string_string_bool%29/)
```csharp
// C:\destination_folder_on_client is created if needed
var bytesReceived = await client.GetDirectoryFromDevice(@"C:\source_folder_on_device", @"C:\destination_folder_on_client");
bytesReceived += await client.GetFileFromDevice(@"C:\different_folder_on_device\file.txt", @"C:\destination_folder_on_client\file.txt");
```

## Copy a file or folder from client to device with [SendFileToDevice](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-SendFileToDevice%28string_string_bool%29/) or [SendDirectoryToDevice](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-SendDirectoryToDevice%28string_string_bool%29/)
```csharp
// C:\destination_folder_on_device is created if needed
var bytesSent = await client.SendDirectoryToDevice(@"C:\source_folder_on_client", @"C:\destination_folder_on_device");
bytesSent += await client.SendFileToDevice(@"C:\different_source_folder_on_client\file.txt", @"C:\destination_folder_on_device\file.txt");
```

## Move a file or folder with [MoveFileOrFolder](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-MoveFileOrFolder%28string_string_bool%29/)
```csharp
await client.MoveFileOrFolder(@"C:\folder_on_device\file.txt", @"C:\different_folder_on_device\file.txt");
await client.MoveFileOrFolder(@"C:\folder_on_device", @"C:\different_folder_on_device");
```

## Delete a file or folder with [DeleteFileOrFolder](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-DeleteFileOrFolder%28string_bool%29/)
```csharp
await client.DeleteFileOrFolder(@"C:\folder_on_device\file.txt");
await client.DeleteFileOrFolder(@"C:\folder_on_device");
```
