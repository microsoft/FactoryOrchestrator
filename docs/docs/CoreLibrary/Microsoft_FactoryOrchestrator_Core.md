## Microsoft.FactoryOrchestrator.Core Namespace

| Classes | |
| :--- | :--- |
| [AppPackages](Microsoft_FactoryOrchestrator_Core_AppPackages.md 'Microsoft.FactoryOrchestrator.Core.AppPackages') | Object representing a list of Application Packages<br/> |
| [BatchFileTask](Microsoft_FactoryOrchestrator_Core_BatchFileTask.md 'Microsoft.FactoryOrchestrator.Core.BatchFileTask') | An BatchFile is a .cmd or .bat script that is run by the FactoryOrchestratorServer. The exit code of the script determines if the task passed or failed.<br/>0 == PASS, all others == FAIL.<br/> |
| [CommandLineTask](Microsoft_FactoryOrchestrator_Core_CommandLineTask.md 'Microsoft.FactoryOrchestrator.Core.CommandLineTask') | An CommandLineTask is a .cmd, .bat, or .sh script. This is known as a Batch file on Windows and a Shell script on Linux. The exit code of the script determines if the task passed or failed.<br/>0 == PASS, all others == FAIL.<br/> |
| [Constants](Microsoft_FactoryOrchestrator_Core_Constants.md 'Microsoft.FactoryOrchestrator.Core.Constants') | Class for any cross-project constants.<br/> |
| [ExecutableTask](Microsoft_FactoryOrchestrator_Core_ExecutableTask.md 'Microsoft.FactoryOrchestrator.Core.ExecutableTask') | An ExecutableTask is an .exe binary that is run by the FactoryOrchestratorServer. The exit code of the process determines if the task passed or failed.<br/>0 == PASS, all others == FAIL.<br/> |
| [ExternalTask](Microsoft_FactoryOrchestrator_Core_ExternalTask.md 'Microsoft.FactoryOrchestrator.Core.ExternalTask') | An ExternalTest is a task run outside of the FactoryOrchestratorServer.<br/>task results must be returned to the server via SetTaskRunStatus().<br/> |
| [FactoryOrchestratorContainerDisabledException](Microsoft_FactoryOrchestrator_Core_FactoryOrchestratorContainerDisabledException.md 'Microsoft.FactoryOrchestrator.Core.FactoryOrchestratorContainerDisabledException') | An exception denoting that container support is disabled.<br/> |
| [FactoryOrchestratorContainerException](Microsoft_FactoryOrchestrator_Core_FactoryOrchestratorContainerException.md 'Microsoft.FactoryOrchestrator.Core.FactoryOrchestratorContainerException') | An exception denoting an issue with the container on the device.<br/> |
| [FactoryOrchestratorException](Microsoft_FactoryOrchestrator_Core_FactoryOrchestratorException.md 'Microsoft.FactoryOrchestrator.Core.FactoryOrchestratorException') | A generic Factory Orchestrator exception.<br/> |
| [FactoryOrchestratorTaskListRunningException](Microsoft_FactoryOrchestrator_Core_FactoryOrchestratorTaskListRunningException.md 'Microsoft.FactoryOrchestrator.Core.FactoryOrchestratorTaskListRunningException') | An exception denoting a running TaskList is preventing the operation from succeeding.<br/> |
| [FactoryOrchestratorUnkownGuidException](Microsoft_FactoryOrchestrator_Core_FactoryOrchestratorUnkownGuidException.md 'Microsoft.FactoryOrchestrator.Core.FactoryOrchestratorUnkownGuidException') | An exception denoting the given GUID is not recognized by Factory Orchestrator.<br/> |
| [FactoryOrchestratorXML](Microsoft_FactoryOrchestrator_Core_FactoryOrchestratorXML.md 'Microsoft.FactoryOrchestrator.Core.FactoryOrchestratorXML') | This class is used to save and load TaskLists from an XML file.<br/> |
| [FactoryOrchestratorXmlException](Microsoft_FactoryOrchestrator_Core_FactoryOrchestratorXmlException.md 'Microsoft.FactoryOrchestrator.Core.FactoryOrchestratorXmlException') | An exception denoting an issue with given FactoryOrchestratorXML file.<br/> |
| [LockingList&lt;T&gt;](Microsoft_FactoryOrchestrator_Core_LockingList_T_.md 'Microsoft.FactoryOrchestrator.Core.LockingList&lt;T&gt;') | A wrapper for the List class that locks the list on any modification operations.<br/> |
| [NotifyPropertyChangedBase](Microsoft_FactoryOrchestrator_Core_NotifyPropertyChangedBase.md 'Microsoft.FactoryOrchestrator.Core.NotifyPropertyChangedBase') | Abstract class to implement INotifyPropertyChanged<br/> |
| [PackageInfo](Microsoft_FactoryOrchestrator_Core_PackageInfo.md 'Microsoft.FactoryOrchestrator.Core.PackageInfo') | object representing the package information<br/> |
| [PackageVersion](Microsoft_FactoryOrchestrator_Core_PackageVersion.md 'Microsoft.FactoryOrchestrator.Core.PackageVersion') | Object representing a package version<br/> |
| [PowerShellTask](Microsoft_FactoryOrchestrator_Core_PowerShellTask.md 'Microsoft.FactoryOrchestrator.Core.PowerShellTask') | An PowerShellTask is a PowerShell Core .ps1 script that is run by the FactoryOrchestratorServer. The exit code of the script determines if the task passed or failed.<br/>0 == PASS, all others == FAIL.<br/> |
| [ServiceEvent](Microsoft_FactoryOrchestrator_Core_ServiceEvent.md 'Microsoft.FactoryOrchestrator.Core.ServiceEvent') | A class containing information about a specific Factory Orchestrator Service event.<br/> |
| [TAEFTest](Microsoft_FactoryOrchestrator_Core_TAEFTest.md 'Microsoft.FactoryOrchestrator.Core.TAEFTest') | A TAEFTest is a type of ExecutableTask, which is always run by TE.exe. TAEF tests are comprised of one or more sub-tests (TAEFTestCase).<br/>Pass/Fail is determined by TE.exe.<br/> |
| [TaskBase](Microsoft_FactoryOrchestrator_Core_TaskBase.md 'Microsoft.FactoryOrchestrator.Core.TaskBase') | TaskBase is an abstract class representing a generic task. It contains all the details needed to run the task.<br/>It also surfaces information about the last TaskRun for this task, for easy consumption.<br/> |
| [TaskList](Microsoft_FactoryOrchestrator_Core_TaskList.md 'Microsoft.FactoryOrchestrator.Core.TaskList') | A TaskList is a grouping of Factory Orchestrator Tasks.<br/> |
| [TaskRun](Microsoft_FactoryOrchestrator_Core_TaskRun.md 'Microsoft.FactoryOrchestrator.Core.TaskRun') | A TaskRun represents one instance of executing any single Task.<br/> |
| [UWPTask](Microsoft_FactoryOrchestrator_Core_UWPTask.md 'Microsoft.FactoryOrchestrator.Core.UWPTask') | A UWPTest is a UWP task run by the FactoryOrchestrator.App client. These are used for UI.<br/>task results must be returned to the server via SetTaskRunStatus().<br/> |

