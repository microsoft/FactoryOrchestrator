using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.FactoryOrchestrator.Core
{
    public enum ServiceEventType
    {
        NewTaskList = 0,
        UpdatedTaskList = 1,
        DeletedTaskList = 2,
        TaskListStarted = 3,
        TaskListFinished = 4,
        DoneWaitingForExternalTaskRun = 5,
        WaitingForExternalTaskRun = 6,
        ServiceReset = 7,
        ServiceError = 8,
        ServiceStarted = 9,
        ServiceStopped = 10,
        Unknown = 11
    }

    public class ServiceEvent
    {
        [JsonConstructor]
        public ServiceEvent()
        {
            _guidStr = "";
            _eventType = ServiceEventType.Unknown;
        }

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


        [JsonRequired]
        public ulong EventIndex { get => _eventIndex; }

        public DateTime EventTime { get => _eventTime; }

        public ServiceEventType ServiceEventType { get => _eventType; }

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
    /// IFTFCommunication defines the client <> server communication model. 
    /// </summary>
    public interface IFTFCommunication
    {
        // Service APIs
        void ResetService(bool preserveLogs = true);
        List<ServiceEvent> GetAllServiceEvents();
        List<ServiceEvent> GetServiceEventsByTime(DateTime timeLastChecked);
        List<ServiceEvent> GetServiceEventsByIndex(ulong lastEventIndex);
        ServiceEvent GetLastServiceError();
        string GetServiceVersionString();
        bool SetDefaultTePath(string teExePath);
        bool SetDefaultLogFolder(string logFolder, bool moveExistingLogs);
        List<Tuple<string, string>> GetIpAddressesAndNicNames();

        // TaskList APIs
        TaskList CreateTaskListFromDirectory(string path, bool onlyTAEF);
        List<Guid> LoadTaskListsFromXmlFile(string filename);
        bool SaveTaskListToXmlFile(Guid guid, string filename);
        bool SaveAllTaskListsToXmlFile(string filename);
        TaskList CreateTaskListFromTaskList(TaskList list);
        List<Guid> GetTaskListGuids();
        TaskList QueryTaskList(Guid taskListGuid);
        bool DeleteTaskList(Guid listToDelete);
        bool UpdateTaskList(TaskList taskList);

        // Task APIs
        TaskBase QueryTask(Guid guid);

        List<string> GetInstalledApps();

        // Task Execution APIs
        bool RunTaskList(Guid taskListGuid);
        void AbortAllTaskLists();
        void AbortTaskList(Guid taskListGuid);
        void AbortTaskRun(Guid taskRunGuid);
        TaskRun RunExecutableAsBackgroundTask(string exeFilePath, string arguments, string logFilePath = null);
        TaskRun RunApp(string packageFamilyName);
        TaskRun RunTask(Guid taskGuid);

        // TaskRun APIs
        bool UpdateTaskRun(TaskRun taskRun);
        TaskRun QueryTaskRun(Guid taskRunGuid);

        // File APIs
        byte[] GetFile(string sourceFilename);
        bool SendFile(string targetFilename, byte[] fileData);
    }
}
