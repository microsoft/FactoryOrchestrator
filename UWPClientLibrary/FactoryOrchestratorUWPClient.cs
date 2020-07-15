// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.FactoryOrchestrator.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Microsoft.FactoryOrchestrator.UWP
{
    /// <summary>
    /// A helper class for Factory Orchestrator UWP clients. It wraps the inter-process calls in a more usable manner.
    /// WARNING: Use FactoryOrchestratorClient for .NET clients or your program will crash!
    /// </summary>
    public class FactoryOrchestratorUWPClient : FactoryOrchestratorClient
    {
        /// <summary>
        /// Creates a new FactoryOrchestratorClient instance. WARNING: Use FactoryOrchestratorUWPClient for UWP clients or your UWP app will crash!
        /// </summary>
        /// <param name="host">IP address of the device running Factory Orchestrator Service. Use IPAddress.Loopback for local device.</param>
        /// <param name="port">Port to use. Factory Orchestrator Service defaults to 45684.</param>
        public FactoryOrchestratorUWPClient(IPAddress host, int port = 45684) : base(host, port)
        { }

        /// <summary>
        /// Overrite read API with UWP equivalent.
        /// </summary>
        protected override async Task<byte[]> ReadFileAsync(string file)
        {
            var buffer = await PathIO.ReadBufferAsync(file);
            return buffer.ToArray();
        }


        /// <summary>
        /// Overrite write API with UWP equivalent.
        /// </summary>
        protected override async Task WriteFileAsync(string file, byte[] data)
        {
            var folderPath = Path.GetDirectoryName(file);
            var filename = Path.GetFileName(file);
            var targetFolder = await StorageFolder.GetFolderFromPathAsync(folderPath);
            StorageFile targetFile = await targetFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteBytesAsync(targetFile, data);
        }

    }
}
