// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;

namespace Microsoft.FactoryOrchestrator.Core
{
    /// <summary>
    /// Class for any cross-project constants.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Chunk size in bytes for file transfers.
        /// </summary>
        public const int FileTransferChunkSize = 1048576;
    }

    /// <summary>
    /// The type of Factory Orchestrator Service event.
    /// </summary>
    public enum ServiceEventType
    {
        /// <summary>
        /// External TaskRun is completed.
        /// </summary>
        DoneWaitingForExternalTaskRun,
        /// <summary>
        /// External TaskRun is waiting on an external action.
        /// </summary>
        WaitingForExternalTaskRun,
        /// <summary>
        /// TaskRun is waiting to be run by the container.
        /// </summary>
        WaitingForContainerTaskRun,
        /// <summary>
        /// The Factory Orchestrator Serivce threw an exception.
        /// </summary>
        ServiceError,
        /// <summary>
        /// The Factory Orchestrator Serivce is starting. It can now communicate with clients, but boot tasks may not be complete.
        /// </summary>
        ServiceStart,
        /// <summary>
        /// The Factory Orchestrator Serivce is fully started. Boot tasks are completed.
        /// </summary>
        BootTasksComplete,
        /// <summary>
        /// An unknown Factory Orchestrator Serivce event occurred.
        /// </summary>
        Unknown = int.MaxValue
    }

    /// <summary>
    /// A class containing information about a specific Factory Orchestrator Service event.
    /// </summary>
    public class ServiceEvent
    {
        /// <summary>
        /// JSON Constructor.
        /// </summary>
        [JsonConstructor]
        public ServiceEvent()
        {
            _guidStr = "";
            _eventType = ServiceEventType.Unknown;
        }

        /// <summary>
        /// Create a new ServiceEvent.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="guid"></param>
        /// <param name="message"></param>
        public ServiceEvent(ServiceEventType type, Guid? guid, String message)
        {
            _eventIndex = _indexCount++;
            _eventTime = DateTime.Now;
            _eventType = type;
            if (guid != null)
            {
                _guidStr = guid.ToString();
            }
            else
            {
                _guidStr = "";
            }

            _message = message;
        }


        /// <summary>
        /// The unique index of the event. Strictly increasing in order.
        /// </summary>
        [JsonRequired]
        public ulong EventIndex { get => _eventIndex; }

        /// <summary>
        /// The time of the event.
        /// </summary>
        public DateTime EventTime { get => _eventTime; }

        /// <summary>
        /// The type of the event.
        /// </summary>
        public ServiceEventType ServiceEventType { get => _eventType; }

        /// <summary>
        /// If not NULL, the object GUID associated with the event.
        /// </summary>
        public Guid? Guid
        {
            get
            {
                if (!String.IsNullOrWhiteSpace(_guidStr))
                {
                    return new Guid(_guidStr);
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// The message in the event.
        /// </summary>
        public String Message { get => _message; }

        [JsonRequired]
        private readonly ulong _eventIndex;

        [JsonRequired]
        private readonly ServiceEventType _eventType;

        [JsonRequired]
        private readonly DateTime _eventTime;

        [JsonRequired]
        private readonly string _message;

        [JsonRequired]
        private readonly string _guidStr;

        private static ulong _indexCount = 0;
    }

    /// <summary>
    /// IFOCommunication defines the Factory Orchestrator Client-Server communication model.
    /// </summary>
    public interface IFactoryOrchestratorService
    {
        // Every Method declaration must be one line only or AutoGenerateInterfaceHelper will fail!

        // Service APIs
        /// <summary>
        /// Stops all running Tasks and deletes all TaskLists.
        /// </summary>
        /// <param name="preserveLogs">If true, all logs are deleted.</param>
        /// <param name="factoryReset">If true, the service is restarted as if it is first boot.</param>
        void ResetService(bool preserveLogs = false, bool factoryReset = false);
        /// <summary>
        /// Gets all Service events.
        /// </summary>
        /// <returns>List of all Service events.</returns>
        List<ServiceEvent> GetServiceEvents();
        /// <summary>
        /// Get all Service events since given time.
        /// </summary>
        /// <param name="timeLastChecked"></param>
        /// <returns>List of Service events.</returns>
        List<ServiceEvent> GetServiceEvents(DateTime timeLastChecked);
        /// <summary>
        /// Get all Service events since given index.
        /// </summary>
        /// <param name="lastEventIndex"></param>
        /// <returns>List of Service events.</returns>
        List<ServiceEvent> GetServiceEvents(ulong lastEventIndex);
        /// <summary>
        /// Get last Service error.
        /// </summary>
        /// <returns></returns>
        ServiceEvent GetLastServiceError();
        /// <summary>
        /// Returns the version of Factory Orchestrator Service.
        /// </summary>
        /// <returns>string representing the Service version.</returns>
        string GetServiceVersionString();
        /// <summary>
        /// Returns the version of the Windows OS.
        /// </summary>
        /// <returns>string representing the Windows OS version.</returns>
        string GetOSVersionString();
        /// <summary>
        /// Returns the version set by the OEM duing WSK Image Customization.
        /// </summary>
        /// <returns>string representing the OEM version.</returns>
        string GetOEMVersionString();
        /// <summary>
        /// Sets the path to TE.exe, used to run TAEF tests.
        /// </summary>
        /// <param name="teExePath">Path to TE.exe</param>
        /// <returns></returns>
        void SetTeExePath(string teExePath);
        /// <summary>
        /// Sets the log folder path used by Factory Orchestrator.
        /// </summary>
        /// <param name="path">Path to the desired folder.</param>
        /// <param name="moveExistingLogs">If true, existing logs are moved to the new location.</param>
        /// <returns></returns>
        void SetLogFolder(string path, bool moveExistingLogs);
        /// <summary>
        /// Gets the log folder path used by Factory Orchestrator.
        /// </summary>
        /// <returns>Path to the log folder.</returns>
        string GetLogFolder();
        /// <summary>
        /// Gets a list of IP addresses and the Network Adapter each IP address belongs to.
        /// </summary>
        /// <returns>A list of IP addresses and the Network Adapter each IP address belongs to.</returns>
        List<Tuple<string, string>> GetIpAddressesAndNicNames();
        /// <summary>
        /// Gets a list of IP addresses for the container. These IPs are internal, they cannot be accessed outside of the host.
        /// </summary>
        /// <returns>A list of IP addresses for the container.</returns>
        List<string> GetContainerIpAddresses();
        /// <summary>
        /// Checks if the service is executing boot tasks. While executing boot tasks, many commands cannot be run.
        /// </summary>
        /// <returns><c>true</c> is the service is executing boot tasks.</returns>
        bool IsExecutingBootTasks();

        /// <summary>
        /// Determines whether the connected device has a container present and running.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if container is present and running; otherwise, <c>false</c>.
        /// </returns>
        bool IsContainerRunning();

        /// <summary>
        /// Gets a list of Factory Orchestrator App pages that were disabled by OEM Customization.
        /// </summary>
        /// <returns>A list of page tags that should be disabled.</returns>
        List<string> GetDisabledPages();

        // TaskList APIs
        /// <summary>
        /// Creates a new TaskList by finding all .exe, .cmd, .bat, .ps1, and TAEF files in a given folder.
        /// </summary>
        /// <param name="path">Path of the directory to search.</param>
        /// <param name="recursive">If true, search recursively.</param>
        /// <returns>The created TaskList</returns>
        TaskList CreateTaskListFromDirectory(string path, bool recursive = false);
        /// <summary>
        /// Creates new TaskLists by loading them from a FactoryOrchestratorXML file.
        /// </summary>
        /// <param name="filename">The path to the FactoryOrchestratorXML file.</param>
        /// <returns>The GUID(s) of the created TaskList(s)</returns>
        List<Guid> LoadTaskListsFromXmlFile(string filename);
        /// <summary>
        /// Saves a TaskList to a FactoryOrchestratorXML file.
        /// </summary>
        /// <param name="guid">The GUID of the TaskList you wish to save.</param>
        /// <param name="filename">The path to the FactoryOrchestratorXML file that will be created.</param>
        /// <returns>true on success</returns>
        void SaveTaskListToXmlFile(Guid guid, string filename);
        /// <summary>
        /// Saves all TaskLists in the Service to a FactoryOrchestratorXML file.
        /// </summary>
        /// <param name="filename">The path to the FactoryOrchestratorXML file that will be created.</param>
        /// <returns>true on success</returns>
        void SaveAllTaskListsToXmlFile(string filename);
        /// <summary>
        /// Creates a TaskList on the Service by copying a TaskList object provided by the Client.
        /// </summary>
        /// <param name="list">The TaskList to add to the Service.</param>
        /// <returns>The created Service TaskList.</returns>
        TaskList CreateTaskListFromTaskList(TaskList list);
        /// <summary>
        /// Gets the GUID of every "active" TaskList on the Service. If "IsExecutingBootTasks()" returns true, this returns the "boot" TaskLists. Otherwise, it returns the "normal" TaskLists.
        /// </summary>
        /// <returns>The list of TaskList GUIDs.</returns>
        List<Guid> GetTaskListGuids();
        /// <summary>
        /// Gets TaskList summaries for every "active" TaskList on the Service.  If "IsExecutingBootTasks()" returns true, this returns the "boot" TaskLists. Otherwise, it returns the "normal" TaskLists. The summary contains basic info about the TaskList.
        /// </summary>
        /// <returns>A list of TaskListSummary objects.</returns>
        List<TaskListSummary> GetTaskListSummaries();
        /// <summary>
        /// Gets the GUID of every "boot" TaskList on the Service.
        /// </summary>
        /// <returns>The list of TaskList GUIDs.</returns>
        List<Guid> GetBootTaskListGuids();
        /// <summary>
        /// Gets "boot" TaskList summaries for every "boot" TaskList on the Service. The summary contains basic info about the TaskList.
        /// </summary>
        /// <returns>A list of TaskListSummary objects.</returns>
        List<TaskListSummary> GetBootTaskListSummaries();
        /// <summary>
        /// Gets the TaskList object for a given TaskList GUID.
        /// </summary>
        /// <param name="taskListGuid">The TaskList GUID.</param>
        /// <returns>The TaskList object with that GUID.</returns>
        TaskList QueryTaskList(Guid taskListGuid);
        /// <summary>
        /// Deletes a TaskList on the Service.
        /// </summary>
        /// <param name="listToDelete">The GUID of the TaskList to delete.</param>
        /// <returns>true if it was deleted successfully.</returns>
        void DeleteTaskList(Guid listToDelete);
        /// <summary>
        /// Updates an existing TaskList on the Service.
        /// </summary>
        /// <param name="taskList">The updated TaskList.</param>
        /// <returns>true if it was updated successfully.</returns>
        void UpdateTaskList(TaskList taskList);
        /// <summary>
        /// Reorders the TaskLists known to the Service.
        /// </summary>
        /// <param name="newOrder">An ordered list of GUIDs corresponding to the TaskList GUIDs known to the Service.</param>
        void ReorderTaskLists(List<Guid> newOrder);

        // Task APIs
        /// <summary>
        /// Returns the Task object for a given Task GUID.
        /// </summary>
        /// <param name="guid">The Task GUID.</param>
        /// <returns></returns>
        TaskBase QueryTask(Guid guid);

        /// <summary>
        /// Gets the AUMIDs of all installed apps on the OS. Requires Windows Device Portal.
        /// </summary>
        /// <returns>The list of app AUMIDs.</returns>
        List<string> GetInstalledApps();

        /// <summary>
        /// Installs an app package on the Service's computer. The app package must already be on the Service's computer. Requires Windows Device Portal.
        /// If the app package is not on the Service's computer already, use SendAndInstallApp() to copy and install it instead.
        /// </summary>
        /// <param name="appPackagePath">Path on the Service's computer to the app package (.appx, .appxbundle, .msix, .msixbundle).</param>
        /// <param name="dependentPackages">List of paths on the Service's computer to the app's dependent packages.</param>
        /// <param name="certificateFile">Path on the Service's computer to the app's certificate file, if needed. Microsoft Store signed apps do not need a certificate.</param>
        void InstallApp(string appPackagePath, List<string> dependentPackages = null, string certificateFile = null);

        /// <summary>
        /// Enables local loopback on the given UWP app.
        /// </summary>
        /// <param name="aumid">The Application User Model ID (AUMID) of the app to enable local loopback on.</param>
        void EnableLocalLoopbackForApp(string aumid);

        // Task Execution APIs
        /// <summary>
        /// Executes all TaskLists in order.
        /// </summary>
        /// <returns><c>true</c> if the TaskLists are successfully queued to run.</returns>
        bool RunAllTaskLists();

        /// <summary>
        /// Executes a TaskList.
        /// </summary>
        /// <param name="taskListGuid">GUID of the TaskList to run.</param>
        /// <param name="initialTask">Index of the Task to start the run from.</param>
        /// <returns></returns>
        void RunTaskList(Guid taskListGuid, int initialTask = 0);
        /// <summary>
        /// Stops all executing Tasks and/or TaskLists.
        /// </summary>
        void AbortAll();
        /// <summary>
        /// Stops executing a TaskList.
        /// </summary>
        /// <param name="taskListGuid">The GUID of the TaskList to stop.</param>
        void AbortTaskList(Guid taskListGuid);
        /// <summary>
        /// Stops executing a TaskRun.
        /// </summary>
        /// <param name="taskRunGuid">The GUID of the TaskRun to stop.</param>
        void AbortTaskRun(Guid taskRunGuid);
        /// <summary>
        /// Runs an executable (.exe) outside of a Task/TaskList.
        /// </summary>
        /// <param name="exeFilePath">Full path to the .exe file</param>
        /// <param name="arguments">Arguments to pass to the .exe</param>
        /// <param name="logFilePath">Optional log file to save the console output to.</param>
        /// <param name="runInContainer">If true, run the executable in the container of the connected device.</param>
        /// <returns>The TaskRun associated with the .exe</returns>
        TaskRun RunExecutable(string exeFilePath, string arguments, string logFilePath = null, bool runInContainer = false);
        /// <summary>
        /// Runs a UWP app outside of a Task/TaskList. Requires Windows Device Portal.
        /// </summary>
        /// <param name="aumid">The Application User Model ID (AUMID) of the app to run.</param>
        /// <returns></returns>
        TaskRun RunApp(string aumid);
        /// <summary>
        /// Runs a Task outside of a TaskList.
        /// </summary>
        /// <param name="taskGuid">The GUID of the Task to run.</param>
        /// <returns>The TaskRun associated with the run.</returns>
        TaskRun RunTask(Guid taskGuid);
        /// <summary>
        /// Runs a Task outside of a TaskList.
        /// </summary>
        /// <param name="task">The Task to run.</param>
        /// <returns>The TaskRun associated with the run.</returns>
        TaskRun RunTask(TaskBase task);

        // TaskRun APIs
        /// <summary>
        /// Updates the status of a TaskRun.
        /// </summary>
        /// <param name="taskRun">The TaskRun to update.</param>
        /// <returns>true if it was updated.</returns>
        void UpdateTaskRun(TaskRun taskRun);
        /// <summary>
        /// Gets a TaskRun object.
        /// </summary>
        /// <param name="taskRunGuid">The GUID of the desired TaskRun</param>
        /// <returns>The TaskRun object.</returns>
        TaskRun QueryTaskRun(Guid taskRunGuid);

        // File APIs
        /// <summary>
        /// Gets all the data in a file on the Service's computer. It is recommended you use FactoryOrchestratorClient::GetFileFromDevice instead.
        /// </summary>
        /// <param name="sourceFilename">The path to the file to retrieve.</param>
        /// <param name="offset">If -1, read the whole file. Otherwise the starting byte to read the file from.</param>
        /// <param name="count">If offset is -1 this is ignored. Otherwise, the number of bytes to read from the file.</param>
        /// <param name="getFromContainer">If true, get the file from the container running on the connected device.</param>
        /// <returns>The bytes in the file.</returns>
        byte[] GetFile(string sourceFilename, long offset = -1, int count = 0, bool getFromContainer = false);
        /// <summary>
        /// Saves data to a file to the Service's computer. It is recommended you use FactoryOrchestratorClient::SendFileToDevice instead.
        /// </summary>
        /// <param name="targetFilename">The name of the file you want created on the Service's computer.</param>
        /// <param name="fileData">The bytes you want saved to that file.</param>
        /// <param name="appendFile">If true, the file is appended to instead of overwritten.</param>
        /// <param name="sendToContainer">If true, send the file to the container running on the connected device.</param>
        /// <returns>true if the file was sucessfully created.</returns>
        void SendFile(string targetFilename, byte[] fileData, bool appendFile = false, bool sendToContainer = false);
        /// <summary>
        /// Permanently deletes a file or folder. If a folder, all contents are deleted.
        /// </summary>
        /// <param name="path">File or folder to delete</param>
        /// <param name="deleteInContainer">If true, delete the file from the container running on the connected device.</param>
        void DeleteFileOrFolder(string path, bool deleteInContainer = false);
        /// <summary>
        /// Moves a file or folder to a new location.
        /// </summary>
        /// <param name="sourcePath">File or folder to move</param>
        /// <param name="destinationPath">Destination path</param>
        /// <param name="moveInContainer">If true, move the file from the container running on the connected device.</param>
        void MoveFileOrFolder(string sourcePath, string destinationPath, bool moveInContainer = false);
        /// <summary>
        /// Returns a list of all directories in a given folder.
        /// </summary>
        /// <param name="path">The folder to search.</param>
        /// <param name="recursive">If true, search recursively.</param>
        /// <returns></returns>
        List<string> EnumerateDirectories(string path, bool recursive = false);
        /// <summary>
        /// Returns a list of all files in a given folder.
        /// </summary>
        /// <param name="path">The folder to search.</param>
        /// <param name="recursive">If true, search recursively.</param>
        /// <returns></returns>
        List<string> EnumerateFiles(string path, bool recursive = false);

    } // IFactoryOrchestratorService. This line is parsed by AutoGenerateInterfaceHelper!
}
