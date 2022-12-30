<!-- Copyright (c) Microsoft Corporation. -->
<!-- Licensed under the MIT license. -->


# Tasks and TasksLists

Factory Orchestrator uses "Tasks" to capture a single action. Tasks can be executables, scripts, apps, TAEF tests, or external actions. TasksLists are used to order and group Tasks.

You can define a collection of tasks in a **[TaskList](../CoreLibrary/Microsoft_FactoryOrchestrator_Core_TaskList/)**. Tasks in a TaskList are run in a defined order, and can be a mixture that includes any type of tasks that's supported by Factory Orchestrator. TaskList data persists through reboots. TaskList data is stored and maintained by the Factory Orchestrator service, and doesn't depend on the app being open or running. Tasks in a TaskList can be configured to run in series, parallel, or in the background.

Factory Orchestrator supports using environment variables (ex: %ProgramData%, $TMPDIR) in all Tasks.

## Factory Orchestrator tasks

## Task types

Factory Orchestrator TaskLists allow adding different types of tasks:

- **Executable**

    These tasks are executable files (.exe on Windows) which are run directly. When you add this type of task, you can specify additional arguments and if the task should run as a background task.

- **CommandLine**

    On Windows, these tasks are .bat or .cmd files which are run by the command prompt (cmd.exe). On Linux, they are .sh files which are run by bash. When you add this type of task, you can specify additional arguments and if the task should run as a background task.

- **PowerShell**

    These tasks are .ps1 files which are run by PowerShell 7+ (pwsh). When you add this type of task, you can specify additional arguments and if the task should run as a background task. On Windows these tasks will use "Windows PowerShell" (powershell.exe) if PowerShell 7+ is not installed.

