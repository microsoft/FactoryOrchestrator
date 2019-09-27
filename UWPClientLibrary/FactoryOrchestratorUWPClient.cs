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
    /// Helper methods for file transfers.
    /// </summary>
    public class FactoryOrchestratorUWPClient : FactoryOrchestratorClient
    {
        public FactoryOrchestratorUWPClient(IPAddress host, int port) : base(host, port)
        { }

        protected override async Task<byte[]> ReadFileAsync(string file)
        {
            var buffer = await PathIO.ReadBufferAsync(file);
            return buffer.ToArray();
        }

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
