// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Newtonsoft.Json;
using System;
using System.Globalization;

namespace Microsoft.FactoryOrchestrator.Core
{
    /// <summary>
    /// A generic Factory Orchestrator exception.
    /// </summary>
    [JsonObject(MemberSerialization.Fields)]
    public class FactoryOrchestratorException : Exception
    {  
        /// <summary>
        /// Initializes a new instance of the <see cref="FactoryOrchestratorException"/> class.
        /// </summary>
        public FactoryOrchestratorException() : base() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="FactoryOrchestratorException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public FactoryOrchestratorException(string message) : base(message) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="FactoryOrchestratorException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public FactoryOrchestratorException(string message, Exception innerException) : base(message, innerException) { }

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
        /// Initializes a new instance of the <see cref="FactoryOrchestratorTaskListRunningException"/> class.
        /// </summary>
        public FactoryOrchestratorTaskListRunningException() : base(Resources.FactoryOrchestratorTaskListRunningException) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="FactoryOrchestratorTaskListRunningException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public FactoryOrchestratorTaskListRunningException(string message) : base(message) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="FactoryOrchestratorTaskListRunningException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public FactoryOrchestratorTaskListRunningException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FactoryOrchestratorTaskListRunningException"/> class.
        /// </summary>
        /// <param name="guid">The TaskList GUID.</param>
        public FactoryOrchestratorTaskListRunningException(Guid guid) : base(string.Format(CultureInfo.CurrentCulture, Resources.FactoryOrchestratorTaskListRunningExceptionWithGuid, guid.ToString()), guid)
        { }
    }

    /// <summary>
    /// An exception denoting the given GUID is not recognized by Factory Orchestrator.
    /// </summary>
    [JsonObject(MemberSerialization.Fields)]
    public class FactoryOrchestratorUnkownGuidException : FactoryOrchestratorException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FactoryOrchestratorUnkownGuidException"/> class.
        /// </summary>
        public FactoryOrchestratorUnkownGuidException() : base(Resources.FactoryOrchestratorUnkownGuidException) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="FactoryOrchestratorUnkownGuidException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public FactoryOrchestratorUnkownGuidException(string message) : base(message) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="FactoryOrchestratorUnkownGuidException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public FactoryOrchestratorUnkownGuidException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FactoryOrchestratorUnkownGuidException"/> class.
        /// </summary>
        /// <param name="guid">The unkonwn GUID.</param>
        public FactoryOrchestratorUnkownGuidException(Guid guid) : base(string.Format(CultureInfo.CurrentCulture, Resources.FactoryOrchestratorUnkownGuidExceptionWithGuid, guid.ToString()), guid)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FactoryOrchestratorUnkownGuidException"/> class.
        /// </summary>
        /// <param name="guid">The unkonwn GUID.</param>
        /// <param name="type">The type of the GUID.</param>
        public FactoryOrchestratorUnkownGuidException(Guid guid, Type type) : base(string.Format(CultureInfo.CurrentCulture, Resources.FactoryOrchestratorUnkownGuidExceptionWithGuidAndType, guid.ToString(), type?.ToString()), guid)
        { }
    }

    /// <summary>
    /// An exception denoting an issue with the container on the device.
    /// </summary>
    [JsonObject(MemberSerialization.Fields)]
    public class FactoryOrchestratorContainerException : FactoryOrchestratorException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FactoryOrchestratorContainerException"/> class.
        /// </summary>
        public FactoryOrchestratorContainerException() : base() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="FactoryOrchestratorContainerException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public FactoryOrchestratorContainerException(string message) : base(message) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="FactoryOrchestratorContainerException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public FactoryOrchestratorContainerException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FactoryOrchestratorContainerException"/> class.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="guid">The GUID this Exception relates to.</param>
        /// <param name="innerException">Inner Exception(s)</param>
        public FactoryOrchestratorContainerException(string message = null, Guid? guid = null, Exception innerException = null) : base(message, guid, innerException)
        { }
    }
}
