
# Get started with Factory Orchestrator

## Install or Run the service
**The service can be downloaded from the [GitHub releases page](https://github.com/microsoft/FactoryOrchestrator/releases).**

The Factory Orchestrator service (Microsoft.FactoryOrchestrator.Service) runs on your Device under Test and acts as the engine powering Factory Orchestrator. To connect to the service, you can use the Factory Orchestrator UWP app, or interact with the service [programmatically using the Factory Orchestrator client APIs](use-the-factory-orchestrator-api.md). Multiple clients can be connected to the same service simultaneously.

The service can either be "run" (one time, not started on boot) or "installed" so that it automatically starts every boot.

![Image of service start](./images/service-start.png) 

### Run the service
Download and unzip the service for your target OS and architecture. Then simply run Microsoft.FactoryOrchestrator.Service as administrator/sudo.

### Install the service (Windows)
Download and unzip the service for your target OS and architecture. Then use an Administrator/Sudo PowerShell to install the Factory Orchestrator service on your device. When installed as a system service (daemon), the service will start every boot until disabled or uninstalled:

```PowerShell
     ## Optionally set it's start up to automatic with: -StartupType Automatic
    New-Service -Name "FactoryOrchestrator" -BinaryPathName "Microsoft.FactoryOrchestrator.Service.exe"

    Start-Service -Name "FactoryOrchestrator"
```

### Service configuration
See [Service configuration](../service-configuration) for details on how you can configure the Factory Orchestraor service's default behavior.

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
