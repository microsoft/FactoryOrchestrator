
# Factory Orchestrator Service Configuration
## (Optional & for advanced users only) Enable network access
By default, the Factory Orchestrator service only allows client connections from the same device the service is running on (i.e. localhost only). However, service can be configured to allow connections from clients anywhere on your local network.

<b>
<p align="center">⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠</p>
<p align="center"><i>WARNING: Please read and understand the following before enabling network access!</i></p>
<p align="center">⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠⚠</p>
</b>

- ⚠ The service allows any client to connect to it without authentication. Any connected client has full access to the service's computer, including the ability to send or copy files, and/or run any command or program with administrator rights. ⚠
- ⚠ If you "install" the service, the service will be configured to run from boot. Depending on the configuration the service may even be running before a user has logged on to the computer. ⚠
- ⚠ Once network access is enabled, it will remain enabled until the changes to enable network access are [reverted](#disable-network-access). ⚠
- ⚠ The service and client send information over the network in unencrypted JSON using TCP. It is therefore vulnerable to man-in-the-middle attacks. ⚠
- ⚠ The service currently has minimal logging about what clients are connected to it and what commands each client has executed. ⚠


If you understand these risks but still require the service to communicate with clients over the local network, set both of the following registry values on the service's computer using regedit.exe or reg.exe:

Key | Value | [Type](../CoreLibrary/Microsoft-FactoryOrchestrator-Core-TaskBase-Type/) | Data
------ | ------ | --- | ---
HKLM\SYSTEM\CurrentControlSet\Control\FactoryOrchestrator | EnableNetworkAccess | REG_DWORD | 0x1
HKLM\SYSTEM\CurrentControlSet\Control\FactoryOrchestrator | DisableNetworkAccess | REG_DWORD | 0x0

Once the two registry values are set, restart the service if it is running.

To check if network access is currently enabled use one of the following:

- The Factory Orchestrator app's "About" page.
- The console output from Microsoft.FactoryOrchestrator.Service.exe
- The [service log file](../#factory-orchestrator-service-log-file)
- The [IsNetworkAccessEnabled](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-IsNetworkAccessEnabled%28%29/) API.

## Disable network access
If you have enabled network access using the above steps, you can delete the two registry values above to disable network access. Restart the service if it is running.

To check if network access is currently disabled use one of the following:

- The Factory Orchestrator app's "About" page.
- The console output from Microsoft.FactoryOrchestrator.Service.exe
- The [service log file](../index#factory-orchestrator-service-log-file)
- The [IsNetworkAccessEnabled](../ClientLibrary/Microsoft-FactoryOrchestrator-Client-FactoryOrchestratorClient-IsNetworkAccessEnabled%28%29/) API.
