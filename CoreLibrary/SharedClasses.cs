using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.FactoryOrchestrator.Core.JSONConverters;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.FactoryOrchestrator.Core
{
    public enum TaskStatus
    {
        Passed,
        Failed,
        Aborted,
        Timeout,
        Running,
        NotRun,
        RunPending,
        WaitingForExternalResult,
        Unknown
    }

    public enum TaskType
    {
        ConsoleExe = 0,
        TAEFDll = 1,
        External = 2,
        UWP = 3,
        PowerShell = 4,
        BatchFile = 5
    }

    public class TaskBaseEqualityComparer : EqualityComparer<TaskBase>
    {
        public override bool Equals(TaskBase x, TaskBase y)
        {
            return x.Equals(y);
        }

        public override int GetHashCode(TaskBase obj)
        {
            return obj.GetHashCode();
        }
    }

    /// <summary>
    /// TaskBase is an abstract class representing a generic task. It contains all the details needed to run the task.
    /// It also surfaces information about the last TaskRun for this task, for easy consumption.
    /// </summary>
    [JsonConverter(typeof(TaskBaseConverter))]
    [XmlInclude(typeof(ExecutableTask))]
    [XmlInclude(typeof(UWPTask))]
    [XmlInclude(typeof(ExternalTask))]
    [XmlInclude(typeof(TAEFTest))]
    [XmlInclude(typeof(PowerShellTask))]
    [XmlInclude(typeof(BatchFileTask))]
    public abstract class TaskBase
    {
        // TODO: Quality: Use Semaphore internally to guarantee accurate state if many things are setting task state
        // lock on modification & lock on query so that internal state is guaranteed to be consistent at all times
        protected TaskBase(TaskType type)
        {
            Type = type;
            TaskLock = new object();
            LatestTaskRunStatus = TaskStatus.NotRun;
            LatestTaskRunExitCode = null;
            LatestTaskRunTimeFinished = null;
            LatestTaskRunTimeStarted = null;
            TaskRunGuids = new List<Guid>();
            TimeoutSeconds = -1;
            Arguments = "";
            MaxNumberOfRetries = 0;
            TimesRetried = 0;
            AbortTaskListOnFailed = false;
        }

        public TaskBase(string taskPath, TaskType type) : this(type)
        {
            Guid = Guid.NewGuid();
            Path = taskPath;
        }

        // TODO: Make only getters and add internal apis to set
        [XmlAttribute("Name")]
        public virtual string Name
        {
            get
            {
                return Path;
            }
            set { }
        }

        [XmlIgnore]
        public TaskType Type { get; set; }
        [XmlAttribute("Path")]
        public string Path { get; set; }
        [XmlAttribute]
        public string Arguments { get; set; }
        [XmlAttribute]
        public Guid Guid { get; set; }
        public DateTime? LatestTaskRunTimeStarted { get; set; }
        public DateTime? LatestTaskRunTimeFinished { get; set; }
        public TaskStatus LatestTaskRunStatus { get; set; }
        public bool? LatestTaskRunPassed
        {
            get
            {
                if (LatestTaskRunStatus == TaskStatus.Passed)
                {
                    return true;
                }
                else if ((LatestTaskRunStatus == TaskStatus.Failed) || (LatestTaskRunStatus == TaskStatus.Timeout))
                {
                    return false;
                }
                else
                {
                    return null;
                }
            }
        }

        [XmlAttribute("Timeout")]
        public int TimeoutSeconds { get; set; }

        public int? LatestTaskRunExitCode { get; set; }

        // TaskRuns are queried by GUID
        [XmlArrayItem("Guid")]
        public List<Guid> TaskRunGuids { get; set; }

        public virtual TimeSpan? LatestTaskRunRunTime
        {
            get
            {
                if (LatestTaskRunTimeStarted != null)
                {
                    if (LatestTaskRunTimeFinished != null)
                    {
                        return LatestTaskRunTimeFinished - LatestTaskRunTimeStarted;
                    }
                    else
                    {
                        return DateTime.Now - LatestTaskRunTimeStarted;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        public bool RunByServer
        {
            get
            {
                return ((Type != TaskType.External) && (Type != TaskType.UWP));
            }
        }

        public bool RunByClient
        {
            get
            {
                return !RunByServer;
            }
        }

        public Guid? LatestTaskRunGuid
        {
            get
            {
                if ((TaskRunGuids != null) && (TaskRunGuids.Count >= 1))
                {
                    return TaskRunGuids.Last();
                }
                else
                {
                    return null;
                }
            }
        }

        [XmlAttribute]
        public bool AbortTaskListOnFailed { get; set; }

        [XmlAttribute]
        public uint MaxNumberOfRetries { get; set; }

        public uint TimesRetried { get; set; }

        // XmlSerializer calls these to check if these values are set.
        // If not set, don't serialize.
        // https://docs.microsoft.com/en-us/dotnet/framework/winforms/controls/defining-default-values-with-the-shouldserialize-and-reset-methods
        public bool ShouldSerializeLatestTaskRunTimeStarted()
        {
            return LatestTaskRunTimeStarted.HasValue;
        }

        public bool ShouldSerializeLatestTaskRunTimeFinished()
        {
            return LatestTaskRunTimeFinished.HasValue;
        }

        public bool ShouldSerializeLatestTaskRunExitCode()
        {
            return LatestTaskRunExitCode.HasValue;
        }
        public bool ShouldSerializeTaskRunGuids()
        {
            return TaskRunGuids.Count > 0;
        }
        public bool ShouldSerializeTimeoutSeconds()
        {
            return TimeoutSeconds != -1;
        }
        public bool ShouldSerializeTimesRetried()
        {
            return TimesRetried != 0;
        }
        public bool ShouldSerializeMaxNumberOfRetries()
        {
            return MaxNumberOfRetries != 0;
        }
        public bool ShouldSerializeAbortTaskListOnFailed()
        {
            return AbortTaskListOnFailed == true;
        }
        public override bool Equals(object obj)
        {
            var rhs = obj as TaskBase;

            if (rhs == null)
            {
                return false;
            }

            if (this.Guid != rhs.Guid)
            {
                return false;
            }

            if (this.Arguments != rhs.Arguments)
            {
                return false;
            }

            if (this.LatestTaskRunExitCode != rhs.LatestTaskRunExitCode)
            {
                return false;
            }

            if (this.LatestTaskRunStatus != rhs.LatestTaskRunStatus)
            {
                return false;
            }

            if (this.LatestTaskRunGuid != rhs.LatestTaskRunGuid)
            {
                return false;
            }

            if (this.LatestTaskRunTimeFinished != rhs.LatestTaskRunTimeFinished)
            {
                return false;
            }

            if (this.LatestTaskRunTimeStarted != rhs.LatestTaskRunTimeStarted)
            {
                return false;
            }

            if (this.Type != rhs.Type)
            {
                return false;
            }

            if (!this.TaskRunGuids.SequenceEqual(rhs.TaskRunGuids))
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return -737073652 + EqualityComparer<Guid>.Default.GetHashCode(Guid);
        }

        // TODO: Quality: Consider using IXmlSerializable so we can make some properties "read only"
        //public XmlSchema GetSchema()
        //{
        //    return null;
        //}

        //public void ReadXml(XmlReader reader)
        //{
        //    reader.MoveToContent();
        //    Guid = reader.GetAttribute("Guid")
        //}

        //public void WriteXml(XmlWriter writer)
        //{

        //}

        [JsonIgnore]
        [XmlIgnore]
        public object TaskLock;
    }

    /// <summary>
    /// An ExecutableTask is an .exe binary that is run by the FactoryOrchestratorServer. The exit code of the process determines if the task passed or failed.
    /// 0 == PASS, all others == FAIL.
    /// </summary>
    [JsonConverter(typeof(NoConverter))]
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public class ExecutableTask : TaskBase
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    {
        private ExecutableTask() : base(TaskType.ConsoleExe)
        {
            BackgroundTask = false;
        }

        public ExecutableTask(String taskPath) : base(taskPath, TaskType.ConsoleExe)
        {
            BackgroundTask = false;
        }

        protected ExecutableTask(String taskPath, TaskType type) : base(taskPath, type)
        {
            BackgroundTask = false;
        }

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            var rhs = obj as ExecutableTask;

            if (rhs == null)
            {
                return false;
            }

            return base.Equals(obj as TaskBase);
        }

        [XmlAttribute("Name")]
        public override string Name
        {
            get
            {
                if (_testFriendlyName == null)
                {
                    return System.IO.Path.GetFileName(Path);
                }
                else
                {
                    return _testFriendlyName;
                }
            }
            set
            {
                _testFriendlyName = value;
            }
        }

        [XmlIgnore]
        public bool BackgroundTask { get; set; }

        private string _testFriendlyName;
    }

    [JsonConverter(typeof(NoConverter))]
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public class PowerShellTask : ExecutableTask
    {
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()

        private PowerShellTask() : base(null, TaskType.PowerShell)
        {

        }

        public PowerShellTask(string scriptPath) : base(scriptPath, TaskType.PowerShell)
        {
            _scriptPath = scriptPath;
        }

        [XmlAttribute("Name")]
        public override string Name
        {
            get
            {
                if (_testFriendlyName != null)
                {
                    return _testFriendlyName;
                }
                else
                {
                    return System.IO.Path.GetFileName(_scriptPath);
                }
            }
            set
            {
                _testFriendlyName = value;
            }
        }

        public override bool Equals(object obj)
        {
            var rhs = obj as TaskBase;

            if (rhs == null)
            {
                return false;
            }

            return base.Equals(obj as ExecutableTask);
        }

        private string _scriptPath;
        private string _testFriendlyName;
    }

    [JsonConverter(typeof(NoConverter))]
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public class BatchFileTask : ExecutableTask
    {
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()

        private BatchFileTask() : base(null, TaskType.BatchFile)
        {

        }

        public BatchFileTask(string scriptPath) : base(scriptPath, TaskType.BatchFile)
        {
            _scriptPath = scriptPath;
        }

        [XmlAttribute("Name")]
        public override string Name
        {
            get
            {
                if (_testFriendlyName != null)
                {
                    return _testFriendlyName;
                }
                else
                {
                    return System.IO.Path.GetFileName(_scriptPath);
                }
            }
            set
            {
                _testFriendlyName = value;
            }
        }

        public override bool Equals(object obj)
        {
            var rhs = obj as TaskBase;

            if (rhs == null)
            {
                return false;
            }

            return base.Equals(obj as ExecutableTask);
        }

        private string _scriptPath;
        private string _testFriendlyName;
    }

    /// <summary>
    /// A TAEFTest is a type of ExecutableTask, which is always run by TE.exe. TAEF tests are comprised of one or more sub-tests (TAEFTestCase).
    /// Pass/Fail is determined by TE.exe.
    /// </summary>
    [JsonConverter(typeof(NoConverter))]
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public class TAEFTest : ExecutableTask
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    {
        private TAEFTest() : base(null, TaskType.TAEFDll)
        {

        }

        public TAEFTest(string testPath) : base(testPath, TaskType.TAEFDll)
        {
        }

        public override bool Equals(object obj)
        {
            var rhs = obj as TaskBase;

            if (rhs == null)
            {
                return false;
            }

            return base.Equals(obj as ExecutableTask);
        }
    }

    /// <summary>
    /// An ExternalTest is a task run outside of the FactoryOrchestratorServer.
    /// task results must be returned to the server via SetTaskRunStatus().
    /// </summary>
    [JsonConverter(typeof(NoConverter))]
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public class ExternalTask : TaskBase
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    {
        private ExternalTask() : base(TaskType.External)
        {
        }

        public ExternalTask(String testName) : base(null, TaskType.External)
        {
            _testFriendlyName = testName;
        }

        protected ExternalTask(String taskPath, String testName, TaskType type) : base(taskPath, type)
        {
            _testFriendlyName = testName;
        }

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            var rhs = obj as ExternalTask;

            if (rhs == null)
            {
                return false;
            }

            if (rhs.Name != Name)
            {
                return false;
            }

            return base.Equals(obj as TaskBase);
        }

        [XmlAttribute("Name")]
        public override string Name
        {
            get
            {
                if (_testFriendlyName == null)
                {
                    return Path;
                }
                else
                {
                    return _testFriendlyName;
                }
            }
            set
            {
                _testFriendlyName = value;
            }
        }

        private string _testFriendlyName;
    }

    /// <summary>
    /// A UWPTest is a UWP task run by the FactoryOrchestratorApp client. These are used for UI.
    /// task results must be returned to the server via SetTaskRunStatus().
    /// </summary>
    [JsonConverter(typeof(NoConverter))]
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public class UWPTask : ExternalTask
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    {
        private UWPTask() : base(null, null, TaskType.UWP)
        {
        }

        public UWPTask(string packageFamilyName, string testFriendlyName) : base(packageFamilyName, testFriendlyName, TaskType.UWP)
        {
            _testFriendlyName = testFriendlyName;
        }

        public UWPTask(string packageFamilyName) : base(packageFamilyName, null, TaskType.UWP)
        {
            _testFriendlyName = packageFamilyName;
        }

        public override bool Equals(object obj)
        {
            var rhs = obj as UWPTask;

            if (rhs == null)
            {
                return false;
            }

            if (this.Name != this.Name)
            {
                return false;
            }

            return base.Equals(obj as ExternalTask);
        }

        [XmlAttribute("Name")]
        public override string Name
        {
            get
            {
                if (_testFriendlyName == null)
                {
                    return Path;
                }
                else
                {
                    return _testFriendlyName;
                }
            }
            set
            {
                _testFriendlyName = value;
            }
        }

        private string _testFriendlyName;
    }

    /// <summary>
    /// A TaskList is a grouping of FTF tests. TaskLists are the only object FTF can "Run".
    /// </summary>
    public class TaskList
    {
        [JsonConstructor]
        internal TaskList()
        {
            Tasks = new Dictionary<Guid, TaskBase>();
            TasksForXml = new List<TaskBase>();
            BackgroundTasks = new Dictionary<Guid, TaskBase>();
            BackgroundTasksForXml = new List<TaskBase>();
            RunInParallel = false;
            AllowOtherTaskListsToRun = false;
            TerminateBackgroundTasksOnCompletion = true;
            Name = "";
        }

        public TaskList(string name, Guid guid) : this()
        {
            if (guid != null)
            {
                Guid = guid;
            }
            else
            {
                Guid = Guid.NewGuid();
            }

            Name = name;
        }

        public TaskStatus TaskListStatus
        {
            get
            {
                if (Tasks.Values.All(x => x.LatestTaskRunPassed == true))
                {
                    return TaskStatus.Passed;
                }
                else if (Tasks.Values.All(x => x.LatestTaskRunStatus == TaskStatus.RunPending))
                {
                    return TaskStatus.RunPending;
                }
                else if (Tasks.Values.Any(x => x.LatestTaskRunStatus == TaskStatus.Aborted))
                {
                    return TaskStatus.Aborted;
                }
                else if (Tasks.Values.Any(x => (x.LatestTaskRunStatus == TaskStatus.Running) || (x.LatestTaskRunStatus == TaskStatus.RunPending) || (x.LatestTaskRunStatus == TaskStatus.WaitingForExternalResult)))
                {
                    return TaskStatus.Running;
                }
                else if (Tasks.Values.Any(x => x.LatestTaskRunStatus == TaskStatus.Unknown))
                {
                    return TaskStatus.Unknown;
                }
                else if (Tasks.Values.Any(x => (x.LatestTaskRunPassed != null) && ((bool)x.LatestTaskRunPassed == false)))
                {
                    return TaskStatus.Failed;
                }
                else
                {
                    return TaskStatus.NotRun;
                }
            }
        }

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            var rhs = obj as TaskList;

            if (rhs == null)
            {
                return false;
            }

            if (this.Guid != rhs.Guid)
            {
                return false;
            }

            //if (!this.Tests.Keys.Equals(rhs.Tests.Keys))
            //{
            //    return false;
            //}

            //if (this.Tests.Values != rhs.Tests.Values)
            //{
            //    return false;
            //}

            if (!this.Tasks.SequenceEqual(rhs.Tasks))
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return -2045414129 + EqualityComparer<Guid>.Default.GetHashCode(Guid);
        }

        public bool ShouldSerializeTerminateBackgroundTasksOnCompletion()
        {
            return BackgroundTasks.Count > 0;
        }

        public bool ShouldSerializeBackgroundTasksForXml()
        {
            return BackgroundTasks.Count > 0;
        }

        /// <summary>
        /// XML serializer can't serialize Dictionaries. Use a list instead for XML.
        /// </summary>
        [XmlArrayItem("Task")]
        [XmlArray("Tasks")]
        [JsonIgnore]
        public List<TaskBase> TasksForXml { get; set; }


        /// <summary>
        /// XML serializer can't serialize Dictionaries. Use a list instead for XML.
        /// </summary>
        [XmlArrayItem("Task")]
        [XmlArray("BackgroundTasks")]
        [JsonIgnore]
        public List<TaskBase> BackgroundTasksForXml { get; set; }

        /// <summary>
        /// Tests in the TaskList, tracked by task GUID
        /// </summary>
        [XmlIgnore]
        public Dictionary<Guid, TaskBase> Tasks { get; set; }

        /// <summary>
        /// Background Tests in the TaskList, tracked by task GUID
        /// </summary>
        [XmlIgnore]
        public Dictionary<Guid, TaskBase> BackgroundTasks { get; set; }

        [XmlAttribute]
        public Guid Guid { get; set; }

        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public bool RunInParallel { get; set; }

        [XmlAttribute]
        public bool AllowOtherTaskListsToRun { get; set; }

        [XmlAttribute]
        public bool TerminateBackgroundTasksOnCompletion { get; set; }
    }

    /// <summary>
    /// Shared client and server TaskRun class. A TaskRun represents one instance of executing any single FTF task.
    /// TaskRuns should only be created by the server, hence no public CTOR.
    /// </summary>
    public class TaskRun
    {
        // TODO: Quality: Use Semaphore internally to guarantee accurate state if many things are setting task state
        // lock on modification & lock on query so that internal state is guaranteed to be consistent at all times
        [JsonConstructor]
        protected TaskRun()
        {

        }

        /// <summary>
        /// task Run shared constructor. 
        /// </summary>
        /// <param name="owningTask"></param>
        protected TaskRun(TaskBase owningTask)
        {
            Guid = Guid.NewGuid();
            OwningTaskGuid = null;
            LogFilePath = null;
            TaskStatus = TaskStatus.NotRun;
            TimeFinished = null;
            TimeStarted = null;
            ExitCode = null;
            TaskOutput = new List<string>();
            TimeoutSeconds = -1;
            BackgroundTask = false;

            if (owningTask != null)
            {
                OwningTaskGuid = owningTask.Guid;
                TaskPath = owningTask.Path;
                Arguments = owningTask.Arguments;
                TaskName = owningTask.Name;
                TaskType = owningTask.Type;
                TimeoutSeconds = owningTask.TimeoutSeconds;
                if (TaskType == TaskType.ConsoleExe)
                {
                    BackgroundTask = ((ExecutableTask)owningTask).BackgroundTask;
                }
            }
        }

        public TaskRun DeepCopy()
        {
            TaskRun copy = (TaskRun)this.MemberwiseClone();
            copy.TaskOutput = new List<string>(this.TaskOutput.Count);
            copy.TaskOutput.AddRange(this.TaskOutput.GetRange(0, copy.TaskOutput.Capacity));

            var stringProps = typeof(TaskRun).GetProperties().Where(x => x.PropertyType == typeof(string));
            foreach (var prop in stringProps)
            {
                var value = prop.GetValue(this);
                if (value != null)
                {
                    var copyStr = String.Copy(value as string);
                    prop.SetValue(copy, copyStr);
                }
                else
                {
                    prop.SetValue(copy, null);
                }
            }

            return copy;
        }

        public List<string> TaskOutput { get; set; }

        public Guid? OwningTaskGuid { get; set; }
        public string TaskName { get; set; }
        public string TaskPath { get; set; }
        public string Arguments { get; set; }
        public bool BackgroundTask { get; set; }
        public TaskType TaskType { get; set; }
        public Guid Guid { get; set; }
        public DateTime? TimeStarted { get; set; }
        public DateTime? TimeFinished { get; set; }
        public TaskStatus TaskStatus { get; set; }
        public string LogFilePath { get; set; }
        public int? ExitCode { get; set; }
        public int TimeoutSeconds { get; set; }

        public bool RunByServer
        {
            get
            {
                return ((TaskType != TaskType.External) && (TaskType != TaskType.UWP));
            }
        }

        public bool RunByClient
        {
            get
            {
                return !RunByServer;
            }
        }

        public bool TaskRunComplete
        {
            get
            {
                switch (TaskStatus)
                {
                    case TaskStatus.Aborted:
                    case TaskStatus.Failed:
                    case TaskStatus.Passed:
                    case TaskStatus.Timeout:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public virtual TimeSpan? RunTime
        {
            get
            {
                if (TimeStarted != null)
                {
                    if (TimeFinished != null)
                    {
                        return TimeFinished - TimeStarted;
                    }
                    else
                    {
                        return DateTime.Now - TimeStarted;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        public override bool Equals(object obj)
        {
            var rhs = obj as TaskRun;

            if (rhs == null)
            {
                return false;
            }

            if (this.Guid != rhs.Guid)
            {
                return false;
            }

            if (this.OwningTaskGuid != rhs.OwningTaskGuid)
            {
                return false;
            }

            if (this.TaskType != rhs.TaskType)
            {
                return false;
            }

            if (this.TaskName != rhs.TaskName)
            {
                return false;
            }

            if (this.Arguments != rhs.Arguments)
            {
                return false;
            }

            if (this.ExitCode != rhs.ExitCode)
            {
                return false;
            }

            if (this.TaskStatus != rhs.TaskStatus)
            {
                return false;
            }

            if (this.TimeFinished != rhs.TimeFinished)
            {
                return false;
            }

            if (this.TimeStarted != rhs.TimeStarted)
            {
                return false;
            }

            if (this.LogFilePath != rhs.LogFilePath)
            {
                return false;
            }

            if (!this.TaskOutput.SequenceEqual(rhs.TaskOutput))
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return -737073652 + EqualityComparer<Guid>.Default.GetHashCode(Guid);
        }
    }

    /// <summary>
    /// This class is used to save & load TaskLists from an XML file.
    /// </summary>
    [XmlRootAttribute(ElementName = "FactoryOrchestratorXML", IsNullable = false)]
    public partial class FactoryOrchestratorXML
    {
        public FactoryOrchestratorXML()
        {
            TaskLists = new List<TaskList>();
        }

        [XmlArrayItem("TaskList")]
        public List<TaskList> TaskLists { get; set; }

        /// <summary>
        /// Create Guids for any imported task or tasklist that is missing one.
        /// Create Tests dictionary.
        /// </summary>
        private void PostDeserialize()
        {
            foreach (var list in TaskLists)
            {
                if (list.Guid == Guid.Empty)
                {
                    list.Guid = Guid.NewGuid();
                }

                foreach (var task in list.TasksForXml)
                {
                    if (task.Guid == Guid.Empty)
                    {
                        task.Guid = Guid.NewGuid();
                    }

                    list.Tasks.Add(task.Guid, task);
                }

                foreach (var bgtask in list.BackgroundTasksForXml)
                {
                    // Validate background tasks meet requirements
                    if ((bgtask.Type != TaskType.ConsoleExe) && (bgtask.Type != TaskType.PowerShell) && (bgtask.Type != TaskType.BatchFile))
                    {
                        throw new XmlSchemaValidationException("BackgroundTasks must be ExecutableTask, PowerShellTask, or BatchFileTask!");
                    }

                    if (bgtask.TimeoutSeconds != -1)
                    {
                        throw new XmlSchemaValidationException("BackgroundTasks cannot have a timeout value!");
                    }

                    if (bgtask.MaxNumberOfRetries != 0)
                    {
                        throw new XmlSchemaValidationException("BackgroundTasks cannot have a retry value!");
                    }

                    if (bgtask.Guid == Guid.Empty)
                    {
                        bgtask.Guid = Guid.NewGuid();
                    }

                    (bgtask as ExecutableTask).BackgroundTask = true;
                    list.BackgroundTasks.Add(bgtask.Guid, bgtask);
                }

                // clear xml lists
                list.TasksForXml = new List<TaskBase>();
                list.BackgroundTasksForXml = new List<TaskBase>();
            }
        }


        /// <summary>
        /// Create TestsForXml List.
        /// </summary>
        private void PreSerialize()
        {
            foreach (var list in TaskLists)
            {
                list.TasksForXml = new List<TaskBase>();
                list.TasksForXml.AddRange(list.Tasks.Values);
                list.BackgroundTasksForXml = new List<TaskBase>();
                list.BackgroundTasksForXml.AddRange(list.BackgroundTasks.Values);
            }
        }

        public static FactoryOrchestratorXML Load(string filename)
        {
            FactoryOrchestratorXML xml;

            try
            {
                lock (XmlSerializeLock)
                {
                    XmlIsValid = false;
                    ValidationErrors = "";

                    if (!File.Exists(filename))
                    {
                        throw new FileNotFoundException($"{filename} does not exist!");
                    }

                    // Validate XSD
                    var asm = Assembly.GetAssembly(typeof(FactoryOrchestratorXML));
                    using (Stream xsdStream = asm.GetManifestResourceStream(GetResourceName(Assembly.GetAssembly(typeof(FactoryOrchestratorXML)), "FactoryOrchestratorXML.xsd", false)))
                    {
                        XmlReaderSettings settings = new XmlReaderSettings();
                        settings.XmlResolver = null;

                        using (XmlReader xsdReader = XmlReader.Create(xsdStream, settings))
                        {
                            XmlSchema xmlSchema = xmlSchema = XmlSchema.Read(xsdReader, ValidationEventHandler);

                            using (XmlReader reader = XmlReader.Create(filename, settings))
                            {
                                XmlDocument document = new XmlDocument();
                                document.XmlResolver = null;
                                document.Schemas.Add(xmlSchema);

                                // Remove xsi:type so they are properly validated against the shared "task" XSD type
                                document.Load(reader);
                                var tasks = document.SelectNodes("//Task");
                                foreach (var taskNode in tasks)
                                {
                                    var removed = ((XmlNode)taskNode).Attributes.RemoveNamedItem("xsi:type");
                                }
                                XmlIsValid = true;
                                document.Validate(ValidationEventHandler);
                            }

                            if (!XmlIsValid)
                            {
                                // Throw all the errors we found
                                throw new XmlSchemaValidationException(ValidationErrors);
                            }
                        }
                    }
                }

                // Deserialize
                using (XmlReader reader = XmlReader.Create(filename))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(FactoryOrchestratorXML));
                    xml = (FactoryOrchestratorXML)serializer.Deserialize(reader);
                }

                xml.PostDeserialize();
            }
            catch (Exception e)
            {
                throw new FileLoadException($"Could not load {filename} as FactoryOrchestratorXML!", e);
            }

            return xml;
        }

        private static void ValidationEventHandler(object sender, ValidationEventArgs e)
        {
            if (e.Severity == XmlSeverityType.Error)
            {
                // Save the error, instead of throwing now.
                // This allows valiatation to catch mutiple errors in one pass.
                XmlIsValid = false;

                if (!String.IsNullOrEmpty(ValidationErrors))
                {
                    ValidationErrors += System.Environment.NewLine;
                }

                ValidationErrors += e.Message;
            }
        }

        private static string GetResourceName(Assembly assembly, string resourceIdentifier, bool matchWholeWord = true)
        {
            resourceIdentifier = resourceIdentifier.ToLowerInvariant();
            var ListOfResources = assembly.GetManifestResourceNames();

            foreach (string resource in ListOfResources)
            {
                if (matchWholeWord)
                {
                    if (resource.ToLowerInvariant().Equals(resourceIdentifier))
                    {
                        return resource;
                    }
                }
                else if (resource.ToLowerInvariant().Contains(resourceIdentifier))
                {
                    return resource;
                }
            }

            throw new FileNotFoundException("Could not find embedded resource", resourceIdentifier);
        }

        public bool Save(string filename)
        {
            PreSerialize();

            var xmlWriterSettings = new XmlWriterSettings() { Indent = true };
            using (XmlWriter writer = XmlWriter.Create(filename, xmlWriterSettings))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(FactoryOrchestratorXML));
                serializer.Serialize(writer, this);
            }

            return true;
        }

        private static bool XmlIsValid { get; set; }
        private static object XmlSerializeLock = new object();
        private static string ValidationErrors;
    }

    /// <summary>
    /// A helper class containing the bare minimum information about a TaskList. Use to quickly update clients about TaskLists and their statuses.
    /// </summary>
    public class TaskListSummary
    {
        public TaskListSummary(Guid guid, string name, TaskStatus status)
        {
            Guid = guid;
            Status = status;
            Name = name;
        }

        public override string ToString()
        {
            // Accessible name.
            return $"Task List {Name} ({Guid}) with Status {Status}";
        }

        public Guid Guid { get; set; }
        public TaskStatus Status { get; set; }
        public string Name { get; set; }
    }
}