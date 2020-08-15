// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.FactoryOrchestrator.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

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
        /// Overrite CreateClientFileReader with UWP equivalent.
        /// </summary>
        /// <param name="clientFilename">The client filename.</param>
        /// <returns></returns>
        protected override Stream CreateClientFileReader(string clientFilename)
        {
            var file = StorageFile.GetFileFromPathAsync(clientFilename).AsTask().Result;
            return file.OpenStreamForReadAsync().Result;
        }

        /// <summary>
        /// Overrite CreateClientFileWriter with UWP equivalent. File is overwitten if it exists.
        /// </summary>
        /// <param name="clientFilename">The client filename.</param>
        /// <returns></returns>
        protected override Stream CreateClientFileWriter(string clientFilename)
        {
            var folderPath = Path.GetDirectoryName(clientFilename);
            var filename = Path.GetFileName(clientFilename);
            var targetFolder = StorageFolder.GetFolderFromPathAsync(folderPath).AsTask().Result;
            StorageFile targetFile = targetFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting).AsTask().Result;
            return targetFile.OpenStreamForWriteAsync().Result;
        }

        /// <summary>
        /// Enumerates local directories in a given folder.
        /// </summary>
        /// <param name="path">The directory to eumerate.</param>
        /// <returns></returns>
        protected override IEnumerable<string> EnumerateLocalDirectories(string path)
        {
            var folder = StorageFolder.GetFolderFromPathAsync(path).AsTask().Result;
            return folder.GetFoldersAsync().AsTask().Result.Select(x => x.Path);
        }

        /// <summary>
        /// Enumerates local files in a given folder.
        /// </summary>
        /// <param name="path">The directory to eumerate.</param>
        /// <returns></returns>
        protected override IEnumerable<string> EnumerateLocalFiles(string path)
        {
            var folder = StorageFolder.GetFolderFromPathAsync(path).AsTask().Result;
            return folder.GetFilesAsync().AsTask().Result.Select(x => x.Path);
        }

        /// <summary>
        /// Tries to delete a local file.
        /// </summary>
        /// <param name="clientFilename">The file to delete.</param>
        /// <returns></returns>
        protected override bool TryDeleteLocalFile(string clientFilename)
        {
            try
            {
                var file = StorageFile.GetFileFromPathAsync(clientFilename).AsTask().Result;
                file.DeleteAsync().AsTask().Wait();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Overrite CreateDirectory API with UWP equivalent.
        /// </summary>
        /// <param name="path">Directory to create.</param>
        protected override void CreateDirectory(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            StorageFolder parent;
            var parentPath = Path.GetDirectoryName(path);

            if (parentPath != null)
            {
                try
                {
                    parent = StorageFolder.GetFolderFromPathAsync(parentPath).AsTask().Result;
                    path = path.TrimEnd(Path.DirectorySeparatorChar);
                    path = path.Substring(path.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                    parent.CreateFolderAsync(path, CreationCollisionOption.OpenIfExists).AsTask().Wait();
                }
                catch (FileNotFoundException)
                {
                    CreateDirectory(parentPath);
                }
            }
        }
    }
}
