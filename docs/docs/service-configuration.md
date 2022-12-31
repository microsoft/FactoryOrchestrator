<!-- Copyright (c) Microsoft Corporation. -->
<!-- Licensed under the MIT license. -->

# Factory Orchestrator service configuration using appsettings.json

The Factory Orchestrator service has many configurable settings that impact its startup behavior, enabled features, and more. This configuration is easily modified using an [appsettings.json file](https://docs.microsoft.com/en-us/dotnet/core/extensions/configuration-providers#json-configuration-provider).

The appsettings.json file is checked for in the following locations in order, with the last appsettings.json file found 'winning' if there are conflicts:

1. The directory where the service executable (Microsoft.FactoryOrchestrator.Service) is located
2. **(Windows only)** %DATADRIVE%\TestContent\Container\FactoryOrchestrator
3. The [service log file directory](#factory-orchestrator-service-log-file): `%ProgramData%\FactoryOrchestrator\` (Windows) or `/var/log/FactoryOrchestrator/`(Linux)
4. **(Linux only)** The `/etc/FactoryOrchestrator/` directory

The following table describes each setting and its usage:

| Setting name        | Type | Usage |
|---------------------|------|-------|
| [InitialTaskLists](#initialtasklists-firstboottasks-and-everyboottasks)    | string | Path to a [FactoryOrchestratorXML](tasks-and-tasklists.md#author-and-manage-factory-orchestrator-tasklists) file. The [TaskLists](tasks-and-tasklists.md#author-and-manage-factory-orchestrator-tasklists) in this file are loaded on first boot only; they are NOT run by default unless RunInitialTaskListsOnFirstBoot is set. This file defines the default "first boot" state of the DUT, that is the TaskLists & tasks that are shown in the app UI. [See below for more details.](#initialtasklists-firstboottasks-and-everyboottasks) |
| RunInitialTaskListsOnFirstBoot | bool | If set to "true", the TaskLists defined by InitialTaskLists are run on first boot of the DUT (or the first time the service is run). They are not run on subsquent boots. |
| [FirstBootTasks](#initialtasklists-firstboottasks-and-everyboottasks) | string | Path to a [FactoryOrchestratorXML](tasks-and-tasklists.md#author-and-manage-factory-orchestrator-tasklists) file. These TaskLists are run once, and then "hidden", on the first boot of the DUT (or the first time the service is run). They are not run on subsquent boots. [See below for more details.](#initialtasklists-firstboottasks-and-everyboottasks) |
| [EveryBootTasks](#initialtasklists-firstboottasks-and-everyboottasks) | string | Path to a [FactoryOrchestratorXML](tasks-and-tasklists.md#author-and-manage-factory-orchestrator-tasklists) file. These TaskLists are run on every boot of the DUT, including first boot. They are then "hidden". [See below for more details.](#initialtasklists-firstboottasks-and-everyboottasks) |
| [EnableNetworkAccess](#network-access) | bool | If set to "true", the service will allow connections from clients/apps anywhere on your local network. Defaults to false. **⚠ It is recommended to only enable this if you also use the SSLCertificateFile and SSLAllowedClientCertificates settings. [See "Network Access" below for more details.](#network-access) ⚠** |
| NetworkPort | int | The network port the service uses to communicate with clients, even local loopback clients. Defaults to 45684. |
| SSLCertificateFile | string | Path to the .PFX certificate (X509Certificate2) file used for the service's identity. If provided, the Factory Orchestrator Service will use the provided certificate for SSL encryption to communicate with clients. If not provided, a default SSL certificate is used. [See "Network Access" below for more details.](#network-access) |
| SSLAllowedClientCertificates | string | Semi-colon separated list of file paths to allowed client .PFX certificates. If provided, the Factory Orchestrator Service will only allow clients to connect to the Service if the client provided a certificate matching one in the list. If you really want to allow any client to connect, even a client with no certificate, set this value to "*". [See below for more details.](#network-access) |
| SSLUseDefaultClientCertificateValidation | bool | If set to "true", the Factory Orchestrator Service will ensure the certificate chain is valid and no certificate in the chain has been revoked. The certificate must also apply to the given hostname. [See "Network Access" below for more details.](#network-access) |
| TaskRunLogFolder | string | Path of the directory where you want Task run logs saved. This setting is a first run default; it can be overriden at runtime by the [SetLogFolder](../ClientLibrary/FactoryOrchestratorClient_SetLogFolder%28string_bool%29/)() API. See [Tasks and Tasklists](tasks-and-tasklists.md#factory-orchestrator-task-log-files) for details about the log files for individual Task runs. |
| AllowedLocalLoopbackApps | string | **Windows only.** Semi-colon separated list of Windows app "Package Family Name"(s). The Factory Orchestrator service will enable local loopback on the given apps every boot. Requires "checknetisolation.exe" is found in your %PATH%. See [this Windows IoT page](https://docs.microsoft.com/en-us/windows/iot-core/develop-your-app/loopback#enabling-loopback-for-a-uwp-application) for more information.
| DisableCommandPromptPage | bool | If set to "true", the Factory Orchestrator app will not show the "Command prompt" page. |
| DisableWindowsDevicePortalPage | bool | **Windows only.** If set to "true", the Factory Orchestrator app will not show the "Device portal" page. |
| DisableUWPAppsPage | bool | **Windows only.** If set to "true", the Factory Orchestrator app will not show the "UWP apps" page. |
| DisableManageTasklistsPage | bool | If set to "true", the Factory Orchestrator app will not show the "Manage TaskLists" page. |
| DisableFileTransferPage | bool | If set to "true", the Factory Orchestrator app will not show the "File Transfer" page. |
| IpcLogLevel | [LogLevel enum](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.loglevel) | Sets the logging level for the IPC binaries. If set to "Debug", or lower information about every client API call will be saved to the [service log](#factory-orchestrator-service-log-file). |
| DisableContainerSupport | bool | **Windows only.** If set to "true", the service will not check for a container running Factory Orchestrator on your PC. |

## Sample appsettings.json
Here's an example of what a valid appsettings.json file looks like.
```json
{
    "EnableNetworkAccess":"true",
    "NetworkPort":"45000",
    "SSLCertificateFile":"/etc/FactoryOrchestrator/ServiceCert.pfx",
    "SSLAllowedClientCertificates":"/etc/FactoryOrchestrator/AllowedClientCert1.pfx;/etc/FactoryOrchestrator/AllowedClientCert2.pfx",
    "DisableFileTransferPage":"true",
    "InitialTaskLists":"/etc/FactoryOrchestrator/InitialTaskLists.xml"
}
```


## Network Access

By default, the Factory Orchestrator service only allows client connections from the same device the service is running on (i.e. localhost only). However, service can be configured to allow connections from clients anywhere on your local network, by setting EnableNetworkAccess to "true" in your [appsettings.json file](#factory-orchestrator-service-configuration-using-appsettingsjson).

### Network access caveats

- It is strongly recommend you use [your own SSL certificates](#ssl-certificates-and-authentication) for the service and clients as the service uses an insecure certificate and allows any client to connect to it without authentication by default. Any connected client has full access to the service's computer, including the ability to send or copy files, and/or run any command or program with administrator rights.
- If you configure the service to automatically start, the service is configured to run from boot. Depending on the service & PC configuration it may even be running before a user has logged on to the computer.
- The service has minimal logging about what clients are connected to it and what commands each client has executed.

### SSL certificates and authentication

Factory Orchestrator supports using both client and service SSL certificates, enabling End-to-End authentication and encryption for all commands and data. **It is strongly recommend you use your own SSL certificates for the service AND all clients.** If you do not use your own certificates, Factory Orchestrator will still attempt (but not enforce) SSL encryption, but with no client authentication and will be vulnerable to impersonation and man-in-the-middle attacks.

You can easily generate self-signed SSL certificates using the following openssl commands to generate a .pfx file for use by either the service or client(s):

```bash
openssl req -x509 -sha256 -days 365 -nodes -newkey rsa:4096 -subj "/CN=MyFactoryOrchestrator" -keyout FactoryOrchestrator.key -out FactoryOrchestrator.crt
openssl pkcs12 -export -in FactoryOrchestrator.crt -inkey FactoryOrchestrator.key -out FactoryOrchestratorCerificate.pfx
```

_📝 PFX certificate passwords are not supported on the certificates used by Factory Orchestator. 📝_

#### Client authentication

Since Factory Orchestrator clients have near complete control of the service's computer, client authentication is stricter than Service authentication.

If **SSLAllowedClientCertificates** is set to a semi-colon separated list of .PFX certificate file paths, the Factory Orchestrator Service will only allow a client to connect if the certificate provided by the client matches on in the **SSLAllowedClientCertificates** list. The client's certificate must be an **exact** match to one in the list.

If **SSLUseDefaultClientCertificateValidation** is true, the Factory Orchestrator Service will only allow a client to connect if the client's provided certificate has a valid certificate chain (on the Service's PC) and no certificate in the chain has been revoked. The certificate must also apply to the given hostname.

Both settings may be used together for the strictest validation.

If using the app, use the "Advanced Options" button on the Connect page to provide a Client certificate.

If using C# or PowerShell, you can provide a client certificate to your [FactoryOrchestratorClient](../ClientLibrary/FactoryOrchestratorClient/) instance in either the [constructor](../ClientLibrary/FactoryOrchestratorClient/) or with the instance's [ClientCertificate](../ClientLibrary/FactoryOrchestratorClient_ClientCertificate/) parameter.

See [client usage samples for an example](../factory-orchestrator-client-usage-samples/#connect-to-the-service-running-on-a-remote-device-with-client-and-service-authentication).

#### Service authentication

Use the **SSLCertificateFile** option to provide a custom .PFX certificate for the Factory Orchestrator Service authentication. If one is not provided, a default certificate is used, but that is insecure and not recommended.

Factory Orchestrator uses the Service's certificate "Thumbprint" and Identity/"Subject" for authentication by a [FactoryOrchestratorClient](../ClientLibrary/FactoryOrchestratorClient/) instance.  To see the thumbprint and subject for a .PFX certificate, you can run the following PowerShell command:

```powershell
Get-PfxCertificate ".\FactoryOrchestratorCerificate.pfx" | Select Thumbprint, Subject
```

If using the app, use the "Advanced Options" button on the Connect page to provide the expected Service certificate Identity and Thumbprint.

If using C# or PowerShell, these values are provided in the [FactoryOrchestratorClient constructor](../ClientLibrary/FactoryOrchestratorClient_FactoryOrchestratorClient(IPAddress_int_string_string_X509Certificate2)/) or with the [CertificateHash](../ClientLibrary/FactoryOrchestratorClient_CertificateHash/) and [ServerIdentity](../ClientLibrary/FactoryOrchestratorClient_ServerIdentity/) properties.

See [client usage samples for an example](../factory-orchestrator-client-usage-samples/#connect-to-the-service-running-on-a-remote-device-with-client-and-service-authentication).

You can also provide your own [certificate validation callback](https://learn.microsoft.com/en-us/dotnet/api/system.net.security.remotecertificatevalidationcallback?f1url=%3FappId%3DDev16IDEF1%26l%3DEN-US%26k%3Dk(System.Net.Security.RemoteCertificateValidationCallback)%3Bk(SolutionItemsProject)%3Bk(SolutionItemsProject)%3Bk(DevLang-csharp)%26rd%3Dtrue&view=netstandard-2.0) to the FactoryOrchestratorClient instance (either in the [constructor](../ClientLibrary/FactoryOrchestratorClient/) or with the [ServerCertificateValidationCallback parameter](../ClientLibrary/FactoryOrchestratorClient_ServerCertificateValidationCallback/)) if you prefer to do your own validation of the Service's certificate.

### Firewall configuration

Depending on the configuration of the OS you are using, you may need to configure the firewall to allow the Factory Orchestrator service to communicate over your local network. Factory Orchestrator uses TCP port 54684 for client<->service communication and UDP port 5353 for [DNS-SD](find-factory-orchestrator-devices.md).

On Windows, run the following command from an Administrator PowerShell window to allow Factory Orchestrator through the Windows firewall:

```powershell
$FoPath = (Get-CimInstance win32_service | ?{$_.Name -like 'Microsoft.FactoryOrchestrator'}).PathName.replace(' -IsService', ''); netsh advfirewall firewall add rule name="Factory Orchestrator service in" dir=in action=allow program="$FoPath" enable=yes; netsh advfirewall firewall add rule name="Factory Orchestrator service out" dir=out action=allow program="$FoPath" enable=yes
```

On Ubuntu and many other Linux distros, run the following Bash commands:

```bash
sudo ufw allow 45684
sudo ufw allow 5353
```

If you set a custom network port via the NetworkPort setting, use that port number instead of 45684.

### Checking for network access

To check if network access is currently enabled use one of the following:

- The Factory Orchestrator app's "About" page.
- The console output from Microsoft.FactoryOrchestrator.Service.exe
- The [service log file](#factory-orchestrator-service-log-file)
- The [IsNetworkAccessEnabled](../ClientLibrary/FactoryOrchestratorClient_IsNetworkAccessEnabled%28%29/) API.

## InitialTaskLists, FirstBootTasks, and EveryBootTasks 
_💡 [We are considering reworking InitialTaskLists and \*BootTasks, as it is hard to understand the use cases and tradeoffs for each type](https://github.com/microsoft/FactoryOrchestrator/issues/109). 💡_

The Factory Orchestrator service looks for certain [FactoryOrchestratorXML](tasks-and-tasklists.md#author-and-manage-factory-orchestrator-tasklists) files when it starts, based on file paths set in the InitialTaskLists, FirstBootTasks, and EveryBootTasks settings. You can use these FactoryOrchestratorXML files to pre-load tasks into Factory Orchestrator (without running them), run tasks the first time a device boots, or run tasks every time a device boots.

These 3 FactoryOrchestratorXML files are executed in the following order:

1. FirstBootTasks (if it is first boot)
2. EveryBootTasks
3. Then finally InitialTaskLists (if it is first boot & RunInitialTaskListsOnFirstBoot is set).

The FirstBootTasks & EveryBootTasks settings are intended to be used for "configuration" & "pre-setup" operations, as they are tracked separately from all other Tasks, and their state is 'hidden' on completion (not show in app UI and not returned by most client APIs). For example, starting a non-critical logger service every boot would be a good use of the EveryBootTasks. On the other hand, InitialTaskLists is intended to be used to setup the "first run" state of Factory Orchestrator. Keep in mind this state is only the "first run" state, it may change as client(s) interact with Factory Orchestrator.

FirstBootTasks & EveryBootTasks have the following 'rules':

- While you can author normal `<Tasks>` in the \*BootTasks files, [`<BackgroundTasks>`](tasks-and-tasklists.md#background-tasks) are especially useful, as you can define `<BackgroundTasks>` which start on boot, are never expected to ever exit, and will run in the background forever (provided `TerminateBackgroundTasksOnCompletion="false"`).
- When all `<Tasks>` defined in the \*BootTasks FactoryOrchestratorXML files are done executing, the service "resets" and any TaskLists & Tasks defined in thoes 2 files are "hidden". This means that the app UI will only show the TaskLists defined by the InitialTaskLists setting if this is the first boot, and possibly any TaskLists that have been added at runtime by a client (if this isn't the first boot).
- `<TaskLists>` defined in the \*BootTasks FactoryOrchestratorXML files are only returned by GetTaskListGuids() and GetTaskListSummaries() [client methods](use-the-factory-orchestrator-api.md) **only while the service is actively executing boot TaskLists**. Upon completion of all boot TaskLists, you can only query boot TaskList information via the GetBootTaskListGuids(), & GetBootTaskListSummaries() client methods. You can also query the \*BootTasks Tasks and TaskLists directly **at any time** if you know their GUID using QueryTask(), QueryTaskList(), and QueryTaskRun().
- The Factory Orchestrator service does not allow certain client commands to run until all `<Tasks>` (excluding `<BackgroundTasks>`) defined in the relevant \*BootTasks files are done executing. You will get a FactoryOrchestratorBootTasksExecutingException if you call them while the *BootTasks files are running. You can check the [FactoryOrchestrator service log file](service-configuration.md#factory-orchestrator-logs) to see if the \*BootTasks files are done executing. Or use the [IsExecutingBootTasks() client API](use-the-factory-orchestrator-api.md). Lastly, when the service is running one of the two \*BootTasks files, you'll see a warning in the Factory Orchestrator app UI: ![Service is executing Boot tasks, many functions are disabled until finished or aborted](./images/fo-background-tasks-executing.png)

You can inspect the [FactoryOrchestrator service and Task run log files](service-configuration.md#factory-orchestrator-logs) for details about the status and/or results of the \*BootTasks FactoryOrchestratorXML files, in "EveryBootTaskLists" & "FirstBootTaskLists" subfolders in the task run log directory.

## Factory Orchestrator logs
### Factory Orchestrator service log file
The service log file contains details about the operation of the Factory Orchestrator service. It is always found at `%ProgramData%\FactoryOrchestrator\FactoryOrchestratorService.log` on Windows and `/var/log/FactoryOrchestrator/FactoryOrchestratorService.log` on Linux. Inspect this log for details about the service's operation.

### Factory Orchestrator Task log files
See [Tasks and Tasklists](tasks-and-tasklists.md#factory-orchestrator-task-log-files) for details about the log files for individual Task runs.

