using Microsoft.FactoryOrchestrator.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Microsoft.FactoryOrchestrator.UWP
{
    /// <summary>
    /// Helper methods for file transfers.
    /// </summary>
    class FileTransferHelper
    {
        public static async Task<bool> SendFileToServer(FactoryOrchestratorClient client, string clientFilename, string serverFilename)
        {
            try
            {
                var clientFile = await StorageFile.GetFileFromPathAsync(clientFilename);
                if (clientFile != null)
                {
                    var buffer = await PathIO.ReadBufferAsync(clientFilename);
                    return await client.SendFile(serverFilename, buffer.ToArray());
                }
            }
            catch (Exception)
            {

            }

            return false;
        }

        public static async Task<bool> GetFileFromServer(FactoryOrchestratorClient client, string serverFilename, string clientFilename)
        {
            try
            {
                var folderPath = Path.GetDirectoryName(clientFilename);
                var filename = Path.GetFileName(clientFilename);
                var targetFolder = await StorageFolder.GetFolderFromPathAsync(folderPath);
                StorageFile targetFile = await targetFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);

                var bytes = await client.GetFile(serverFilename);
                await FileIO.WriteBytesAsync(targetFile, bytes);

                return true;
            }
            catch (Exception)
            {

            }

            return false;
        }
    }
}