| Structs | |
| :--- | :--- |
| [TaskListSummary](Microsoft_FactoryOrchestrator_Core_TaskListSummary.md 'Microsoft.FactoryOrchestrator.Core.TaskListSummary') | A helper class containing basic information about a TaskList. Use to quickly update clients about TaskLists and their statuses.<br/> |

| Interfaces | |
| :--- | :--- |
| [IFactoryOrchestratorService](Microsoft_FactoryOrchestrator_Core_IFactoryOrchestratorService.md 'Microsoft.FactoryOrchestrator.Core.IFactoryOrchestratorService') | IFOCommunication defines the Factory Orchestrator Client-Server communication model.<br/> |

| Enums | |
| :--- | :--- |
| [ServiceEventType](Microsoft_FactoryOrchestrator_Core_ServiceEventType.md 'Microsoft.FactoryOrchestrator.Core.ServiceEventType') | The type of Factory Orchestrator Service event.<br/> |
| [TaskStatus](Microsoft_FactoryOrchestrator_Core_TaskStatus.md 'Microsoft.FactoryOrchestrator.Core.TaskStatus') | The status of a Task, TaskRun, or TaskList.<br/> |
| [TaskType](Microsoft_FactoryOrchestrator_Core_TaskType.md 'Microsoft.FactoryOrchestrator.Core.TaskType') | The type of Task.<br/> |
