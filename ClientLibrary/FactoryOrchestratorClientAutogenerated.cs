//
// Autogenerated by building FactoryOrchestratorClientLibrary. DO NOT MODIFY, CHANGES WILL BE LOST.
//

using Microsoft.FactoryOrchestrator.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskStatus = Microsoft.FactoryOrchestrator.Core.TaskStatus;

namespace Microsoft.FactoryOrchestrator.Client
{
    public partial class FactoryOrchestratorClient
    {
        /// <summary>
        /// Stops all running Tasks and deletes all TaskLists.
        /// </summary>
        /// <param name="preserveLogs">If true, all logs are deleted.</param>
        /// <param name="factoryReset">If true, the service is restarted as if it is first boot.</param>
        public async Task ResetService(bool preserveLogs = false, bool factoryReset = false)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                 await _IpcClient.InvokeAsync(x => x.ResetService(preserveLogs , factoryReset ));
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Gets all Service events.
        /// </summary>
        /// <returns>List of all Service events.</returns>
        public async Task<List<ServiceEvent>> GetServiceEvents()
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                return await _IpcClient.InvokeAsync(x => x.GetServiceEvents());
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Get all Service events since given time.
        /// </summary>
        /// <param name="timeLastChecked"></param>
        /// <returns>List of Service events.</returns>
        public async Task<List<ServiceEvent>> GetServiceEvents(DateTime timeLastChecked)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                return await _IpcClient.InvokeAsync(x => x.GetServiceEvents(timeLastChecked));
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Get all Service events since given index.
        /// </summary>
        /// <param name="lastEventIndex"></param>
        /// <returns>List of Service events.</returns>
        public async Task<List<ServiceEvent>> GetServiceEvents(ulong lastEventIndex)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                return await _IpcClient.InvokeAsync(x => x.GetServiceEvents(lastEventIndex));
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Get last Service error.
        /// </summary>
        /// <returns></returns>
        public async Task<ServiceEvent> GetLastServiceError()
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                return await _IpcClient.InvokeAsync(x => x.GetLastServiceError());
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Returns the version of Factory Orchestrator Service.
        /// </summary>
        /// <returns>string representing the Service version.</returns>
        public async Task<string> GetServiceVersionString()
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                return await _IpcClient.InvokeAsync(x => x.GetServiceVersionString());
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Returns the version of the Windows OS.
        /// </summary>
        /// <returns>string representing the Windows OS version.</returns>
        public async Task<string> GetOSVersionString()
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                return await _IpcClient.InvokeAsync(x => x.GetOSVersionString());
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Returns the version set by the OEM duing WSK Image Customization.
        /// </summary>
        /// <returns>string representing the OEM version.</returns>
        public async Task<string> GetOEMVersionString()
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                return await _IpcClient.InvokeAsync(x => x.GetOEMVersionString());
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Sets the path to TE.exe, used to run TAEF tests.
        /// </summary>
        /// <param name="teExePath">Path to TE.exe</param>
        /// <returns></returns>
        public async Task SetTeExePath(string teExePath)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                 await _IpcClient.InvokeAsync(x => x.SetTeExePath(teExePath));
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Sets the log folder path used by Factory Orchestrator.
        /// </summary>
        /// <param name="path">Path to the desired folder.</param>
        /// <param name="moveExistingLogs">If true, existing logs are moved to the new location.</param>
        /// <returns></returns>
        public async Task SetLogFolder(string path, bool moveExistingLogs)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                 await _IpcClient.InvokeAsync(x => x.SetLogFolder(path, moveExistingLogs));
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Gets the log folder path used by Factory Orchestrator.
        /// </summary>
        /// <returns>Path to the log folder.</returns>
        public async Task<string> GetLogFolder()
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                return await _IpcClient.InvokeAsync(x => x.GetLogFolder());
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Gets a list of IP addresses and the Network Adapter each IP address belongs to.
        /// </summary>
        /// <returns>A list of IP addresses and the Network Adapter each IP address belongs to.</returns>
        public async Task<List<Tuple<string, string>>> GetIpAddressesAndNicNames()
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                return await _IpcClient.InvokeAsync(x => x.GetIpAddressesAndNicNames());
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Gets a list of Factory Orchestrator App pages that were disabled by OEM Customization.
        /// </summary>
        /// <returns>A list of page tags that should be disabled.</returns>
        public async Task<List<string>> GetDisabledPages()
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                return await _IpcClient.InvokeAsync(x => x.GetDisabledPages());
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Creates a new TaskList by finding all .exe, .cmd, .bat, .ps1, and TAEF files in a given folder.
        /// </summary>
        /// <param name="path">Path of the directory to search.</param>
        /// <param name="recursive">If true, search recursively.</param>
        /// <returns>The created TaskList</returns>
        public async Task<TaskList> CreateTaskListFromDirectory(string path, bool recursive = false)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                return await _IpcClient.InvokeAsync(x => x.CreateTaskListFromDirectory(path, recursive ));
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Creates new TaskLists by loading them from a FactoryOrchestratorXML file.
        /// </summary>
        /// <param name="filename">The path to the FactoryOrchestratorXML file.</param>
        /// <returns>The GUID(s) of the created TaskList(s)</returns>
        public async Task<List<Guid>> LoadTaskListsFromXmlFile(string filename)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                return await _IpcClient.InvokeAsync(x => x.LoadTaskListsFromXmlFile(filename));
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Saves a TaskList to a FactoryOrchestratorXML file.
        /// </summary>
        /// <param name="guid">The GUID of the TaskList you wish to save.</param>
        /// <param name="filename">The path to the FactoryOrchestratorXML file that will be created.</param>
        /// <returns>true on success</returns>
        public async Task SaveTaskListToXmlFile(Guid guid, string filename)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                 await _IpcClient.InvokeAsync(x => x.SaveTaskListToXmlFile(guid, filename));
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Saves all TaskLists in the Service to a FactoryOrchestratorXML file.
        /// </summary>
        /// <param name="filename">The path to the FactoryOrchestratorXML file that will be created.</param>
        /// <returns>true on success</returns>
        public async Task SaveAllTaskListsToXmlFile(string filename)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                 await _IpcClient.InvokeAsync(x => x.SaveAllTaskListsToXmlFile(filename));
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Creates a TaskList on the Service by copying a TaskList object provided by the Client.
        /// </summary>
        /// <param name="list">The TaskList to add to the Service.</param>
        /// <returns>The created Service TaskList.</returns>
        public async Task<TaskList> CreateTaskListFromTaskList(TaskList list)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                return await _IpcClient.InvokeAsync(x => x.CreateTaskListFromTaskList(list));
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Gets the GUID of every TaskList on the Service.
        /// </summary>
        /// <returns>The list of TaskList GUIDs.</returns>
        public async Task<List<Guid>> GetTaskListGuids()
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                return await _IpcClient.InvokeAsync(x => x.GetTaskListGuids());
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Gets TaskList summaries for every TaskList on the Service. The summary contains basic info about the TaskList.
        /// </summary>
        /// <returns>A list of TaskListSummary objects.</returns>
        public async Task<List<TaskListSummary>> GetTaskListSummaries()
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                return await _IpcClient.InvokeAsync(x => x.GetTaskListSummaries());
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Gets the TaskList object for a given TaskList GUID.
        /// </summary>
        /// <param name="taskListGuid">The TaskList GUID.</param>
        /// <returns>The TaskList object with that GUID.</returns>
        public async Task<TaskList> QueryTaskList(Guid taskListGuid)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                return await _IpcClient.InvokeAsync(x => x.QueryTaskList(taskListGuid));
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Deletes a TaskList on the Service.
        /// </summary>
        /// <param name="listToDelete">The GUID of the TaskList to delete.</param>
        /// <returns>true if it was deleted successfully.</returns>
        public async Task DeleteTaskList(Guid listToDelete)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                 await _IpcClient.InvokeAsync(x => x.DeleteTaskList(listToDelete));
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Updates an existing TaskList on the Service.
        /// </summary>
        /// <param name="taskList">The updated TaskList.</param>
        /// <returns>true if it was updated successfully.</returns>
        public async Task UpdateTaskList(TaskList taskList)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                 await _IpcClient.InvokeAsync(x => x.UpdateTaskList(taskList));
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Reorders the TaskLists known to the Service.
        /// </summary>
        /// <param name="newOrder">An ordered list of GUIDs corresponding to the TaskList GUIDs known to the Service.</param>
        public async Task ReorderTaskLists(List<Guid> newOrder)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                 await _IpcClient.InvokeAsync(x => x.ReorderTaskLists(newOrder));
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Returns the Task object for a given Task GUID.
        /// </summary>
        /// <param name="guid">The Task GUID.</param>
        /// <returns></returns>
        public async Task<TaskBase> QueryTask(Guid guid)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                return await _IpcClient.InvokeAsync(x => x.QueryTask(guid));
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Gets the AUMIDs of all installed apps on the OS.
        /// </summary>
        /// <returns>The list of app AUMIDs.</returns>
        public async Task<List<string>> GetInstalledApps()
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                return await _IpcClient.InvokeAsync(x => x.GetInstalledApps());
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Installs an app on the Service's computer.
        /// </summary>
        /// <param name="appPackagePath">Path on the Service's computer to the app package (.appx, .appxbundle, .msix, .msixbundle).</param>
        /// <param name="dependentPackages">List of paths on the Service's computer to the app's dependent packages.</param>
        /// <param name="certificateFile">Path on the Service's computer to the app's certificate file, if needed. Microsoft Store signed apps do not need a certificate.</param>
        public async Task InstallApp(string appPackagePath, List<string> dependentPackages = null, string certificateFile = null)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                 await _IpcClient.InvokeAsync(x => x.InstallApp(appPackagePath, dependentPackages , certificateFile ));
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Executes all TaskLists in order.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> RunAllTaskLists()
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                return await _IpcClient.InvokeAsync(x => x.RunAllTaskLists());
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Executes a TaskList.
        /// </summary>
        /// <param name="taskListGuid">GUID of the TaskList to run.</param>
        /// <param name="initialTask">Index of the Task to start the run from.</param>
        /// <returns></returns>
        public async Task RunTaskList(Guid taskListGuid, int initialTask = 0)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                 await _IpcClient.InvokeAsync(x => x.RunTaskList(taskListGuid, initialTask ));
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Stops all executing Tasks and/or TaskLists.
        /// </summary>
        public async Task AbortAll()
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                 await _IpcClient.InvokeAsync(x => x.AbortAll());
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Stops executing a TaskList.
        /// </summary>
        /// <param name="taskListGuid">The GUID of the TaskList to stop.</param>
        public async Task AbortTaskList(Guid taskListGuid)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                 await _IpcClient.InvokeAsync(x => x.AbortTaskList(taskListGuid));
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Stops executing a TaskRun.
        /// </summary>
        /// <param name="taskRunGuid">The GUID of the TaskRun to stop.</param>
        public async Task AbortTaskRun(Guid taskRunGuid)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                 await _IpcClient.InvokeAsync(x => x.AbortTaskRun(taskRunGuid));
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Runs an executable (.exe) outside of a Task/TaskList.
        /// </summary>
        /// <param name="exeFilePath">Full path to the .exe file</param>
        /// <param name="arguments">Arguments to pass to the .exe</param>
        /// <param name="logFilePath">Optional log file to save the console output to.</param>
        /// <returns>The TaskRun associated with the .exe</returns>
        public async Task<TaskRun> RunExecutable(string exeFilePath, string arguments, string logFilePath = null)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                return await _IpcClient.InvokeAsync(x => x.RunExecutable(exeFilePath, arguments, logFilePath ));
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Runs a UWP app outside of a Task/TaskList.
        /// </summary>
        /// <param name="aumid">The Application User Model ID (AUMID) of the app to run.</param>
        /// <returns></returns>
        public async Task<TaskRun> RunApp(string aumid)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                return await _IpcClient.InvokeAsync(x => x.RunApp(aumid));
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Runs a Task outside of a TaskList.
        /// </summary>
        /// <param name="taskGuid">The GUID of the Task to run.</param>
        /// <returns>The TaskRun associated with the run.</returns>
        public async Task<TaskRun> RunTask(Guid taskGuid)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                return await _IpcClient.InvokeAsync(x => x.RunTask(taskGuid));
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Runs a Task outside of a TaskList.
        /// </summary>
        /// <param name="task">The Task to run.</param>
        /// <returns>The TaskRun associated with the run.</returns>
        public async Task<TaskRun> RunTask(TaskBase task)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                return await _IpcClient.InvokeAsync(x => x.RunTask(task));
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Updates the status of a TaskRun.
        /// </summary>
        /// <param name="taskRun">The TaskRun to update.</param>
        /// <returns>true if it was updated.</returns>
        public async Task UpdateTaskRun(TaskRun taskRun)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                 await _IpcClient.InvokeAsync(x => x.UpdateTaskRun(taskRun));
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Gets a TaskRun object.
        /// </summary>
        /// <param name="taskRunGuid">The GUID of the desired TaskRun</param>
        /// <returns>The TaskRun object.</returns>
        public async Task<TaskRun> QueryTaskRun(Guid taskRunGuid)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                return await _IpcClient.InvokeAsync(x => x.QueryTaskRun(taskRunGuid));
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Gets all the data in a file on the Service's computer. It is recommended you use FactoryOrchestratorClient::GetFileFromServer instead.
        /// </summary>
        /// <param name="sourceFilename">The path to the file to retrieve.</param>
        /// <returns>The bytes in the file.</returns>
        public async Task<byte[]> GetFile(string sourceFilename)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                return await _IpcClient.InvokeAsync(x => x.GetFile(sourceFilename));
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Saves data to a file to the Service's computer. It is recommended you use FactoryOrchestratorClient::SendFileToServer instead.
        /// </summary>
        /// <param name="targetFilename">The name of the file you want created on the Service's computer.</param>
        /// <param name="fileData">The bytes you want saved to that file.</param>
        /// <returns>true if the file was sucessfully created.</returns>
        public async Task SendFile(string targetFilename, byte[] fileData)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                 await _IpcClient.InvokeAsync(x => x.SendFile(targetFilename, fileData));
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Permanently deletes a file or folder. If a folder, all contents are deleted.
        /// </summary>
        /// <param name="path">File or folder to delete</param>
        public async Task DeleteFileOrFolder(string path)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                 await _IpcClient.InvokeAsync(x => x.DeleteFileOrFolder(path));
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Moves a file or folder to a new location.
        /// </summary>
        /// <param name="sourcePath">File or folder to move</param>
        /// <param name="destinationPath">Destination path</param>
        public async Task MoveFileOrFolder(string sourcePath, string destinationPath)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                 await _IpcClient.InvokeAsync(x => x.MoveFileOrFolder(sourcePath, destinationPath));
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Returns a list of all directories in a given folder.
        /// </summary>
        /// <param name="path">The folder to search.</param>
        /// <param name="recursive">If true, search recursively.</param>
        /// <returns></returns>
        public async Task<List<string>> EnumerateDirectories(string path, bool recursive = false)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                return await _IpcClient.InvokeAsync(x => x.EnumerateDirectories(path, recursive ));
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Returns a list of all files in a given folder.
        /// </summary>
        /// <param name="path">The folder to search.</param>
        /// <param name="recursive">If true, search recursively.</param>
        /// <returns></returns>
        public async Task<List<string>> EnumerateFiles(string path, bool recursive = false)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                return await _IpcClient.InvokeAsync(x => x.EnumerateFiles(path, recursive ));
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }


    }
}
