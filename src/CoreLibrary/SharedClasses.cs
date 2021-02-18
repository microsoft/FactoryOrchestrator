// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Collections;
using System.Runtime.InteropServices;

namespace Microsoft.FactoryOrchestrator.Core
{
    /// <summary>
    /// Extends the Exception class.
    /// </summary>
    /// <exclude/>
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Returns a string describing the given Exception including all inner exceptions.
        /// </summary>
        /// <param name="ex">The Exception.</param>
        /// <returns>A string describing the given Exception including all inner exceptions.</returns>
        public static string AllExceptionsToString(this Exception ex)
        {
            string ret = "";
            var exc = ex;
            while (exc != null)
            {
                if (ret.Length > 0)
                {
                    ret += " -> ";
                }
                if (exc.Message != null)
                {
                    ret += $"{exc.GetType().ToString()}:{exc.Message}";
                }
                else
                {
                    ret += $"{exc.GetType().ToString()}";
                }

                exc = exc.InnerException;
            }

            return ret;
        }
    }

    /// <summary>
    /// The status of a Task, TaskRun, or TaskList.
    /// </summary>
    public enum TaskStatus
    {
        /// <summary>
        /// The Task passed with no errors.
        /// </summary>
        Passed,
        /// <summary>
        /// The Task failed.
        /// </summary>
        Failed,
        /// <summary>
        /// The Task was cancelled.
        /// </summary>
        Aborted,
        /// <summary>
        /// The Task hit its timeout and was cancelled.
        /// </summary>
        Timeout,
        /// <summary>
        /// The Task is actively running.
        /// </summary>
        Running,
        /// <summary>
        /// The Task has never been run.
        /// </summary>
        NotRun,
        /// <summary>
        /// The Task is queued to run.
        /// </summary>
        RunPending,
        /// <summary>
        /// The Task is waiting for its result from a client.
        /// </summary>
        WaitingForExternalResult,
        /// <summary>
        /// The Task state is unknown, likely due to a Service error.
        /// </summary>
        Unknown
    }

    /// <summary>
    /// The type of Task.
    /// </summary>
    public enum TaskType
    {
        /// <summary>
        /// The Task is a executable (exe) task.
        /// </summary>
        Executable = 0,
        /// <summary>
        /// The Task is a executable (exe) task.
        /// </summary>
        ConsoleExe = Executable,


        /// <summary>
        /// The Task is a TAEF test.
        /// </summary>
        TAEFDll = 1,
        /// <summary>
        /// The Task is an external task.
        /// </summary>
        External = 2,
        /// <summary>
        /// The Task is a UWP app.
        /// </summary>
        UWP = 3,
        /// <summary>
        /// The Task is a PowerShell Core script.
        /// </summary>
        PowerShell = 4,
        /// <summary>
        /// The Task is a Command Prompt script.
        /// </summary>
        BatchFile = 5
    }

#pragma warning disable CS1591 //  Missing XML comment for publicly visible type or member
    /// <summary>
    /// Comparer for Task objects
    /// </summary>
    /// <exclude/>
    public class TaskBaseEqualityComparer : EqualityComparer<TaskBase>
    {
        public override bool Equals(TaskBase x, TaskBase y)
        {
            if (x == null)
            {
                throw new ArgumentNullException(nameof(x));
            }

            return x.Equals(y);
        }

        public override int GetHashCode(TaskBase obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            return obj.GetHashCode();
        }
    }
