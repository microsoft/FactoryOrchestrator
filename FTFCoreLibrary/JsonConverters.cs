using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.FactoryTestFramework.Core.JSONConverters
{
    /// <summary>
    /// TestBaseConverter serializes "instances" of the abstract class TestBase.
    /// </summary>
    public class TestBaseConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(TestBase));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            switch ((TestType)(jo["TestType"].Value<int>()))
            {
                case TestType.ConsoleExe:
                    return JsonConvert.DeserializeObject<ExecutableTest>(jo.ToString());
                case TestType.TAEFDll:
                    return JsonConvert.DeserializeObject<TAEFTest>(jo.ToString());
                case TestType.UWP:
                    return JsonConvert.DeserializeObject<UWPTest>(jo.ToString());
                default:
                    throw new Exception("Trying to deserialize an unknown test type!");
            }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var test = (TestBase)value;
            switch (test.TestType)
            {
                case TestType.ConsoleExe:
                    serializer.Serialize(writer, value, typeof(ExecutableTest));
                    break;
                case TestType.TAEFDll:
                    serializer.Serialize(writer, value, typeof(TAEFTest));
                    break;
                case TestType.UWP:
                    serializer.Serialize(writer, value, typeof(UWPTest));
                    break;
                default:
                    throw new Exception("Trying to serialize an unknown test type!");
            }
        }
    }

    /// <summary>
    /// NoConverter class is used children of abstract classes (ex: ExecutableTest), to prevent infinite loop.
    /// All serialization is done by the Abstract class converter (TestBaseConverter)
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