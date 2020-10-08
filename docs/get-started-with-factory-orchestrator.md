---
ms.author: themar
author: themar-msft
ms.date: 09/30/2019
---

# Get started with Factory Orchestrator

Get started with Factory Orchestrator by installing the Factory Orchestrator service (FactoryOrchestratorService.exe) on your device to enable you to connect to the service to run Factory Orchestrator tasks. 

To connect to the service, you can use the Factory Orchestrator UWP app, or crete your own app using the Factory Orchestrator client APIs. You can connect to the Factory Orchestrator service from a remote technician PC, or from the device itself.

## Install the service

User PowerShell to install the Factory Orchestrator on your device:

```PowerShell
New-Service -Name "FactoryOrchestrator" -BinaryPathName "FactoryOrchestratorService.exe"
```

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

1. Connect to the Windows Core device with Device Portal
2. From Device Portal's Apps manager tab, choose `Factory Orchestrator (App)` from the Installed Apps list.
3. Click Start

The Factory Orchestrator app will start on the DUT. The Factory Orchestrator service is always running.

## Configure Factory Orchestrator to automatically load TaskLists when it starts

Factory Orchestrator looks for certain FactoryOrchestratorXML files when it starts. You can use these FactoryOrchestratorXML files to preload tasks into Factory Orchestrator, run tasks the first time a device boots, or run tasks every time a device boots.

See [Special FactoryOrchestratorXML files](automate-factory-orchestrator.md#special-factoryorchestratorxml-files) for more information.

## Add test content to your device

### Manually copy test content

If you want to run tasks on a device but haven't preloaded any test content, you can use Factory Orchestrator's file transfer feature, the Factory Orchestrator API, or use TShell or SSH to copy files.


