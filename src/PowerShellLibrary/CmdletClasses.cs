// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Management.Automation;
using Microsoft.FactoryOrchestrator.Core;
using System.Net;
using Newtonsoft.Json.Serialization;

namespace Microsoft.FactoryOrchestrator.Client
{
    /// <summary>
    /// Type of GUID passed to FactoryOrchestratorServerPoller
    /// </summary>
    public enum ServerPollerGuidType
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        Task,
        TaskList,
        TaskRun
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }

    /// <summary>
    /// Cmdlet class. Intended for PowerShell use only.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "FactoryOrchestratorClient")]
    [OutputType(typeof(FactoryOrchestratorClient))]
    public class FactoryOrchestratorClientCmdlet : Cmdlet
    {
        /// <summary>
        /// IP Address to connect to.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        public IPAddress IpAddress { get; set; }

        /// <summary>
        /// TCP port to use for connection.
        /// </summary>
        [Parameter(Mandatory = false)]
        public int Port { get; set; }

        /// <summary>
        /// CTOR.
        /// </summary>
        public FactoryOrchestratorClientCmdlet()
        {
            Port = 45684;
        }

        /// <summary>
        /// Creates a PowerShell object.
        /// </summary>
        protected override void ProcessRecord()
        {
            this.WriteObject(new FactoryOrchestratorClientSync(IpAddress, Port));
        }
    }

    /// <summary>
    /// Cmdlet class. Intended for PowerShell use only.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "FactoryOrchestratorTaskList")]
    [OutputType(typeof(TaskList))]
    public class FactoryOrchestratorTaskListCmdlet : Cmdlet
    {
        /// <summary>
        /// Name of the TaskList.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        public string Name { get; set; }

        /// <summary>
        /// Guid of the TaskList.
        /// </summary>
        [Parameter(Mandatory = false)]
        public Guid Guid { get; set; }

        /// <summary>
        /// CTOR.
        /// </summary>
        public FactoryOrchestratorTaskListCmdlet()
        {
            Guid = Guid.NewGuid();
        }

        /// <summary>
        /// Creates a PowerShell object.
        /// </summary>
        protected override void ProcessRecord()
        {
            this.WriteObject(new TaskList(Name, Guid));
        }
    }

    /// <summary>
    /// Cmdlet class. Intended for PowerShell use only.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "FactoryOrchestratorTask")]
    [OutputType(typeof(TaskList))]
    public class FactoryOrchestratorTaskCmdlet : Cmdlet
    {
        /// <summary>
        /// Path of the Task.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName ="Path")]
        public string Path { get; set; }

        /// <summary>
        /// Type of the Task.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        public TaskType Type { get; set; }

        /// <summary>
        /// Friendly name for the Task.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "Name")]
        [Parameter(Mandatory = false, ParameterSetName = "Path")]
        public string Name { get; set; }

        /// <summary>
        /// Arguments for the Task.
        /// </summary>
        [Parameter(Mandatory = false)]
        public string Arguments { get; set; }

        /// <summary>
        /// Create a BatchFileTask instead of a CommandLineTask. Only needed for backwards compatibility with FactoryOrchestrator version 9.0.0 or order.
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter ForceBatchFileTaskType
        {
            get
            {
                return forceBatch;
            }
            set
            {
                forceBatch = value;
            }
        }
        private bool forceBatch;

        /// <summary>
        /// Creates a PowerShell object.
        /// </summary>
        protected override void ProcessRecord()
        {
            TaskBase t;
            switch (Type)
            {
                case TaskType.BatchFile:
                    if (ForceBatchFileTaskType)
                    {
#pragma warning disable CS0618 // Type or member is obsolete
                        t = new BatchFileTask(Path);
#pragma warning restore CS0618 // Type or member is obsolete
                    }
                    else
                    {
                        t = new CommandLineTask(Path);
                    }
                    t.Arguments = Arguments;
                    t.Name = Name;
                    break;
                case TaskType.Executable:
                    t = new ExecutableTask(Path);
                    t.Arguments = Arguments;
                    t.Name = Name;
                    break;
                case TaskType.External:
                    t = new ExternalTask(Name);
                    t.Arguments = Arguments;
                    break;
                case TaskType.PowerShell:
                    t = new PowerShellTask(Path);
                    t.Arguments = Arguments;
                    t.Name = Name;
                    break;
                case TaskType.TAEFDll:
                    t = new TAEFTest(Path);
                    t.Arguments = Arguments;
                    t.Name = Name;
                    break;
                case TaskType.UWP:
                    t = new UWPTask(Path);
                    t.Arguments = Arguments;
                    t.Name = Name;
                    break;
                default:
                    throw new FactoryOrchestratorException(Resources.InvalidTaskRunTypeException);
            }

            this.WriteObject(t);
        }
    }

    /// <summary>
    /// Cmdlet class. Intended for PowerShell use only.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "FactoryOrchestratorServerPoller")]
    [OutputType(typeof(FactoryOrchestratorClient))]
    public class PowerShellServerPoller : Cmdlet
    {
        /// <summary>
        /// GUID of the object you want to poll. $null is allowed.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        public Guid? GuidToPoll { get; set; }

        /// <summary>
        /// The type of object that GuidToPoll is.
        /// </summary>
        [Parameter(Mandatory = true, Position = 1)]
        public ServerPollerGuidType GuidType { get; set; }

        /// <summary>
        /// How frequently the polling should be done, in milliseconds. Defaults to 500ms.
        /// </summary>
        [Parameter(Mandatory = false, Position = 2)]
        public int PollingIntervalMs { get; set; }

        /// <summary>
        /// If true, automatically adjust the polling interval for best performance. Defaults to true.
        /// </summary>
        [Parameter(Mandatory = false, Position = 3)]
        public bool AdaptiveInterval { get; set; }

        /// <summary>
        /// If adaptiveInterval is set, this defines the maximum multiplier/divisor that will be applied to the polling interval. For example, if maxAdaptiveModifier=2 and pollingIntervalMs=100, the object would be polled at a rate between 50ms to 200ms. 
        /// </summary>
        [Parameter(Mandatory = false, Position = 4)]
        public int MaxAdaptiveModifier { get; set; }

        /// <summary>
        /// CTOR.
        /// </summary>
        public PowerShellServerPoller()
        {
            AdaptiveInterval = false;
            PollingIntervalMs = 500;
            MaxAdaptiveModifier = 3;
        }

        /// <summary>
        /// Creates a PowerShell object.
        /// </summary>
        protected override void ProcessRecord()
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            switch (GuidType)
            {
                case ServerPollerGuidType.Task:
                    this.WriteObject(new ServerPoller(GuidToPoll, typeof(TaskBase), PollingIntervalMs, AdaptiveInterval, MaxAdaptiveModifier));
                    break;
                case ServerPollerGuidType.TaskList:
                    this.WriteObject(new ServerPoller(GuidToPoll, typeof(TaskList), PollingIntervalMs, AdaptiveInterval, MaxAdaptiveModifier));
                    break;
                case ServerPollerGuidType.TaskRun:
                    this.WriteObject(new ServerPoller(GuidToPoll, typeof(TaskRun), PollingIntervalMs, AdaptiveInterval, MaxAdaptiveModifier));
                    break;
                default:
                    throw new FactoryOrchestratorException(Resources.UnsupportedGuidType);
            }
#pragma warning restore CA2000 // Dispose objects before losing scope
        }
    }
}
