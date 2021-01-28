
# Get Started with Factory Orchestrator

## Install the service

The Factory Orchestrator service (Microsoft.FactoryOrchestrator.Service.exe) runs on your Device under Test and acts as the engine powering Factory Orchestrator. You can connect to the Factory Orchestrator service from a remote technician PC, or from the device itself. 

User PowerShell to install the Factory Orchestrator on your device:

```PowerShell
     ## Optionally set it's start up to automatic with: -StartupType Automatic
    New-Service -Name "FactoryOrchestrator" -BinaryPathName "Microsoft.FactoryOrchestrator.Service.exe"

    Start-Service -Name "FactoryOrchestrator"
```

To connect to the service, you can use the Factory Orchestrator UWP app, or interact with the service programattically using the Factory Orchestrator client APIs. 

## Install the app

You can install the Factory Orchestrator app on your DUT or on a technician PC.

To install the app:

1. On your technician PC, open an administrative Command prompt.

2. Use Powershell to [install the app and its dependencies](https://docs.microsoft.com/powershell/module/appx/add-appxpackage?view=win10-ps).

    ```PowerShell
    Add-AppxPackage -path "FactoryOrchestrator\Microsoft.FactoryOrchestratorApp_8wekyb3d8bbwe.msixbundle" -DependencyPath "frameworks\Microsoft.NET.CoreFramework.x64.Debug.2.2.appx" -DependencyPath "frameworks\Microsoft.NET.CoreRuntime.x64.2.2.appx" -DependencyPath "frameworks\Microsoft.VCLibs.x64.14.00.appx"
    ```

## Run Factory Orchestrator

### Default behavior

>[!Important]
>Need instructions on how to configure a device to run FO automatically

### If the app isn't configured to run automatically

To run the Factory Orchestrator app:

1. Connect to the device where the app is installed with Device Portal
2. From Device Portal's Apps manager tab, choose `Factory Orchestrator (App)` from the Installed Apps list.
3. Click Start

The Factory Orchestrator app will start on the DUT. The Factory Orchestrator service is always running.
