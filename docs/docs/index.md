---
author: themar-msft;hajya
ms.date: 09/30/2019
---

# Factory Orchestrator
Factory Orchestrator, is a cross-platform service for organizing, executing, and logging a set of executable scripts, binaries, or ["Tasks"](tasks-and-tasklists.md) on a system. Factory Orchestrator tracks task information, including run unique per-run results and logging; even persisting task state to allow the service to be resilient to data loss due to system failure. Factory Orchestrator also provides an optional [UWP client app](use-the-factory-orchestrator-app.md) and [robust client API surface](use-the-factory-orchestrator-api.md) for clients to monitor & interact with the service via the App or C# .NET, C# UWP, or PowerShell code. The UWP app and and any other client can communicate with any Factory Orchestrator service running on the same system and/or over a network!


Factory Orchestrator provides a simple and reliable way to run and manage factory line validation and fault analysis workflows. Beyond the factory floor, Factory Orchestrator can be used during OS and hardware development to support various developer inner-loop and diagnostics activities.


Tasks are used to capture actions that the server can execute, and TaskLists are used to organize and manage these Tasks. Learn more about [Tasks and Tasklists](tasks-and-tasklists.md).

See [Getting started with Factory Orchestrator](get-started-with-factory-orchestrator.md) for details on how to install and run the UWP app and/or service.
