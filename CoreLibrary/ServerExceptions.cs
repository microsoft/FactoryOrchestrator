using Newtonsoft.Json;
using System;

namespace Microsoft.FactoryOrchestrator.Core
{
    [JsonObject(MemberSerialization.Fields)]
    public class FactoryOrchestratorException : Exception
    {
        public FactoryOrchestratorException(string message = null, Guid? guid = null, Exception innerException = null) : base(message, innerException)
        {
            Guid = guid;
        }

        public Guid? Guid { get; }
    }

    [JsonObject(MemberSerialization.Fields)]
    public class FactoryOrchestratorTaskListRunningException : FactoryOrchestratorException
    {
        public FactoryOrchestratorTaskListRunningException() : base("Cannot perform operation because one or more TaskLists are actively running!")
        { }

        public FactoryOrchestratorTaskListRunningException(Guid guid) : base($"Cannot perform operation because TaskList {guid} is actively running!", guid)
        { }
    }

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
