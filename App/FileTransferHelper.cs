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
    /// Helper methods for file transfers. Expects IPCClientHelper is connected.
    /// </summary>
    class FileTransferHelper
    {
        public static async Task<bool> SendFileToServer(string clientFilename, string serverFilename)
        {
            try
            {
                var clientFile = await StorageFile.GetFileFromPathAsync(clientFilename);
                if (clientFile != null)
                {
                    var buffer = await PathIO.ReadBufferAsync(clientFilename);
                    return await IPCClientHelper.IpcClient.InvokeAsync(x => x.SendFile(serverFilename, buffer.ToArray()));
                }
            }
            catch (Exception)
            {

            }

            return false;
        }

        public static async Task<bool> GetFileFromServer(string serverFilename, string clientFilename)
        {
            try
            {
                var folderPath = Path.GetDirectoryName(clientFilename);
                var filename = Path.GetFileName(clientFilename);
                var targetFolder = await StorageFolder.GetFolderFromPathAsync(folderPath);
                StorageFile targetFile = await targetFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);

                var bytes = await IPCClientHelper.IpcClient.InvokeAsync(x => x.GetFile(serverFilename));
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
