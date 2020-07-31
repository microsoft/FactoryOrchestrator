// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Newtonsoft.Json;
using System;

namespace Microsoft.FactoryOrchestrator.Core
{
    /// <summary>
    /// A generic Factory Orchestrator exception.
    /// </summary>
    [JsonObject(MemberSerialization.Fields)]
    public class FactoryOrchestratorException : Exception
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="guid">The GUID this Exception relates to.</param>
        /// <param name="innerException">Inner Exception(s)</param>
        public FactoryOrchestratorException(string message = null, Guid? guid = null, Exception innerException = null) : base(message, innerException)
        {
            Guid = guid;
        }

        /// <summary>
        /// The GUID this Exception relates to. NULL if it is not related to a specific object.
        /// </summary>
        public Guid? Guid { get; }
    }

    /// <summary>
    /// An exception denoting a running TaskList is preventing the operation from succeeding.
    /// </summary>
    [JsonObject(MemberSerialization.Fields)]
    public class FactoryOrchestratorTaskListRunningException : FactoryOrchestratorException
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public FactoryOrchestratorTaskListRunningException() : base(Resources.FactoryOrchestratorTaskListRunningException)
        { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="guid">The TaskList GUID.</param>
        public FactoryOrchestratorTaskListRunningException(Guid guid) : base(string.Format(Resources.FactoryOrchestratorTaskListRunningExceptionWithGuid, guid.ToString()), guid)
        { }
    }

    /// <summary>
    /// An exception denoting the given GUID is not recognized by Factory Orchestrator.
    /// </summary>
    [JsonObject(MemberSerialization.Fields)]
    public class FactoryOrchestratorUnkownGuidException : FactoryOrchestratorException
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public FactoryOrchestratorUnkownGuidException() : base(Resources.FactoryOrchestratorUnkownGuidException)
        { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="guid">The unkonwn GUID.</param>
        public FactoryOrchestratorUnkownGuidException(Guid guid) : base(string.Format(Resources.FactoryOrchestratorUnkownGuidExceptionWithGuid, guid.ToString()), guid)
        { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="guid">The unkonwn GUID.</param>
        /// <param name="type">The type of the GUID.</param>
        public FactoryOrchestratorUnkownGuidException(Guid guid, Type type) : base(string.Format(Resources.FactoryOrchestratorUnkownGuidExceptionWithGuidAndType, guid.ToString(), type.ToString()), guid)
        { }
    }

    /// <summary>
    /// An exception denoting an issue with the container on the device.
    /// </summary>
    [JsonObject(MemberSerialization.Fields)]
    public class FactoryOrchestratorContainerException : FactoryOrchestratorException
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="guid">The GUID this Exception relates to.</param>
        /// <param name="innerException">Inner Exception(s)</param>
        public FactoryOrchestratorContainerException(string message = null, Guid? guid = null, Exception innerException = null) : base(message, guid, innerException)
        { }
    }
}
