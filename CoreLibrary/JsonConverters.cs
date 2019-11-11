using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.FactoryOrchestrator.Core.JSONConverters
{
    /// <summary>
    /// TaskBaseConverter serializes "instances" of the abstract class TaskBase.
    /// </summary>
    public class TaskBaseConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(TaskBase));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            switch ((TaskType)(jo["Type"].Value<int>()))
            {
                case TaskType.ConsoleExe:
                    return JsonConvert.DeserializeObject<ExecutableTask>(jo.ToString());
                case TaskType.TAEFDll:
                    return JsonConvert.DeserializeObject<TAEFTest>(jo.ToString());
                case TaskType.External:
                    return JsonConvert.DeserializeObject<ExternalTask>(jo.ToString());
                case TaskType.UWP:
                    return JsonConvert.DeserializeObject<UWPTask>(jo.ToString());
                case TaskType.PowerShell:
                    return JsonConvert.DeserializeObject<PowerShellTask>(jo.ToString());
                case TaskType.BatchFile:
                    return JsonConvert.DeserializeObject<BatchFileTask>(jo.ToString());
                default:
                    throw new FactoryOrchestratorException("Trying to deserialize an unknown task type!");
            }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var task = (TaskBase)value;
            switch (task.Type)
            {
                case TaskType.ConsoleExe:
                    serializer.Serialize(writer, value, typeof(ExecutableTask));
                    break;
                case TaskType.TAEFDll:
                    serializer.Serialize(writer, value, typeof(TAEFTest));
                    break;
                case TaskType.External:
                    serializer.Serialize(writer, value, typeof(ExternalTask));
                    break;
                case TaskType.UWP:
                    serializer.Serialize(writer, value, typeof(UWPTask));
                    break;
                case TaskType.PowerShell:
                    serializer.Serialize(writer, value, typeof(PowerShellTask));
                    break;
                case TaskType.BatchFile:
                    serializer.Serialize(writer, value, typeof(BatchFileTask));
                    break;
                default:
                    throw new FactoryOrchestratorException("Trying to serialize an unknown task type!");
            }
        }
    }

    /// <summary>
    /// NoConverter class is used children of abstract classes (ex: ExecutableTask), to prevent infinite loop.
    /// All serialization is done by the Abstract class converter (TaskBaseConverter)
    /// </summary>
    [JsonConverter(typeof(NoConverter))]
    public class NoConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return false;
        }

        public override bool CanRead { get { return false; } }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanWrite { get { return false; } }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}