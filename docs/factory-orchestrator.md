---
ms.author: themar
author: themar-msft
ms.date: 09/30/2019
---

# Factory Orchestrator

Factory Orchestrator enables you to run manufacturing tests and tasks for hardware validation and diagnostics during the OEM manufacturing process.

Factory Orchestrator consists of two components: 

- A UWP app: Communicates with the service to run executable tasks and commands on a device under test (DUT).

- A system service (FactoryOrchestratorService.exe): The service persists task information, including TaskLists and task results; if you close the app and then reconnect to a device, even from a different PC, you'll see loaded TaskLists, tasks, and their status.

The UWP app can be run on the same device and/or over a network. The service runs on the DUT.

Factory Orchestrator can run multiple tasks at a time, either in series or in parallel, with minimal overhead.

## Factory Orchestrator features

Factory Orchestrator includes the following features that enable you to coordinate tasks that validate your manufacturing process:

- **TaskLists** - A collection of tasks that can be run in series, parallel, or in the background. Tasks can be executables, TAEF tests, UWP apps, or external tasks. TaskLists can even be configured to [run on first boot or every boot](get-started-with-factory-orchestrator.md#configure-factory-orchestrator-to-automatically-load-tasklists-when-it-starts).
- **Command prompt** - A simple, non-interactive, command prompt that allows you to troubleshoot without having use other methods to connect to your DUT.
- **File transfer** - A basic file transfer function that enables you to transfer files to and from your device when you're connected from a technician PC
- Ability to **launch UWP apps**

## Factory Orchestrator tasks

Factory Orchestrator can run executables and [TAEF tests](https://docs.microsoft.com/windows-hardware/drivers/taef/getting-started) that have been validated by [ApiValidator](https://docs.microsoft.com/windows-hardware/drivers/develop/validating-universal-drivers). It can also launch UWP apps and run cmd.exe and PowerShell scripts as tasks.

You can define a collection of tasks in a **TaskList** from within the Factory Orchestrator app. Tasks in a TaskList are run in a defined order, and can be a mixture that includes any type of tasks that's supported by Factory Orchestrator. TaskList data persists through reboots. TaskList data is stored and maintained by the Factory Orchestrator service, and doesn't depend on the app being open or running.

Once you've run a task, the Factory Orchestrator service creates a **TaskRun** that is the output and results of the task, as well as other details about the task such as runtime.

## Factory Orchestrator logs
By default, the Factory Orchestrator Service generates log files in the following location on the test device: `%ProgramData%\FactoryOrchestrator`.

### Factory Orchestrator Service log file
The service log file contains details about the operation of the Factory Orchestrator Service. It is always found at `%ProgramData%\FactoryOrchestrator\FactoryOrchestratorService.log` on a device. Inspect this log for details about the service's operation.

### Factory Orchestrator Task log files
The Task log files contain details about the executaion of a specific of the Factory Orchestrator Task. There is one log file generated for each run of a Task. The files are saved to `%ProgramData%\FactoryOrchestrator\Logs\` on a device by default, but this location can be changed using the FactoryOrchestratorClient.SetLogFolder() API. Use the FactoryOrchestratorClient.GetLogFolder() API to programmatically retrieve the log folder.