#pragma warning restore CS1591

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
    public abstract class TaskBase : NotifyPropertyChangedBase
    {
        // TODO: Quality: Use Semaphore internally to guarantee accurate state if many things are setting task state
        // lock on modification & lock on query so that internal state is guaranteed to be consistent at all times
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="type">The Task type.</param>
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
            RunInContainer = false;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="type">The Task type.</param>
        /// <param name="taskPath">The Task path.</param>
        public TaskBase(string taskPath, TaskType type) : this(type)
        {
            Guid = Guid.NewGuid();
            Path = taskPath;
        }

        /// <summary>
        /// Create a "deep" copy of the Task.
        /// </summary>
        /// <returns></returns>
        public TaskBase DeepCopy()
        {
            TaskBase copy;

            lock (TaskLock)
            {
                copy = (TaskBase)this.MemberwiseClone();
                var stringProps = this.GetType().GetProperties().Where(x => x.PropertyType == typeof(string));
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
            }

            return copy;
        }

        // TODO: Make only getters and add internal apis to set
        /// <summary>
        /// The friendly name of the Task.
        /// </summary>
        [XmlAttribute("Name")]
        public virtual string Name
        {
            get
            {
                return Path;
            }
            set { }
        }

        /// <summary>
        /// The type of the Task.
        /// </summary>
        [XmlIgnore]
        public TaskType Type
        {
            get => _type;
            set
            {
                if (!Equals(value, _type))
                {
                    _type = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private TaskType _type;

        /// <summary>
        /// The path to the file used for the Task such an Exe.
        /// </summary>
        [XmlAttribute]
        public string Path
        {
            get => _path;
            set
            {
                if (!Equals(value, _path))
                {
                    _path = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private string _path;

        /// <summary>
        /// The arguments passed to the Task.
        /// </summary>
        [XmlAttribute]
        public string Arguments
        {
            get => _arguments;
            set
            {
                if (!Equals(value, _arguments))
                {
                    _arguments = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private string _arguments;

        /// <summary>
        /// The GUID identifying the Task.
        /// </summary>
        [XmlAttribute]
        public Guid Guid
        {
            get => _guid;
            set
            {
                if (!Equals(value, _guid))
                {
                    _guid = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private Guid _guid;

        /// <summary>
        /// The time the latest run of this Task started. NULL if it has never started.
        /// </summary>
        public DateTime? LatestTaskRunTimeStarted
        {
            get => _latestTaskRunTimeStarted;
            set
            {
                if (!Equals(value, _latestTaskRunTimeStarted))
                {
                    _latestTaskRunTimeStarted = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private DateTime? _latestTaskRunTimeStarted;

        /// <summary>
        /// The time the latest run of this Task finished. NULL if it has never finished.
        /// </summary>
        public DateTime? LatestTaskRunTimeFinished
        {
            get => _latestTaskRunTimeFinished;
            set
            {
                if (!Equals(value, _latestTaskRunTimeFinished))
                {
                    _latestTaskRunTimeFinished = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private DateTime? _latestTaskRunTimeFinished;

        /// <summary>
        /// The status of the latest run of this Task.
        /// </summary>
        public TaskStatus LatestTaskRunStatus
        {
            get => _latestTaskRunStatus;
            set
            {
                if (!Equals(value, _latestTaskRunStatus))
                {
                    _latestTaskRunStatus = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private TaskStatus _latestTaskRunStatus;

        /// <summary>
        /// True if the latest run of this Task passed. NULL if it has never been run.
        /// </summary>
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

        /// <summary>
        /// The exit code of the latest run of this Task. NULL if it has never completed.
        /// </summary>
        public int? LatestTaskRunExitCode
        {
            get => _latestTaskRunExitCode;
            set
            {
                if (!Equals(value, _latestTaskRunExitCode))
                {
                    _latestTaskRunExitCode = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private int? _latestTaskRunExitCode;

        /// <summary>
        /// The amount of time elapsed while running the latest run of this Task. NULL if it has never started.
        /// </summary>
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
                    else if (LatestTaskRunStatus != TaskStatus.Unknown)
                    {
                        return DateTime.Now - LatestTaskRunTimeStarted;
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// GUID of the latest run of this Task. NULL if it has never started.
        /// </summary>
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

        /// <summary>
        /// True if the Task is running or queued to run.
        /// </summary>
        public bool IsRunningOrPending
        {
            get
            {
                return ((LatestTaskRunStatus == TaskStatus.Running) || (LatestTaskRunStatus == TaskStatus.RunPending) || (LatestTaskRunStatus == TaskStatus.WaitingForExternalResult));
            }
        }

        /// <summary>
        /// The timeout for this Task, in seconds.
        /// </summary>
        [XmlAttribute("Timeout")]
        public int TimeoutSeconds
        {
            get => _timeoutSeconds;
            set
            {
                if (!Equals(value, _timeoutSeconds))
                {
                    _timeoutSeconds = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private int _timeoutSeconds;

        /// <summary>
        /// The GUIDs for all runs of this Task.
        /// </summary>
        [XmlArrayItem("Guid")]
        public List<Guid> TaskRunGuids
        {
            get => _taskRunGuids;
            set
            {
                if (!Equals(value, _taskRunGuids))
                {
                    _taskRunGuids = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private List<Guid> _taskRunGuids;

        /// <summary>
        /// True if this Task is run by the server, such as an ExecutableTask.
        /// </summary>
        public bool RunByServer
        {
            get
            {
                return (!RunInContainer) && (Type != TaskType.External) && (Type != TaskType.UWP);
            }
        }

        /// <summary>
        /// True if this Task is run by the client, such as an ExternalTask.
        /// </summary>
        public bool RunByClient
        {
            get
            {
                return !RunByServer;
            }
        }

        /// <summary>
        /// If true, the TaskList running this Task is aborted if this Task fails.
        /// </summary>
        [XmlAttribute]
        public bool AbortTaskListOnFailed
        {
            get => _abortTaskListOnFailed;
            set
            {
                if (!Equals(value, _abortTaskListOnFailed))
                {
                    _abortTaskListOnFailed = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private bool _abortTaskListOnFailed;

        /// <summary>
        /// The number of re-runs the Task automatically attempts if the run fails.
        /// </summary>
        [XmlAttribute]
        public uint MaxNumberOfRetries
        {
            get => _maxNumberOfRetries;
            set
            {
                if (!Equals(value, _maxNumberOfRetries))
                {
                    _maxNumberOfRetries = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private uint _maxNumberOfRetries;

        /// <summary>
        /// The number of reties so far for this latest run.
        /// </summary>
        public uint TimesRetried
        {
            get => _timesRetried;
            set
            {
                if (!Equals(value, _timesRetried))
                {
                    _timesRetried = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private uint _timesRetried;

        /// <summary>
        /// If true, the task is executed inside the Win32 container.
        /// </summary>
        [XmlAttribute]
        public bool RunInContainer
        {
            get => _runInContainer;
            set
            {
                if (!Equals(value, _runInContainer))
                {
                    _runInContainer = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private bool _runInContainer;

        // XmlSerializer calls these to check if these values are set.
        // If not set, don't serialize.
        // https://docs.microsoft.com/en-us/dotnet/framework/winforms/controls/defining-default-values-with-the-shouldserialize-and-reset-methods
        /// <summary>
        /// XmlSerializer calls to check if this should be serialized.
        /// </summary>
        /// <returns>true if it should be serialized.</returns>
        public bool ShouldSerializeLatestTaskRunTimeStarted()
        {
            return LatestTaskRunTimeStarted.HasValue;
        }

        /// <summary>
        /// XmlSerializer calls to check if this should be serialized.
        /// </summary>
        /// <returns>true if it should be serialized.</returns>
        public bool ShouldSerializeLatestTaskRunStatus()
        {
            return LatestTaskRunStatus != TaskStatus.NotRun;
        }

        /// <summary>
        /// XmlSerializer calls to check if this should be serialized.
        /// </summary>
        /// <returns>true if it should be serialized.</returns>
        public bool ShouldSerializeLatestTaskRunTimeFinished()
        {
            return LatestTaskRunTimeFinished.HasValue;
        }

        /// <summary>
        /// XmlSerializer calls to check if this should be serialized.
        /// </summary>
        /// <returns>true if it should be serialized.</returns>
        public bool ShouldSerializeLatestTaskRunExitCode()
        {
            return LatestTaskRunExitCode.HasValue;
        }

        /// <summary>
        /// XmlSerializer calls to check if this should be serialized.
        /// </summary>
        /// <returns>true if it should be serialized.</returns>
        public bool ShouldSerializeTaskRunGuids()
        {
            return TaskRunGuids.Count > 0;
        }

        /// <summary>
        /// XmlSerializer calls to check if this should be serialized.
        /// </summary>
        /// <returns>true if it should be serialized.</returns>
        public bool ShouldSerializeTimeoutSeconds()
        {
            return TimeoutSeconds != -1;
        }

        /// <summary>
        /// XmlSerializer calls to check if this should be serialized.
        /// </summary>
        /// <returns>true if it should be serialized.</returns>
        public bool ShouldSerializeTimesRetried()
        {
            return TimesRetried != 0;
        }

        /// <summary>
        /// XmlSerializer calls to check if this should be serialized.
        /// </summary>
        /// <returns>true if it should be serialized.</returns>
        public bool ShouldSerializeMaxNumberOfRetries()
        {
            return MaxNumberOfRetries != 0;
        }

        /// <summary>
        /// XmlSerializer calls to check if this should be serialized.
        /// </summary>
        /// <returns>true if it should be serialized.</returns>
        public bool ShouldSerializeAbortTaskListOnFailed()
        {
            return AbortTaskListOnFailed == true;
        }

        /// <summary>
        /// XmlSerializer calls to check if this should be serialized.
        /// </summary>
        /// <returns>true if it should be serialized.</returns>
        public bool ShouldSerializeRunInContainer()
        {
            return RunInContainer == true;
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
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

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
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

        // TODO: server only
        /// <summary>
        /// Lock used by the server when updating values in the Task object. Clients should not use.
        /// </summary>
        [JsonIgnore]
        [XmlIgnore]
        public object TaskLock { get; private set; }

        /// <summary>
        /// Creates a Task object from a TaskRun object.
        /// </summary>
        /// <param name="run">The TaskRun.</param>
        /// <returns>a TaskBase object representing the Task defined by the Run.</returns>
        /// <exception cref="InvalidEnumArgumentException">Given TaskRun has an invalid TaskType</exception>
        public static TaskBase CreateTaskFromTaskRun(TaskRun run)
        {
            if (run == null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            TaskBase task;
            switch (run.TaskType)
            {
                case TaskType.BatchFile:
                    task = new BatchFileTask(run.TaskPath);
                    (task as ExecutableTask).BackgroundTask = run.BackgroundTask;
                    break;
                case TaskType.Executable:
                    task = new ExecutableTask(run.TaskPath);
                    (task as ExecutableTask).BackgroundTask = run.BackgroundTask;
                    break;
                case TaskType.External:
                    task = new ExternalTask(run.TaskName);
                    break;
                case TaskType.PowerShell:
                    task = new PowerShellTask(run.TaskPath);
                    (task as ExecutableTask).BackgroundTask = run.BackgroundTask;
                    break;
                case TaskType.TAEFDll:
                    task = new TAEFTest(run.TaskPath);
                    break;
                case TaskType.UWP:
                    task = new UWPTask(run.TaskPath);
                    (task as ExecutableTask).BackgroundTask = run.BackgroundTask;
                    break;
                default:
                    throw new InvalidEnumArgumentException(Resources.InvalidTaskRunTypeException);
            }

            task.Name = run.TaskName;
            task.Arguments = run.Arguments;

            if (run.OwningTaskGuid != null)
            {
                task.Guid = (Guid)run.OwningTaskGuid;
            }

            return task;
        }
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

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutableTask"/> class.
        /// </summary>
        /// <param name="taskPath">The task path.</param>
        public ExecutableTask(String taskPath) : base(taskPath, TaskType.ConsoleExe)
        {
            BackgroundTask = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutableTask"/> class.
        /// </summary>
        /// <param name="taskPath">The Task path.</param>
        /// <param name="type">The Task type.</param>
        protected ExecutableTask(String taskPath, TaskType type) : base(taskPath, type)
        {
            BackgroundTask = false;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            var rhs = obj as ExecutableTask;

            if (rhs == null)
            {
                return false;
            }

            return base.Equals(obj as TaskBase);
        }

        /// <summary>
        /// The friendly name of the Task.
        /// </summary>
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
                NotifyPropertyChanged();
            }
        }
        private string _testFriendlyName;

        /// <summary>
        /// Denotes if this Task is run as a background task.
        /// </summary>
        [XmlIgnore]
        public bool BackgroundTask
        {
            get => _backgroundTask;
            set
            {
                if (!Equals(value, _backgroundTask))
                {
                    _backgroundTask = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private bool _backgroundTask;
    }

    /// <summary>
    /// An PowerShellTask is a PowerShell Core .ps1 script that is run by the FactoryOrchestratorServer. The exit code of the script determines if the task passed or failed.
    /// 0 == PASS, all others == FAIL.
    /// </summary>
    [JsonConverter(typeof(NoConverter))]
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public class PowerShellTask : ExecutableTask
    {
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()

        private PowerShellTask() : base(null, TaskType.PowerShell)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PowerShellTask"/> class.
        /// </summary>
        /// <param name="scriptPath">The script path.</param>
        public PowerShellTask(string scriptPath) : base(scriptPath, TaskType.PowerShell)
        {
            _scriptPath = scriptPath;
        }

        /// <summary>
        /// The friendly name of the Task.
        /// </summary>
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

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            var rhs = obj as TaskBase;

            if (rhs == null)
            {
                return false;
            }

            return base.Equals(obj as ExecutableTask);
        }

        private readonly string _scriptPath;
        private string _testFriendlyName;
    }

    /// <summary>
    /// An BatchFile is a .cmd or .bat script that is run by the FactoryOrchestratorServer. The exit code of the script determines if the task passed or failed.
    /// 0 == PASS, all others == FAIL.
    /// </summary>
    [JsonConverter(typeof(NoConverter))]
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public class BatchFileTask : ExecutableTask
    {
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()

        private BatchFileTask() : base(null, TaskType.BatchFile)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BatchFileTask"/> class.
        /// </summary>
        /// <param name="scriptPath">The script path.</param>
        public BatchFileTask(string scriptPath) : base(scriptPath, TaskType.BatchFile)
        {
            _scriptPath = scriptPath;
        }

        /// <summary>
        /// The friendly name of the Task.
        /// </summary>
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

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            var rhs = obj as TaskBase;

            if (rhs == null)
            {
                return false;
            }

            return base.Equals(obj as ExecutableTask);
        }

        private readonly string _scriptPath;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="TAEFTest"/> class.
        /// </summary>
        /// <param name="testPath">The test path.</param>
        public TAEFTest(string testPath) : base(testPath, TaskType.TAEFDll)
        {
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalTask"/> class.
        /// </summary>
        /// <param name="testName">Name of the Task.</param>
        public ExternalTask(String testName) : base(null, TaskType.External)
        {
            _testFriendlyName = testName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalTask"/> class.
        /// </summary>
        /// <param name="taskPath">The task path.</param>
        /// <param name="testName">Name of the Task.</param>
        /// <param name="type">The external Task type.</param>
        protected ExternalTask(String taskPath, String testName, TaskType type) : base(taskPath, type)
        {
            _testFriendlyName = testName;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
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

        /// <summary>
        /// The friendly name of the Task.
        /// </summary>
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
    /// A UWPTest is a UWP task run by the FactoryOrchestrator.App client. These are used for UI.
    /// task results must be returned to the server via SetTaskRunStatus().
    /// </summary>
    [JsonConverter(typeof(NoConverter))]
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public class UWPTask : ExternalTask
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    {
        private UWPTask() : base(null, null, TaskType.UWP)
        {
            AutoPassedIfLaunched = false;
            TerminateOnCompleted = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UWPTask"/> class.
        /// </summary>
        /// <param name="packageFamilyName">The package family name (PFN).</param>
        /// <param name="testFriendlyName">A friendly name for the app.</param>
        public UWPTask(string packageFamilyName, string testFriendlyName) : base(packageFamilyName, testFriendlyName, TaskType.UWP)
        {
            _testFriendlyName = testFriendlyName;
            AutoPassedIfLaunched = false;
            TerminateOnCompleted = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UWPTask"/> class.
        /// </summary>
        /// <param name="packageFamilyName">The package family name (PFN).</param>
        public UWPTask(string packageFamilyName) : base(packageFamilyName, null, TaskType.UWP)
        {
            _testFriendlyName = packageFamilyName;
            AutoPassedIfLaunched = false;
            TerminateOnCompleted = true;
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
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

        /// <summary>
        /// The friendly name of the Task.
        /// </summary>
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

        /// <summary>
        /// Gets or sets a value indicating whether this Task is automatically marked as Passed if the app is launched.
        /// </summary>
        /// <value>
        ///   If <c>true</c>, if this UWP app is successfully invoked, the TaskRun is marked as passed; otherwise, if <c>false</c>, the TaskRun must be manually passed via UpdateTaskRun().
        /// </value>
        [XmlAttribute]
        public bool AutoPassedIfLaunched { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this UWP app is terminated when the Task is completed (Passed or Failed). If AutoPassedIfLaunched is <c>true</c>, this value is ignored.
        /// </summary>
        /// <value>
        ///   If <c>true</c>, the app is automatically terminated when the TaskRun is completed.
        /// </value>
        [XmlAttribute]
        public bool TerminateOnCompleted { get; set; }

        private string _testFriendlyName;
    }

    /// <summary>
    /// A TaskList is a grouping of Factory Orchestrator Tasks.
    /// </summary>
    public class TaskList : NotifyPropertyChangedBase
    {
        [JsonConstructor]
        internal TaskList()
        {
            Tasks = new List<TaskBase>();
            BackgroundTasks = new List<TaskBase>();
            RunInParallel = false;
            AllowOtherTaskListsToRun = false;
            TerminateBackgroundTasksOnCompletion = true;
            Name = "";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskList"/> class. Used for editing an existing TaskList.
        /// </summary>
        /// <param name="name">The TaskList name.</param>
        /// <param name="guid">The GUID for the TaskList.</param>
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

        /// <summary>
        /// Validates the TaskList is compliant with the XSD schema and other requirements.
        /// Will assign a random Guid to any Task missing one.
        /// </summary>
        /// <exception cref="XmlSchemaValidationException">If the TaskList has an issue</exception>
        public void ValidateTaskList()
        {
            if (Guid == Guid.Empty)
            {
                Guid = Guid.NewGuid();
            }

            foreach (var task in Tasks)
            {
                if (task.Guid == Guid.Empty)
                {
                    task.Guid = Guid.NewGuid();
                }

                if (task.RunInContainer)
                {
                    if (task is UWPTask || task is ExternalTask || task is TAEFTest)
                    {
                        throw new XmlSchemaValidationException(Resources.ContainerTaskTypeException);
                    }
                }

                if (task is UWPTask && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    throw new XmlSchemaValidationException(string.Format(CultureInfo.CurrentCulture, Resources.WindowsOnlyError, "UWPTask"));
                }
            }

            foreach (var bgtask in BackgroundTasks)
            {
                // Validate background tasks meet requirements
                if ((bgtask as ExecutableTask) == null)
                {
                    throw new XmlSchemaValidationException(Resources.BackgroundTaskTypeException);
                }

                if (bgtask.TimeoutSeconds != -1)
                {
                    throw new XmlSchemaValidationException(Resources.BackgroundTimeoutException);
                }

                if (bgtask.MaxNumberOfRetries != 0)
                {
                    throw new XmlSchemaValidationException(Resources.BackgroundRetryException);
                }

                if (bgtask.Guid == Guid.Empty)
                {
                    bgtask.Guid = Guid.NewGuid();
                }

                (bgtask as ExecutableTask).BackgroundTask = true;
            }
        }

        /// <summary>
        /// The status of the TaskList.
        /// </summary>
        public TaskStatus TaskListStatus
        {
            get
            {
                if (Tasks.All(x => x.LatestTaskRunPassed == true))
                {
                    return TaskStatus.Passed;
                }
                else if (Tasks.All(x => x.LatestTaskRunStatus == TaskStatus.RunPending))
                {
                    return TaskStatus.RunPending;
                }
                else if (Tasks.Any(x => x.LatestTaskRunStatus == TaskStatus.Aborted))
                {
                    return TaskStatus.Aborted;
                }
                else if (Tasks.Any(x => x.IsRunningOrPending))
                {
                    return TaskStatus.Running;
                }
                else if (Tasks.Any(x => x.LatestTaskRunStatus == TaskStatus.Unknown))
                {
                    return TaskStatus.Unknown;
                }
                else if (Tasks.Any(x => (x.LatestTaskRunPassed != null) && ((bool)x.LatestTaskRunPassed == false)))
                {
                    return TaskStatus.Failed;
                }
                else
                {
                    return TaskStatus.NotRun;
                }
            }
        }

        /// <summary>
        /// True if the TaskList is running or queued to run.
        /// </summary>
        public bool IsRunningOrPending
        {
            get
            {
                return ((TaskListStatus == TaskStatus.Running) || (TaskListStatus == TaskStatus.RunPending));
            }
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.TaskListToString, Name, Guid.ToString(), TaskListStatus.ToString());
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
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

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return -2045414129 + EqualityComparer<Guid>.Default.GetHashCode(Guid);
        }

        /// <summary>
        /// XmlSerializer calls to check if this should be serialized.
        /// </summary>
        /// <returns>true if it should be serialized.</returns>
        public bool ShouldSerializeTerminateBackgroundTasksOnCompletion()
        {
            return BackgroundTasks.Count > 0;
        }

        /// <summary>
        /// XmlSerializer calls to check if this should be serialized.
        /// </summary>
        /// <returns>true if it should be serialized.</returns>
        public bool ShouldSerializeBackgroundTasks()
        {
            return BackgroundTasks.Count > 0;
        }

        /// <summary>
        /// XmlSerializer calls to check if this should be serialized.
        /// </summary>
        /// <returns>true if it should be serialized.</returns>
        public bool ShouldSerializeTasks()
        {
            return Tasks.Count > 0;
        }

        /// <summary>
        /// The Tasks in the TaskList.
        /// </summary>
        [XmlArrayItem("Task")]
        [XmlArray("Tasks")]
        public List<TaskBase> Tasks
        {
            get => _tasks;
            set
            {
                if (!Equals(value, _tasks))
                {
                    _tasks = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private List<TaskBase> _tasks;


        /// <summary>
        /// Background Tasks in the TaskList.
        /// </summary>
        [XmlArrayItem("Task")]
        [XmlArray("BackgroundTasks")]
        public List<TaskBase> BackgroundTasks
        {
            get => _backgroundTasks;
            set
            {
                if (!Equals(value, _backgroundTasks))
                {
                    _backgroundTasks = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private List<TaskBase> _backgroundTasks;

        /// <summary>
        /// The GUID identifying this TaskList.
        /// </summary>
        [XmlAttribute]
        public Guid Guid
        {
            get => _guid;
            set
            {
                if (!Equals(value, _guid))
                {
                    _guid = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private Guid _guid;

        /// <summary>
        /// The name of this TaskList.
        /// </summary>
        [XmlAttribute]
        public string Name
        {
            get => _name;
            set
            {
                if (!Equals(value, _name))
                {
                    _name = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private string _name;

        /// <summary>
        /// If true, Tasks in this TaskList are run in parallel. Order is non-deterministic.
        /// </summary>
        [XmlAttribute]
        public bool RunInParallel
        {
            get => _runInParallel;
            set
            {
                if (!Equals(value, _runInParallel))
                {
                    _runInParallel = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private bool _runInParallel;

        /// <summary>
        /// If false, while this TaskList is running no other TaskList may run.
        /// </summary>
        [XmlAttribute]
        public bool AllowOtherTaskListsToRun
        {
            get => _allowOtherTaskListsToRun;
            set
            {
                if (!Equals(value, _allowOtherTaskListsToRun))
                {
                    _allowOtherTaskListsToRun = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private bool _allowOtherTaskListsToRun;

        /// <summary>
        /// If true, Background Tasks defined in this TaskList are forcibly terminated when the TaskList stops running.
        /// </summary>
        [XmlAttribute]
        public bool TerminateBackgroundTasksOnCompletion
        {
            get => _terminateBackgroundTasksOnCompletion;
            set
            {
                if (!Equals(value, _terminateBackgroundTasksOnCompletion))
                {
                    _terminateBackgroundTasksOnCompletion = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private bool _terminateBackgroundTasksOnCompletion;
    }

    /// <summary>
    /// A TaskRun represents one instance of executing any single Task.
    /// </summary>
    public class TaskRun : NotifyPropertyChangedBase
    {
        // TODO: Quality: Use Semaphore internally to guarantee accurate state if many things are setting task state
        // lock on modification & lock on query so that internal state is guaranteed to be consistent at all times
        [JsonConstructor]
        internal TaskRun()
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
            TaskStatus = TaskStatus.RunPending;
            TimeFinished = null;
            TimeStarted = null;
            ExitCode = null;
            TaskOutput = new LockingList<string>();
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
                RunInContainer = owningTask.RunInContainer;

                if (owningTask as ExecutableTask != null)
                {
                    BackgroundTask = ((ExecutableTask)owningTask).BackgroundTask;
                }
            }
        }

        /// <summary>
        /// Create a "deep" copy of the TaskRun. WARNING: If the object is a ServerTaskRun information is lost!
        /// </summary>
        /// <returns></returns>
        public TaskRun DeepCopy()
        {
            lock (_taskOutput.SyncRoot)
            {
                return JsonConvert.DeserializeObject<TaskRun>(JsonConvert.SerializeObject(this));
            }
        }

        /// <summary>
        /// The output of the Task.
        /// </summary>
        public LockingList<string> TaskOutput
        {
            get => _taskOutput;
            set
            {
                if (!Equals(value, _taskOutput))
                {
                    _taskOutput = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private LockingList<string> _taskOutput;

        /// <summary>
        /// The GUID of the Task which created this run. NULL if this run is not associated with a Task.
        /// </summary>
        public Guid? OwningTaskGuid
        {
            get => _owningTaskGuid;
            set
            {
                if (!Equals(value, _owningTaskGuid))
                {
                    _owningTaskGuid = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private Guid? _owningTaskGuid;

        /// <summary>
        /// The name of the Task (at the time the run started).
        /// </summary>
        public string TaskName
        {
            get => _taskName;
            set
            {
                if (!Equals(value, _taskName))
                {
                    _taskName = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private string _taskName;

        /// <summary>
        /// The path of the Task (at the time the run started).
        /// </summary>
        public string TaskPath
        {
            get => _taskPath;
            set
            {
                if (!Equals(value, _taskPath))
                {
                    _taskPath = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private string _taskPath;

        /// <summary>
        /// The arguments of the Task (at the time the run started).
        /// </summary>
        public string Arguments
        {
            get => _arguments;
            set
            {
                if (!Equals(value, _arguments))
                {
                    _arguments = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private string _arguments;

        /// <summary>
        /// Denotes if this TaskRun is for a background task.
        /// </summary>
        public bool BackgroundTask
        {
            get => _backgroundTask;
            set
            {
                if (!Equals(value, _backgroundTask))
                {
                    _backgroundTask = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private bool _backgroundTask;

        /// <summary>
        /// The type of the Task which created this run.
        /// </summary>
        public TaskType TaskType
        {
            get => _taskType;
            set
            {
                if (!Equals(value, _taskType))
                {
                    _taskType = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private TaskType _taskType;

        /// <summary>
        /// The GUID identifying this TaskRun.
        /// </summary>
        public Guid Guid
        {
            get => _guid;
            set
            {
                if (!Equals(value, _guid))
                {
                    _guid = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private Guid _guid;

        /// <summary>
        /// The time this run started. NULL if it has never started.
        /// </summary>
        public DateTime? TimeStarted
        {
            get => _timeStarted;
            set
            {
                if (!Equals(value, _timeStarted))
                {
                    _timeStarted = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private DateTime? _timeStarted;

        /// <summary>
        /// The time this run finished. NULL if it has never finished.
        /// </summary>
        public DateTime? TimeFinished
        {
            get => _timeFinished;
            set
            {
                if (!Equals(value, _timeFinished))
                {
                    _timeFinished = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private DateTime? _timeFinished;

        /// <summary>
        /// The status of this run.
        /// </summary>
        public TaskStatus TaskStatus
        {
            get => _taskStatus;
            set
            {
                if (!Equals(value, _taskStatus))
                {
                    _taskStatus = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private TaskStatus _taskStatus;

        /// <summary>
        /// The path to the log file for this run. NULL if it is not logged to a file.
        /// </summary>
        public string LogFilePath
        {
            get => _logFilePath;
            set
            {
                if (!Equals(value, _logFilePath))
                {
                    _logFilePath = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private string _logFilePath;

        /// <summary>
        /// The exit code of this run. NULL if it has not finished.
        /// </summary>
        public int? ExitCode
        {
            get => _exitCode;
            set
            {
                if (!Equals(value, _exitCode))
                {
                    _exitCode = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private int? _exitCode;

        /// <summary>
        /// The timeout for this run.
        /// </summary>
        public int TimeoutSeconds
        {
            get => _timeoutSeconds;
            set
            {
                if (!Equals(value, _timeoutSeconds))
                {
                    _timeoutSeconds = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private int _timeoutSeconds;

        /// <summary>
        /// If true, the TaskRun is executed inside the Win32 container.
        /// </summary>
        public bool RunInContainer
        {
            get => _runInContainer;
            set
            {
                if (!Equals(value, _runInContainer))
                {
                    _runInContainer = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private bool _runInContainer;

        /// <summary>
        /// True if this TaskRun is run by the server, such as an ExecutableTask.
        /// </summary>
        public bool RunByServer
        {
            get
            {
                return (!RunInContainer) && (TaskType != TaskType.External) && (TaskType != TaskType.UWP);
            }
        }


        /// <summary>
        /// True if this TaskRun is run by the client, such as an ExternalTask.
        /// </summary>
        public bool RunByClient
        {
            get
            {
                return !RunByServer;
            }
        }

        /// <summary>
        /// True if this run is finished executing.
        /// </summary>
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

        /// <summary>
        /// The amount of time this run executed for. NULL if it has never started.
        /// </summary>
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
                    else if (TaskStatus != TaskStatus.Unknown)
                    {
                        return DateTime.Now - TimeStarted;
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
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

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return -737073652 + EqualityComparer<Guid>.Default.GetHashCode(Guid);
        }
    }

    /// <summary>
    /// This class is used to save and load TaskLists from an XML file.
    /// </summary>
    [XmlRootAttribute(ElementName = "FactoryOrchestratorXML", IsNullable = false)]
    public partial class FactoryOrchestratorXML
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public FactoryOrchestratorXML()
        {
            TaskLists = new List<TaskList>();
        }

        /// <summary>
        /// The TaskLists in the XML file.
        /// </summary>
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
                list.ValidateTaskList();
            }

            // Check the XML for any duplicate GUIDs
            var allGuids = TaskLists.Select(x => x.Guid).Concat(TaskLists.SelectMany(y => y.Tasks.Select(z => z.Guid)));
            var hash = new HashSet<Guid>();
            var dupGuidString = string.Empty;
            foreach (var guid in allGuids)
            {
                // If false, the GUID already was added, so it's a duplicate
                if (!hash.Add(guid))
                {
                    dupGuidString += $"{guid.ToString()}, ";
                }
            }

            if (!string.IsNullOrEmpty(dupGuidString))
            {
                throw new XmlSchemaValidationException(string.Format(CultureInfo.CurrentCulture, Resources.DuplicateGuidInXml, dupGuidString));
            }
        }

        /// <summary>
        /// Loads the TaskLists in a FactoryOrchestratorXML file.
        /// </summary>
        /// <param name="filename">The FactoryOrchestratorXML file to load.</param>
        /// <returns>a FactoryOrchestratorXML object that can then be parsed by the Server.</returns>
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
                        throw new FileNotFoundException(string.Format(CultureInfo.CurrentCulture, Resources.FileNotFoundException, filename));
                    }

                    // Validate XSD
                    var asm = Assembly.GetAssembly(typeof(FactoryOrchestratorXML));
                    using (Stream xsdStream = asm.GetManifestResourceStream(GetResourceName(Assembly.GetAssembly(typeof(FactoryOrchestratorXML)), "FactoryOrchestratorXML.xsd", false)))
                    {
                        XmlReaderSettings settings = new XmlReaderSettings
                        {
                            XmlResolver = null
                        };

                        using (XmlReader xsdReader = XmlReader.Create(xsdStream, settings))
                        {
                            XmlSchema xmlSchema = xmlSchema = XmlSchema.Read(xsdReader, ValidationEventHandler);

                            using (XmlReader reader = XmlReader.Create(filename, settings))
                            {
                                XmlDocument document = new XmlDocument
                                {
                                    XmlResolver = null
                                };
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
                throw new FactoryOrchestratorXmlException(string.Format(CultureInfo.CurrentCulture, Resources.FOXMLFileLoadException, filename), e);
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
            resourceIdentifier = resourceIdentifier.ToUpperInvariant();
            var ListOfResources = assembly.GetManifestResourceNames();

            foreach (string resource in ListOfResources)
            {
                if (matchWholeWord)
                {
                    if (resource.ToUpperInvariant().Equals(resourceIdentifier, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return resource;
                    }
                }
                else if (resource.ToUpperInvariant().Contains(resourceIdentifier))
                {
                    return resource;
                }
            }

            throw new FileNotFoundException("Could not find embedded resource", resourceIdentifier);
        }

        /// <summary>
        /// Saves a FactoryOrchestratorXML object to the given file. The file is overwritten if it exists.
        /// </summary>
        /// <param name="filename">The path of the FactoryOrchestratorXML file you want to create.</param>
        /// <returns></returns>
        public bool Save(string filename)
        {
            var xmlWriterSettings = new XmlWriterSettings() { Indent = true };
            using (XmlWriter writer = XmlWriter.Create(filename, xmlWriterSettings))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(FactoryOrchestratorXML));
                serializer.Serialize(writer, this);
            }

            return true;
        }

        private static bool XmlIsValid { get; set; }
        private static readonly object XmlSerializeLock = new object();
        private static string ValidationErrors;
    }

    /// <summary>
    /// A helper class containing basic information about a TaskList. Use to quickly update clients about TaskLists and their statuses.
    /// </summary>
    public struct TaskListSummary : IEquatable<TaskListSummary>
    {
        /// <summary>
        /// Creates a new TaskListSummary.
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="name"></param>
        /// <param name="status"></param>
        /// <param name="runInParallel"></param>
        /// <param name="allowOtherTaskListsToRun"></param>
        /// <param name="terminateBackgroundTasksOnCompletion"></param>
        public TaskListSummary(Guid guid, string name, TaskStatus status, bool runInParallel, bool allowOtherTaskListsToRun, bool terminateBackgroundTasksOnCompletion)
        {
            Guid = guid;
            Status = status;
            Name = name;
            RunInParallel = runInParallel;
            AllowOtherTaskListsToRun = allowOtherTaskListsToRun;
            TerminateBackgroundTasksOnCompletion = terminateBackgroundTasksOnCompletion;
        }

        /// <summary>
        /// Creates a new TaskListSummary.
        /// </summary>
        /// <param name="summary">The summary to copy from.</param>
        public TaskListSummary(TaskListSummary summary)
        {
            this.Status = summary.Status;
            this.Name = summary.Name;
            this.Guid = summary.Guid;
            this.AllowOtherTaskListsToRun = summary.AllowOtherTaskListsToRun;
            this.RunInParallel = summary.RunInParallel;
            this.TerminateBackgroundTasksOnCompletion = summary.TerminateBackgroundTasksOnCompletion;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            // Accessible name.
            return string.Format(CultureInfo.CurrentCulture, Resources.TaskListToString, Name, Guid.ToString(), Status.ToString());
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return Guid.GetHashCode() + Status.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (!(obj is TaskListSummary))
            {
                return false;
            }

            return Equals((TaskListSummary)obj);
        }

        /// <summary>
        /// Determines whether the specified <see cref="TaskListSummary" />, is equal to this instance.
        /// </summary>
        /// <param name="rhs">The <see cref="TaskListSummary" /> to compare with this instance.</param>
        /// <returns></returns>
        public bool Equals(TaskListSummary rhs)
        {
            if (!Guid.Equals(rhs.Guid))
            {
                return false;
            }

            if (!Name.Equals(rhs.Name, StringComparison.CurrentCulture))
            {
                return false;
            }

            if (!Status.Equals(rhs.Status))
            {
                return false;
            }

            if (!RunInParallel.Equals(rhs.RunInParallel))
            {
                return false;
            }

            if (!AllowOtherTaskListsToRun.Equals(rhs.AllowOtherTaskListsToRun))
            {
                return false;
            }

            if (!TerminateBackgroundTasksOnCompletion.Equals(rhs.TerminateBackgroundTasksOnCompletion))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="first">The first.</param>
        /// <param name="second">The second.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(TaskListSummary first, TaskListSummary second)
        {
            return first.Equals(second);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="first">The first.</param>
        /// <param name="second">The second.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(TaskListSummary first, TaskListSummary second)
        {
            return !first.Equals(second);
        }

        /// <summary>
        /// The GUID identifying the TaskList.
        /// </summary>
        public Guid Guid { get; set; }

        /// <summary>
        /// The status of the TaskList.
        /// </summary>
        public TaskStatus Status { get; set; }

        /// <summary>
        /// The name of the TaskList.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// If true, Tasks in this TaskList are run in parallel. Order is non-deterministic.
        /// </summary>
        public bool RunInParallel { get; set; }

        /// <summary>
        /// If false, while this TaskList is running no other TaskList may run.
        /// </summary>
        public bool AllowOtherTaskListsToRun { get; set; }

        /// <summary>
        /// If true, Background Tasks defined in this TaskList are forcibly terminated when the TaskList stops running.
        /// </summary>
        public bool TerminateBackgroundTasksOnCompletion { get; set; }

        /// <summary>
        /// True if the TaskList is running or queued to run.
        /// </summary>
        public bool IsRunningOrPending
        {
            get
            {
                return ((Status == TaskStatus.Running) || (Status == TaskStatus.RunPending));
            }
        }
    }

    /// <summary>
    /// A wrapper for the List class that locks the list on any modification operations.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="System.Collections.Generic.ICollection{T}" />
    /// <seealso cref="System.Collections.Generic.IEnumerable{T}" />
    /// <seealso cref="System.Collections.IEnumerable" />
    /// <seealso cref="System.Collections.Generic.IList{T}" />
    /// <seealso cref="System.Collections.Generic.IReadOnlyCollection{T}" />
    /// <seealso cref="System.Collections.Generic.IReadOnlyList{T}" />
    [Serializable]
    public class LockingList<T> : ICollection<T>, IEnumerable<T>, IEnumerable, IList<T>, IReadOnlyCollection<T>, IReadOnlyList<T>
    {
        [XmlElement]
        private List<T> _list = new List<T>();

        [XmlIgnore]
        private object _lock = new object();

        /// <summary>
        /// Gets or sets the item T at the specified index.
        /// </summary>
        /// <value>
        /// The item T.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public T this[int index] { get => _list[index]; set => this.Insert(index, value); }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="System.Collections.ICollection"></see>.
        /// </summary>
        public int Count => _list.Count;

        /// <summary>
        /// Gets a value indicating whether the <see cref="System.Collections.ICollection"></see> is read-only.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Gets a value indicating whether this instance is fixed size.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is fixed size; otherwise, <c>false</c>.
        /// </value>
        public bool IsFixedSize => false;

        /// <summary>
        /// Gets a value indicating whether this instance is synchronized.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is synchronized; otherwise, <c>false</c>.
        /// </value>
        public bool IsSynchronized => false;

        /// <summary>
        /// Gets the synchronize root.
        /// </summary>
        /// <value>
        /// The synchronize root.
        /// </value>
        public object SyncRoot => _lock;

        /// <summary>
        /// Adds an item to the <see cref="System.Collections.ICollection"></see>.
        /// </summary>
        /// <param name="item">The object to add to the <see cref="System.Collections.ICollection"></see>.</param>
        public void Add(T item)
        {
            lock (_lock)
            {
                _list.Add(item);
            }
        }

        /// <summary>
        /// Removes all items from the <see cref="System.Collections.ICollection"></see>.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _list.Clear();
            }
        }

        /// <summary>
        /// Determines whether this instance contains the object.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="System.Collections.ICollection"></see>.</param>
        /// <returns>
        /// true if <paramref name="item">item</paramref> is found in the <see cref="System.Collections.ICollection"></see>; otherwise, false.
        /// </returns>
        public bool Contains(T item)
        {
            return _list.Contains(item);
        }

        /// <summary>
        /// Copies the elements of the <see cref="System.Collections.ICollection"></see> to an <see cref="System.Array"></see>, starting at a particular <see cref="System.Array"></see> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="System.Array"></see> that is the destination of the elements copied from <see cref="System.Collections.ICollection"></see>. The <see cref="System.Array"></see> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        /// <summary>
        /// Determines the index of a specific item in the <see cref="System.Collections.IList"></see>.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="System.Collections.IList"></see>.</param>
        /// <returns>
        /// The index of <paramref name="item">item</paramref> if found in the list; otherwise, -1.
        /// </returns>
        public int IndexOf(T item)
        {
            return _list.IndexOf(item);
        }

        /// <summary>
        /// Inserts an item to the <see cref="System.Collections.IList"></see> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which item should be inserted.</param>
        /// <param name="item">The object to insert into the <see cref="System.Collections.IList"></see>.</param>
        public void Insert(int index, T item)
        {
            lock (_lock)
            {
                _list.Insert(index, item);
            }
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="System.Collections.ICollection"></see>.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="System.Collections.ICollection"></see>.</param>
        /// <returns>
        /// true if <paramref name="item">item</paramref> was successfully removed from the <see cref="System.Collections.ICollection"></see>; otherwise, false. This method also returns false if <paramref name="item">item</paramref> is not found in the original <see cref="System.Collections.ICollection"></see>.
        /// </returns>
        public bool Remove(T item)
        {
            lock (_lock)
            {
                return _list.Remove(item);
            }
        }

        /// <summary>
        /// Removes the <see cref="System.Collections.IList"></see> item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        public void RemoveAt(int index)
        {
            lock (_lock)
            {
                _list.RemoveAt(index);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        /// <summary>
        ///  Creates a shallow copy of a range of elements in the source System.Collections.Generic.List`1.
        /// </summary>
        /// <param name="index">The zero-based System.Collections.Generic.List`1 index at which the range starts.</param>
        /// <param name="count">The number of elements in the range.</param>
        /// <returns></returns>
        public List<T> GetRange(int index, int count)
        {
            return _list.GetRange(index, count);
        }

        /// <summary>
        /// Adds the elements of the specified collection to the end of the System.Collections.Generic.List`1.
        /// </summary>
        /// <param name="collection">The collection whose elements should be added to the end of the System.Collections.Generic.List`1. 
        /// The collection itself cannot be null, but it can contain elements that are null, if type T is a reference type.</param>
        public void AddRange(IEnumerable<T> collection)
        {
            lock (_lock)
            {
                _list.AddRange(collection);
            }
        }
    }

    /// <summary>
    /// Abstract class to implement INotifyPropertyChanged
    /// </summary>
    /// <exclude/>
    public abstract class NotifyPropertyChangedBase : INotifyPropertyChanged
    {
        /// <summary>
        /// Event when a Property changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Call when a Property changes.
        /// </summary>
        /// <param name="propertyName">Name of the Property that changed.</param>
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
