using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.FactoryOrchestrator.Core
{
    /// <summary>
    /// The type of Factory Orchestrator Service event.
    /// </summary>
    public enum ServiceEventType
    {
        //NewTaskList = 0,
        //UpdatedTaskList = 1,
        //DeletedTaskList = 2,
        //TaskListStarted = 3,
        //TaskListFinished = 4,
        /// <summary>
        /// External TaskRun is completed.
        /// </summary>
        DoneWaitingForExternalTaskRun = 5,
        /// <summary>
        /// External TaskRun is waiting on an external action.
        /// </summary>
        WaitingForExternalTaskRun = 6,
        //ServiceReset = 7,
        /// <summary>
        /// The Factory Orchestrator Serivce threw an exception.
        /// </summary>
        ServiceError = 8,
        //ServiceStarted = 9,
        //ServiceStopped = 10,
        /// <summary>
        /// An unknown Factory Orchestrator Serivce event occurred.
        /// </summary>
        Unknown = 11
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
        private ulong _eventIndex;

        [JsonRequired]
        private ServiceEventType _eventType;

        [JsonRequired]
        private DateTime _eventTime;

        [JsonRequired]
        private string _message;

        [JsonRequired]
        private string _guidStr;

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
        /// Gets the GUID of every TaskList on the Service.
        /// </summary>
        /// <returns>The list of TaskList GUIDs.</returns>
        List<Guid> GetTaskListGuids();
        /// <summary>
        /// Gets TaskList summaries for every TaskList on the Service. The summary contains basic info about the TaskList.
        /// </summary>
        /// <returns>A list of TaskListSummary objects.</returns>
        List<TaskListSummary> GetTaskListSummaries();
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
        /// Gets the AUMIDs of all installed apps on the OS.
        /// </summary>
        /// <returns>The list of app AUMIDs.</returns>
        List<string> GetInstalledApps();

        /// <summary>
        /// Installs an app on the Service's computer.
        /// </summary>
        /// <param name="appPackagePath">Path on the Service's computer to the app package (.appx, .appxbundle, .msix, .msixbundle).</param>
        /// <param name="dependentPackages">List of paths on the Service's computer to the app's dependent packages.</param>
        /// <param name="certificateFile">Path on the Service's computer to the app's certificate file, if needed. Microsoft Store signed apps do not need a certificate.</param>
        void InstallApp(string appPackagePath, List<string> dependentPackages = null, string certificateFile = null);

        // Task Execution APIs
        /// <summary>
        /// Executes all TaskLists in order.
        /// </summary>
        /// <returns></returns>
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
        /// <returns>The TaskRun associated with the .exe</returns>
        TaskRun RunExecutable(string exeFilePath, string arguments, string logFilePath = null);
        /// <summary>
        /// Runs a UWP app outside of a Task/TaskList.
        /// </summary>
        /// <param name="packageFamilyName">The Package Family Name of the app to run.</param>
        /// <returns></returns>
        TaskRun RunApp(string packageFamilyName);
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
        /// Gets all the data in a file on the Service's computer. It is recommended you use FactoryOrchestratorClient::GetFileFromServer instead.
        /// </summary>
        /// <param name="sourceFilename">The path to the file to retrieve.</param>
        /// <returns>The bytes in the file.</returns>
        byte[] GetFile(string sourceFilename);
        /// <summary>
        /// Saves data to a file to the Service's computer. It is recommended you use FactoryOrchestratorClient::SendFileToServer instead.
        /// </summary>
        /// <param name="targetFilename">The name of the file you want created on the Service's computer.</param>
        /// <param name="fileData">The bytes you want saved to that file.</param>
        /// <returns>true if the file was sucessfully created.</returns>
        void SendFile(string targetFilename, byte[] fileData);
        /// <summary>
        /// Permanently deletes a file or folder. If a folder, all contents are deleted.
        /// </summary>
        /// <param name="path">File or folder to delete</param>
        void DeleteFileOrFolder(string path);
        /// <summary>
        /// Moves a file or folder to a new location.
        /// </summary>
        /// <param name="sourcePath">File or folder to move</param>
        /// <param name="destinationPath">Destination path</param>
        void MoveFileOrFolder(string sourcePath, string destinationPath);
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