- **TAEF**

    **Windows Only.** [TAEF tests](https://docs.microsoft.com/windows-hardware/drivers/taef/) can be added to a TaskList, and you can specify arguments and if the task should run as a background task.

- **UWP**

    **Windows Only.** Allows you to run a UWP app as a task. See [here](get-started-with-factory-orchestrator.md#windows-uwp-app-support) for setup requirments.

    UWP apps cannot take arguments (though you can use arguments to pass info to the operator about the goal of the Task), nor can they automatically return a pass/fail result. Instead, the operator must manually specify if the app passed or failed via the UpdateTaskRun() API or via a result prompt that the Factory Orchestrator App launches when the app exits. If using the app, the operator must manually specify if the task passed or failed via a result prompt that the Factory Orchestrator App launches when the app exits, like is shown on this screen:  

    ![External task windows](./images/externalTaskNoMedia.png)

    The Factory Orchestrator service can launch apps even if the Factory Orchestrator app isn't running.

- **External**

    These are tasks that require interaction from a technician before completing. These types of tasks could be used to tell a technician to connect a cable, check a device for scratches, move the device to the next station, etc.

    External Tasks can't automatically return a pass/fail result. The operator must manually specify if the Task passed or failed via the UpdateTaskRun() API or via a result prompt that the Factory Orchestrator App launches when the task executes.

    If using the app, the operator must manually specify if the task passed or failed via a result prompt that the Factory Orchestrator App launches when the app exits, like is shown on this screen:  

    ![External task windows](./images/externalTaskNoMedia.png)

    External tasks also support displaying custom images or video as part of the task. These images and videos are intended to allow external tasks to convey more information to technicians on the factory floor. When you create an external task, use **Arguments** to let the operator know what to expect or what actions to take. When you create an external task, use the **Image or Video Path:** field to specify an image or a video.

    ![screen shot of external task image](./images/externalTaskPicture.png) ![screen shot of external task video](./images/externalTaskVideo.png)

### Background tasks

A [BackgroundTask](../CoreLibrary/Microsoft_FactoryOrchestrator_Core_TaskRun_BackgroundTask/) is a type of Task which is not expected to return a pass/fail result. Instead, [BackgroundTasks](../CoreLibrary/Microsoft_FactoryOrchestrator_Core_TaskList-BackgroundTasks/) are started before any Tasks defined in the TaskList, and are not tracked by the Factory Orchestrator Service, though their output is logged to a file. [BackgroundTasks](../CoreLibrary/Microsoft_FactoryOrchestrator_Core_TaskList-BackgroundTasks/) are intended to be used for logging/monitoring tasks that need to be running before any Task in the TaskList executes.

BackgroundTasks are defined the exactly the same as a normal Task with the following exceptions:

- [BackgroundTasks](../CoreLibrary/Microsoft_FactoryOrchestrator_Core_TaskList_BackgroundTasks/) can only be an Executable, PowerShell, or BatchFile Task
- [BackgroundTasks](../CoreLibrary/Microsoft_FactoryOrchestrator_Core_TaskList_BackgroundTasks/) cannot have Timeout or [MaxNumberOfRetries](../CoreLibrary/Microsoft_FactoryOrchestrator_Core_TaskBase-MaxNumberOfRetries/) set

When editing a task from the Factory Orchestrator app, you can choose the option of making the task a background task by choosing the "Add as background task?" option.

Once you've run a task, the Factory Orchestrator service creates a **[TaskRun](../CoreLibrary/Microsoft_FactoryOrchestrator_Core_TaskRun/)** that is the output and results of the task, as well as other details about the task such as runtime.

## Author and manage Factory Orchestrator TaskLists with FactoryOrchestratorXML

Factory Orchestrator uses XML files, called FactoryOrchestratorXML, to manage TaskLists and their associated Tasks. An XML file can contain one or more TaskLists, each with any number of Tasks.

The XML can either be hand-authored; or authored, imported, and/or exported using the [Factory Orchestrator app's "Manage TaskLists"](use-the-factory-orchestrator-app.md#managing-tasklists) page.

You can get started with Factory Orchestrator TaskLists by using the [**Manage TaskLists**](use-the-factory-orchestrator-app.md#managing-tasklists) page in the Factory Orchestrator app to create a TaskList.

### Factory Orchestrator XML Schema

When hand-authoring FactoryOrchestratorXML files, you'll need to follow the FactoryOrchestratorXML schema. At the end of this topic, we've also provided a [sample FactoryOrchestratorXML file](#sample-factory-orchestrator-xml-file):

```XML
  <?xml version="1.0" encoding="utf-8"?>
  <xs:schema id="FactoryOrchestratorXML"
      elementFormDefault="qualified"
      xmlns:xs="http://www.w3.org/2001/XMLSchema"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
  >
    <xs:element name="TaskRunGuids">
      <xs:complexType>
        <xs:sequence>
            <xs:element name="Guid" type="xs:string" minOccurs="1" maxOccurs="unbounded"/>
        </xs:sequence>
      </xs:complexType>
    </xs:element>

    <xs:complexType name="TaskType">
      <xs:all>
        <xs:element name="LatestTaskRunTimeStarted" type="xs:dateTime" minOccurs="0" maxOccurs="1"/>
        <xs:element name="LatestTaskRunTimeFinished" type="xs:dateTime" minOccurs="0" maxOccurs="1"/>
        <xs:element name="LatestTaskRunStatus" type="xs:string" minOccurs="0" maxOccurs="1"/>
        <xs:element name="LatestTaskRunExitCode" type="xs:int" minOccurs="0" maxOccurs="1"/>
        <xs:element name="TimesRetried" type="xs:int" minOccurs="0" maxOccurs="1"/>
        <xs:element ref="TaskRunGuids" minOccurs="0" maxOccurs="1"/>
      </xs:all>
      <xs:attribute type="xs:string" name="Name" use="optional"/>
      <xs:attribute type="xs:string" name="Path" use="optional"/>
      <xs:attribute type="xs:string" name="Guid" use="optional"/>
      <xs:attribute type="xs:string" name="Arguments" use="optional"/>
      <xs:attribute type="xs:int" name="MaxNumberOfRetries" use="optional"/>
      <xs:attribute type="xs:int" name="Timeout" use="optional"/>
      <xs:attribute type="xs:boolean" name="AbortTaskListOnFailed" use="optional"/>
      <xs:attribute type="xs:boolean" name="AutoPassedIfLaunched" use="optional"/>
      <xs:attribute type="xs:boolean" name="TerminateOnCompleted" use="optional"/>
    </xs:complexType>

    <xs:complexType name="TasksType">
      <xs:sequence>
        <xs:element name="Task" type="TaskType" minOccurs="1" maxOccurs="unbounded"/>
      </xs:sequence>
    </xs:complexType>

      <xs:element name="TaskList">
        <xs:complexType>
          <xs:sequence>
            <xs:element name="Tasks" type="TasksType" minOccurs="0" maxOccurs="1"/>
            <xs:element name="BackgroundTasks" type="TasksType" minOccurs="0" maxOccurs="1"/>
          </xs:sequence>
          <xs:attribute name="Name" type="xs:string" use="required"/>
          <xs:attribute name="Guid" type="xs:string" use="optional"/>
          <xs:attribute name="RunInParallel" type="xs:boolean" use="required"/>
          <xs:attribute name="AllowOtherTaskListsToRun" type="xs:boolean" use="required"/>
          <xs:attribute name="TerminateBackgroundTasksOnCompletion" type="xs:boolean" use="optional"/>
        </xs:complexType>
      </xs:element>
      <xs:element name="TaskLists">
        <xs:complexType>
          <xs:sequence>
            <xs:element ref="TaskList" minOccurs="1" maxOccurs="unbounded"/>
          </xs:sequence>
        </xs:complexType>
      </xs:element>
      <xs:element name="FactoryOrchestratorXML">
        <xs:complexType>
          <xs:sequence>
            <xs:element ref="TaskLists" minOccurs="1" maxOccurs="1"/>
          </xs:sequence>
        </xs:complexType>
      </xs:element>  
  </xs:schema>
```

### TaskList attributes

A TaskList element defines a Factory Orchestrator TaskList. The following defines that attributes of the `TaskList` element in the Factory Orchestrator XML schema.

| Attribute Name                        | Type    | Required?  | Details                                                                                                                                                                                          |
|---------------------------------------|---------|------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Name                                  | String  | Y          | The "friendly name" of the TaskList.                                                  |
| Guid                                  | String  | N          | The GUID used to identify the TaskList. If not set, it will be assigned by the Factory Orchestrator Service automatically when the FactoryOrchestratorXML is loaded.                             |
| RunInParallel                         | Bool    | Y          | If "true", the Tasks in this TaskList are executed in parallel. If "false", the Tasks in this TaskList are executed in order, one at a time.                                                     |
| AllowOtherTaskListsToRun              | Bool    | Y          | If "false", while this TaskList is running all other TaskLists are blocked from executing. If "true", other TaskLists may execute while this TaskList is running.                                 |
| TerminateBackgroundTasksOnCompletion  | Bool    | N          | If "true", any [BackgroundTasks](../CoreLibrary/Microsoft_FactoryOrchestrator_Core_TaskList-BackgroundTasks/) defined in this TaskList are forcibly terminated when the TaskList's Tasks complete. If "false", any [BackgroundTasks](../CoreLibrary/Microsoft_FactoryOrchestrator_Core_TaskList-BackgroundTasks/) defined in this TaskList continue executing. Defaults to "true". |

#### Sample TaskList element

```xml
<TaskList Guid="15332a4b-ee08-4c97-a7ad-d69d4210c3a6" Name="List2" RunInParallel="true" AllowOtherTaskListsToRun="true" TerminateBackgroundTasksOnCompletion="true">
```

### Task attributes

A Task element defines a Factory Orchestrator Task. Tasks are pass/fail executables, apps, or jobs that are run as part of the TaskList that defines them. The following defines that attributes of the `Task` element in the Factory Orchestrator XML schema.

<!--Delete this table-->
| Attribute Name         | Type         | Required?    | Details                                                                                                                                                                                                                   |
|------------------------|--------------|--------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| xsi:type               | See details  | Y            | The type of the Task. Allowed values are: [ExecutableTask](../CoreLibrary/Microsoft_FactoryOrchestrator_Core_ExecutableTask/), [PowerShellTask](../CoreLibrary/Microsoft_FactoryOrchestrator_Core_PowerShellTask/), BatchFileTask, TAEFTest, [UWPTask](../CoreLibrary/Microsoft_FactoryOrchestrator_Core_UWPTask/), and [ExternalTask](../CoreLibrary/Microsoft_FactoryOrchestrator_Core_ExternalTask/).                                                                                             |
| Name                   | String       | N            | The "friendly name" of the Task. If not set, it will be assigned by the Factory Orchestrator Service automatically when the FactoryOrchestratorXML is loaded, based on the Task type and other attributes.                |
| Guid                   | String       | N            | The GUID used to identify the Task. If not set, it will be assigned by the Factory Orchestrator Service automatically when the FactoryOrchestratorXML is loaded.                                                          |
| Path                   | String       | Depends      | See the [Path table below](#path-definitions) to see which Tasks require you to include a Path element.  |                                                                                                                                     |
| Arguments              | String       | N            | For Executable, PowerShell, BatchFile, and TAEF Tasks: this is the list of arguments to provide to the executable you specified in the "Path".<br><br>   For UWP Tasks: this can be used to provide details about the Task to the client. It is NOT passed to the UWP app.<br><br>For External Tasks: this can be used to provide details about the Task to the client.       |
| Timeout                | Int          | N            | In seconds, the amount of time to wait for the Task to be completed. Defaults to "-1" (infinite).<br><br> If "-1", the Task will never timeout.<br><br>If the timeout is reached, the Task status is set to "Timeout", a failed state. The Task's executable is also forcibly terminated (if it has one).                                                                                                                        |
| MaxNumberOfRetries     | Int          | N            | The number of times the Task should automatically be re-run if it completes in a failed state (Aborted/Failed/Timeout). Defaults to "0" (do not retry).<br><br>For example, if this is set to "2", the Task could be run up to 3 times automatically.                                                                         |
| AbortTaskListOnFailed  | Bool         | N            | If "true", if the Task is run during a TaskList and the Task fails (Aborted/Failed/Timeout), the TaskList is aborted in its current state. Any other pending or running Tasks will be aborted.<br><br>This action takes place after any re-runs specified by [MaxNumberOfRetries](../CoreLibrary/Microsoft_FactoryOrchestrator_Core_TaskBase-MaxNumberOfRetries/).<br><br>While allowed, it is not recommended to use this for "RunInParallel" TaskLists, as the execution order of such a TaskList is not guaranteed, and Tasks may be aborted mid-execution.                                    |
| TerminateOnCompleted   | Bool         | N            | By default, an app is terminated when the [UWPTask](../CoreLibrary/Microsoft_FactoryOrchestrator_Core_UWPTask/) completes. Set to false to not terminate after a [UWPTask](../CoreLibrary/Microsoft_FactoryOrchestrator_Core_UWPTask/) completes.  TerminateOnCompleted is ignored if AutoPassedIfLaunched=`true` |
| AutoPassedIfLaunched   | Bool         | N            | By default, a [UWPTask](../CoreLibrary/Microsoft_FactoryOrchestrator_Core_UWPTask/) waits for its [TaskRun](../CoreLibrary/Microsoft_FactoryOrchestrator_Core_TaskRun/) to be completed by a Factory Orchestrator Client. Setting this to true marks the UWP task completed when the app is launched. |

#### Path definitions

| Task type | Required | Path definition |
| --- | --- | --- |
| Executable  | Yes |   The path to the executable file that should be executed for this Task. |
| PowerShell | Yes | The path to the PowerShell file that should be executed for this Task. |
| BatchFile | Yes | The path to the Batch file that should be executed for this Task. |
| TAEF | Yes | The path to the TAEF test that should be executed for this Task. |
| UWP | Yes | the PackageFamilyName of the app you wish to launch. The PackageFamilyName is found in the package.appxmanifest for your app. It is also shown on the Factory Orchestrator app's "UWP Apps" page. |
| External | No | This is optional, but can be used to provide details about the Task. |

#### Sample Task element

```xml
<Task xsi:type="ExecutableTask" Name="Exe with abort tasklist on fail" Path="%DataDrive%\TestContent\testapp4.exe" Arguments="" Guid="6279616a-345d-4469-bba0-fd019c78b531" AbortTaskListOnFailed="true"/>
```

### Background tasks

A Background Task is a type of Task which is not expected to return a pass/fail result. Instead, Background Tasks are started before any Tasks defined in the TaskList, and are not tracked by the Factory Orchestrator Service, though their output is logged to a file. Background Tasks are intended to be used for logging/monitoring tasks that need to be running before any Task in the TaskList executes.

Background Tasks are defined as children of the `<BackgroundTasks>` element. That element can have any number of child `<Task>` elements which are run as Background Tasks.

The `TerminateBackgroundTasksOnCompletion` attribute on the owning TaskList determines if the Background Tasks are forcibly terminated when the TaskList is done executing.

Background Tasks are defined the exactly the same as a normal Task with the following exceptions:

- Any Executable, PowerShell, or Batch File Task can be made a Background Task.
- Background Tasks cannot have Timeout or [MaxNumberOfRetries](../CoreLibrary/Microsoft_FactoryOrchestrator_Core_TaskBase-MaxNumberOfRetries/) set

#### Sample [BackgroundTasks](../CoreLibrary/Microsoft_FactoryOrchestrator_Core_TaskList-BackgroundTasks/) element

```xml
<BackgroundTasks>
    <Task xsi:type="ExecutableTask" Name="Background Monitor" Path="%DataDrive%\TestContent\SomeBackgroundProcess.exe" Arguments="" Guid="34d3827c-6397-411f-85a6-7e92dca5f364"/>
</BackgroundTasks>
```

### Validate Factory Orchestrator XML

You can validate FactoryOrchestratorXML using the Factory Orchestrator app on a Windows PC, even without having to connect to a Factory Orchestrator service.

1. [Install the Factory Orchestrator app](../get-started-with-factory-orchestrator/#install-the-app) on a Windows PC and launch it.
2. Click "Validate FactoryOrchestratorXML" in the bottom left of the app's connect page.
3. Browse to the path of your FactoryOrchestratorXML file and click open.
4. The FactoryOrchestratorXML file will be validated against the schema. Because this validation happens on the Windows PC, it will only catch XML syntax errors. It will not catch runtime errors such as duplicate GUIDs or invalid file paths.

    - If the FactoryOrchestratorXML is valid you will see a success message saying that "FactoryOrchestratorXML was successfully validated."

    - If the FactoryOrchestratorXML is invalid, you'll see a message that says "FactoryOrchestratorXML failed validation", with a description of why it failed validation.
### Sample Factory Orchestrator XML file

The following sample FactoryOrchestratorXML file shows two TaskLists containing various types of tests, as well as a [BackgroundTask](../CoreLibrary/Microsoft_FactoryOrchestrator_Core_TaskRun-BackgroundTask/) that is part of the first TaskList.

```xml
<?xml version="1.0" encoding="utf-8"?>
<FactoryOrchestratorXML xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <TaskLists>
  <!-- RunInParallel = all tasks can be executed simultaneously. AllowOtherTaskListsToRun = other tasklists can run in parallel with this one. TerminateBackgroundTasksOnCompletion = Any still running background tasks are forcefully terminated when all tasks complete -->
    <TaskList Guid="15332a4b-ee08-4c97-a7ad-d69d4210c3a5" Name="List1" RunInParallel="false" AllowOtherTaskListsToRun="true" TerminateBackgroundTasksOnCompletion="true">
      <Tasks>
        <!-- When a UWPTask is launched from Factory Orchestrator, the Factory Orchestrator app will prompt the user for the result of the app after it terminates. -->
        <!-- UWPTasks require Factory Orchestrator app is running! -->
        <Task xsi:type="UWPTask" Name="Launch app directly via FO (exit with ALT+F4)" Path="Microsoft.FactoryOrchestratorApp_8wekyb3d8bbwe" Arguments="" Guid="6ec52ded-e455-44cb-b861-b8d78996db95"/>
        <Task xsi:type="TAEFTest" Name="TAEF test" Path="%DataDrive%\TestContent\taeftest1.dll" Arguments="" Guid="ff908b05-e491-4a6e-9305-793b3c2d7f97"/>
        <Task xsi:type="ExecutableTask" Name="Exe with 5 retries" Path="%DataDrive%\TestContent\testapp1.exe" Arguments="" Guid="d75f7c9c-8de9-4d0f-b6dc-d2a51d8818cb" MaxNumberOfRetries="5"/>
        <Task xsi:type="ExecutableTask" Name="Exe with 10 second timeout" Path="%DataDrive%\TestContent\testapp2.exe" Arguments="" Guid="7ffd4516-b092-4f22-beec-f12c814f50dc" Timeout="10"/>
        <Task xsi:type="ExecutableTask" Name="Exe with arguments" Path="%DataDrive%\TestContent\testapp3.exe" Arguments="-arg1 abc -arg2 def" Guid="186e4b74-280f-45ca-a73b-a0b342fa1cb8" />
        <Task xsi:type="ExecutableTask" Name="Exe with abort tasklist on fail" Path="%DataDrive%\TestContent\testapp4.exe" Arguments="" Guid="6279616a-345d-4469-bba0-fd019c78b531" AbortTaskListOnFailed="true"/>
        <Task xsi:type="BatchFileTask" Name="Cmd/Batch file with arguments" Path="%DataDrive%\TestContent\cmdfile.cmd" Arguments="-arg1 abc" Guid="cf334ad7-4480-4915-b84c-2a38c84b4e8c"/>
        <Task xsi:type="PowerShellTask" Name="RunAppViaDevicePortal.ps1 (PowerShell Core, exit app with ALT+F4)" Path="%DataDrive%\TestContent\RunAppViaDevicePortal.ps1" Arguments="-AppId Microsoft.FactoryOrchestratorApp_8wekyb3d8bbwe!App" Guid="1f596b55-7c9b-4e71-97b4-a0c93c098a86"/>
        <!-- When a ExternalTask is launched from Factory Orchestrator, the Factory Orchestrator app will prompt the user for the result of task -->
        <Task xsi:type="ExternalTask" Name="External Task (No executable code or app)" Path="User interaction required" Arguments="Plug in power cord" Guid="f90ba40d-4982-46dc-b9a2-c1cd7900fef7" AbortTaskListOnFailed="true"/>
      </Tasks>
      <!-- Background Tasks are started before any other task in the TaskList, they are intended for processes that you want running throughout the entire tasklist, ex: a monitor or logging program -->
      <!-- BackgroundTasks can be ExecutableTask, BatchFileTask, or PowerShellTask -->
      <BackgroundTasks>
        <Task xsi:type="ExecutableTask" Name="Background Monitor" Path="%DataDrive%\TestContent\SomeBackgroundProcess.exe" Arguments="" Guid="34d3827c-6397-411f-85a6-7e92dca5f364"/>
      </BackgroundTasks>
    </TaskList>
    <!-- FactoryOrchestratorXML can contain multiple tasklists, so you can define your entire factory flow in one file, if desired. -->
    <TaskList Guid="15332a4b-ee08-4c97-a7ad-d69d4210c3a6" Name="List2" RunInParallel="true" AllowOtherTaskListsToRun="true" TerminateBackgroundTasksOnCompletion="true">
      <Tasks>
        <Task xsi:type="ExecutableTask" Name="Exe in another tasklist" Path="%DataDrive%\TestContent\testapp5.exe" Arguments="" Guid="45945e43-d251-4c97-a9ad-63cd52b09801"/>
      </Tasks>
    </TaskList>
  </TaskLists>
</FactoryOrchestratorXML>
```

## Configure Factory Orchestrator to automatically load or execute TaskLists when it starts

Factory Orchestrator looks for certain FactoryOrchestratorXML files when it starts. You can use these FactoryOrchestratorXML files to pre-load tasks into Factory Orchestrator, run tasks the first time a device boots, or run tasks every time a device boots.

See [Service Configuration](service-configuration.md#configure-factory-orchestrator-to-automatically-load-or-execute-tasklists-when-it-starts) for information on how to setup this.

## Factory Orchestrator Task log files

The Task log files contain details about the execution of a specific of the Factory Orchestrator Task. There is one log file generated for each run of a Task ([TaskRun](../CoreLibrary/Microsoft_FactoryOrchestrator_Core_TaskRun/)). The files are saved to `%ProgramData%\FactoryOrchestrator\Logs\` on a Windows and `/var/log/FactoryOrchestrator/logs` on Linux, but this location can be changed using the [FactoryOrchestratorClient](../ClientLibrary/Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_FactoryOrchestratorClient%28System_Net_IPAddress_int%29/).[SetLogFolder](../ClientLibrary/Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_SetLogFolder%28string_bool%29/)() API. Use the [FactoryOrchestratorClient](../ClientLibrary/Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_FactoryOrchestratorClient%28System_Net_IPAddress_int%29/).[GetLogFolder](../ClientLibrary/Microsoft_FactoryOrchestrator_Client_FactoryOrchestratorClient_GetLogFolder%28%29/)() API to programmatically retrieve the active log folder.
