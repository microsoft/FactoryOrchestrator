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
        public FactoryOrchestratorException(string message = null, Guid? guid = null, Exception innerException = null) : base(message, innerException)
        {
            Guid = guid;
        }

        public Guid? Guid { get; }
    }

    /// <summary>
    /// An exception denoting a running TaskList is preventing the operation from succeeding.
    /// </summary>
    [JsonObject(MemberSerialization.Fields)]
    public class FactoryOrchestratorTaskListRunningException : FactoryOrchestratorException
    {
        public FactoryOrchestratorTaskListRunningException() : base("Cannot perform operation because one or more TaskLists are actively running!")
        { }

        public FactoryOrchestratorTaskListRunningException(Guid guid) : base($"Cannot perform operation because TaskList {guid} is actively running!", guid)
        { }
    }

    /// <summary>
    /// An exception denoting the given GUID is not recognized by Factory Orchestrator.
    /// </summary>
    [JsonObject(MemberSerialization.Fields)]
    public class FactoryOrchestratorUnkownGuidException : FactoryOrchestratorException
    {
        public FactoryOrchestratorUnkownGuidException() : base($"Guid is not valid!")
        { }

        public FactoryOrchestratorUnkownGuidException(Guid guid) : base($"{guid} is not valid!", guid)
        { }

        public FactoryOrchestratorUnkownGuidException(Guid guid, Type type) : base($"{guid} is not a valid {type.Name}!", guid)
        { }
    }
}
