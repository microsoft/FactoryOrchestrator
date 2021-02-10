---
author: themar-msft;hajya
ms.date: 09/30/2019
---

# Factory Orchestrator

Factory Orchestrator provides a simple and reliable way to run and manage factory line validation and fault analysis workflows. Beyond the factory floor, Factory Orchestrator can be used during OS and hardware development to support various developer inner-loop and diagnostics activities.

Factory Orchestrator consists of two components:

- A .NET Core system service (Microsoft.FactoryOrchestrator.Service.exe): The service tracks task information, including run unique per-run results and logging; even persisting task state to allow the service to be resilient to data loss due to client failure. It also provides a robust API surface for clients to monitor & interact with the service via C# .NET, C# UWP, or PowerShell code. 

- A UWP app: Communicates with the service to monitor & run executable tasks and commands on a device under test (DUT). This app can communicate with the service running on the same device and/or over a network. The app is optional, the service does not depend on the app.

Tasks are used to capture actions that the server can execute, and TaskLists are used to organize and manage these Tasks. Learn more about [Tasks and Tasklists](tasks-and-tasklists.md).

See [Getting started with Factory Orchestrator](get-started-with-factory-orchestrator.md) for details on how to install and run the app and/or service.

## Factory Orchestrator logs

By default, the Factory Orchestrator Service generates log files in the following location on the device: `%ProgramData%\FactoryOrchestrator`.

### Factory Orchestrator Service log file

The service log file contains details about the operation of the Factory Orchestrator service. It is always found at `%ProgramData%\FactoryOrchestrator\FactoryOrchestratorService.log` on a device. Inspect this log for details about the service's operation.

### Factory Orchestrator Task log files

The Task log files contain details about the execution of a specific of the Factory Orchestrator Task. There is one log file generated for each run of a Task (TaskRun). The files are saved to `%ProgramData%\FactoryOrchestrator\Logs\` on a device by default, but this location can be changed using the FactoryOrchestratorClient.SetLogFolder() API. Use the FactoryOrchestratorClient.GetLogFolder() API to programmatically retrieve the log folder.
