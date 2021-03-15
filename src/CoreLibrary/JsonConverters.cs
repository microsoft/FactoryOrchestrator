﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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
    /// <exclude/>
    public class TaskBaseConverter : JsonConverter
    {
        /// <summary>Determines whether this instance can convert the specified object type.</summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        ///   <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.</returns>
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(TaskBase));
        }

        /// <summary>Reads the JSON representation of the object.</summary>
        /// <param name="reader">The <see cref="Newtonsoft.Json.JsonReader"/> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        /// <exception cref="FactoryOrchestratorException">Trying to deserialize an unknown task type!</exception>
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
                case TaskType.CommandLine:
                    {
                        // Use the object type the serializer used to ensure back-compatibiilty
#pragma warning disable CA1062 // Validate arguments of public methods
                        if (objectType.Equals(typeof(CommandLineTask)))
#pragma warning restore CA1062 // Validate arguments of public methods
                        {
                            return JsonConvert.DeserializeObject<CommandLineTask>(jo.ToString());
                        }
                        else
                        {
#pragma warning disable CS0618 // Type or member is obsolete
                            return JsonConvert.DeserializeObject<BatchFileTask>(jo.ToString());
#pragma warning restore CS0618 // Type or member is obsolete
                        }
                    }
                default:
                    throw new FactoryOrchestratorException(Resources.TaskBaseDeserializationException);
            }
        }

        /// <summary>Gets a value indicating whether this <see cref="Newtonsoft.Json.JsonConverter"/> can write JSON.</summary>
        /// <value>
        ///   <c>true</c> if this <see cref="Newtonsoft.Json.JsonConverter"/> can write JSON; otherwise, <c>false</c>.</value>
        public override bool CanWrite
        {
            get { return true; }
        }

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="Newtonsoft.Json.JsonWriter" /> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <exception cref="FactoryOrchestratorException">Trying to serialize an unknown task type</exception>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

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
                case TaskType.CommandLine:
                    // Use the exact object type to ensure back-compatibiilty
                    if (task.GetType().Equals(typeof(CommandLineTask)))
                    {
                        serializer.Serialize(writer, value, typeof(CommandLineTask));
                    }
                    else
                    {
#pragma warning disable CS0618 // Type or member is obsolete
                        serializer.Serialize(writer, value, typeof(BatchFileTask));
#pragma warning restore CS0618 // Type or member is obsolete
                    }
                    break;
                default:
                    throw new FactoryOrchestratorException(Resources.TaskBaseSerializationException);
            }
        }
    }

    /// <summary>
    /// NoConverter class is used children of abstract classes (ex: ExecutableTask), to prevent infinite loop.
    /// All serialization is done by the Abstract class converter (TaskBaseConverter)
    /// </summary>
    /// <exclude/>
    [JsonConverter(typeof(NoConverter))]
    public class NoConverter : JsonConverter
    {
        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType)
        {
            return false;
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Newtonsoft.Json.JsonConverter" /> can read JSON.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this <see cref="Newtonsoft.Json.JsonConverter" /> can read JSON; otherwise, <c>false</c>.
        /// </value>
        public override bool CanRead { get { return false; } }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="Newtonsoft.Json.JsonReader" /> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>
        /// The object value.
        /// </returns>
        /// <exception cref="NotImplementedException"></exception>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Newtonsoft.Json.JsonConverter" /> can write JSON.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this <see cref="Newtonsoft.Json.JsonConverter" /> can write JSON; otherwise, <c>false</c>.
        /// </value>
        public override bool CanWrite { get { return false; } }

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="Newtonsoft.Json.JsonWriter" /> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <exception cref="NotImplementedException"></exception>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
