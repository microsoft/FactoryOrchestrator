<!-- Copyright (c) Microsoft Corporation. -->
<!-- Licensed under the MIT license. -->


# Uninstall Factory Orchestrator

We're sorry to see you go! Please consider filing an [issue on GitHub](https://github.com/microsoft/FactoryOrchestrator/issues) with any questions, bugs, or feedback you have first. There is a good chance we can help!

## Uninstall the service on Windows
To uninstall the service, run the following from an administrator PowerShell:
```PowerShell
    . "$env:ProgramFiles\FactoryOrchestrator\UninstallFactoryOrchestratorService.ps1"
```

If you installed the service to a different directory, use that above.

#### Execution Policy
If you see the following error, you need to set your ExecutionPolicy to RemoteSigned.

```File UninstallFactoryOrchestratorService.ps1 cannot be loaded because running scripts is disabled on this system. For more information, see about_Execution_Policies at https://go.microsoft.com/fwlink/?LinkID=135170.```

To resolve it, run the following command to temporarily allow you to run the install script:
```powershell
    # Temporarily allow your PC to run signed scripts
    Set-ExecutionPolicy RemoteSigned -Scope Process

    # Then, you can install the service!
    .\UninstallFactoryOrchestratorService.ps1 # -AutoStart
```

## Uninstall the app on Windows
To uninstall the app [follow these directions](https://support.microsoft.com/en-us/windows/uninstall-or-remove-apps-and-programs-in-windows-10-4b55f974-2cc6-2d2b-d092-5905080eaf98).

## Uninstall the service on Linux
To uninstall the service, run the following from bash:
```Bash
    /usr/sbin/FactoryOrchestrator/UninstallFactoryOrchestratorService.sh
```
