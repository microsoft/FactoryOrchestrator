
# Get Started with Factory Orchestrator

## Install or Run the service
The service can be downloaded from the [GitHub releases page](https://github.com/microsoft/FactoryOrchestrator/releases).

The Factory Orchestrator service (Microsoft.FactoryOrchestrator.Service.exe) runs on your Device under Test and acts as the engine powering Factory Orchestrator. To connect to the service, you can use the Factory Orchestrator UWP app, or interact with the service [programmatically using the Factory Orchestrator client APIs](use-the-factory-orchestrator-api.md). Multiple clients can be connected to the same service simultaneously.

The service can either be "run" (one time, not started on boot) or "installed" so that it automatically starts every boot.

### Run the service
Download and unzip the service for your target OS and architecture. Then simply run Microsoft.FactoryOrchestrator.Service.exe as administrator/sudo.

### Install the service
Download and unzip the service for your target OS and architecture. Then use an Administrator/Sudo PowerShell to install the Factory Orchestrator service on your device. When installed as a system service (daemon), the service will start every boot until disabled or uninstalled:

```PowerShell
     ## Optionally set it's start up to automatic with: -StartupType Automatic
    New-Service -Name "FactoryOrchestrator" -BinaryPathName "Microsoft.FactoryOrchestrator.Service.exe"

    Start-Service -Name "FactoryOrchestrator"
```

### (Optional & **not** recommended) Enable network access
By default, the Factory Orchestrator service only allows client connections from the same device the service is running on (i.e. localhost only). However, service can be configured to allow connections from clients anywhere on your local network.

***⚠⚠⚠⚠ WARNING: Please read and understand the following before enabling network access! ⚠⚠⚠⚠***

- ⚠ The service allows any client to connect to it without authentication. Any connected client has full access to the service's computer, including the ability to send or copy files, and/or run any command or program with administrator rights. ⚠
- ⚠ If you "install" the service using the above steps, the service will be configured to run from boot. Depending on the configuration the service may even be running before a user has logged into the computer. ⚠
- ⚠ Once network access is enabled, it will remain enabled until the changes to enable network access are reverted. ⚠
- ⚠ The service and client send information over the network in unencrypted JSON using TCP. It is therefore vulnerable to man-in-the-middle attacks. ⚠
- ⚠ The service currently has minimal logging about what clients are connected to it and what commands each client has executed. ⚠


If you understand these risks but still require the service to communicate with clients over the local network, set both of the following registry values on the service's computer using regedit.exe or reg.exe:

Key | Value | [Type](../CoreLibrary/Microsoft-FactoryOrchestrator-Core-TaskBase-Type/) | Data
------ | ------ | --- | ---
HKLM\SYSTEM\CurrentControlSet\Control\FactoryOrchestrator | EnableNetworkAccess | REG_DWORD | 0x1
HKLM\SYSTEM\CurrentControlSet\Control\FactoryOrchestrator | DisableNetworkAccess | REG_DWORD | 0x0

Once the two registry values are set, restart the service if it is running.

#### Disable network access
If you have enabled network access using the above steps, you can delete the two registry values above to disable network access. Restart the service if it is running.

## Install the app

You can install the Factory Orchestrator app on your DUT or on a technician PC running Windows. The app can be downloaded from the [GitHub releases page](https://github.com/microsoft/FactoryOrchestrator/releases).

To install the app, run the .msixbundle to install the app. Alternately, use [Windows Device Portal](https://docs.microsoft.com/en-us/windows/uwp/debug-test-perf/device-portal) to install the app on your DUT. The app on the GitHub releases page is signed by Microsoft and does not require a certificate file to install.

## Run the app

To run the Factory Orchestrator app, use the Start menu to launch "Factory Orchestrator". Alternately, use [Windows Device Portal](https://docs.microsoft.com/en-us/windows/uwp/debug-test-perf/device-portal) to run the app:

1. Connect to the device where the app is installed with Device Portal
2. From Device Portal's Apps manager tab, choose `Factory Orchestrator (App)` from the Installed Apps list.
3. Click Start

The Factory Orchestrator app will start on the PC. If the PC has the Factory Orchestrator service running, it will automatically connect to the service. If not, you will be prompted to enter the IPv4 address of the service you wish to connect to.

See [Run using the application](use-the-factory-orchestrator-app.md) for details on how to use the app.
