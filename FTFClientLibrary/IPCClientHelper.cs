using JKang.IpcServiceFramework;
using Microsoft.FactoryTestFramework.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.FactoryTestFramework.Client
{
    public static class IPCClientHelper
    {
        static IPCClientHelper()
        {
            OnConnected = null;
            IpcClient = null;
        }

        public static async Task StartIPCConnection(IPAddress host, int port)
        {
            IsLocalHost = (host == IPAddress.Loopback) ? true : false;

            IpcClient = new IpcServiceClientBuilder<IFTFCommunication>()
                .UseTcp(host, port)
                .Build();

            // Test a command to make sure connection works
            await IpcClient.InvokeAsync(x => x.GetServiceVersionString());

            OnConnected?.Invoke();
        }

        /// <summary>
        /// Warning: This helper API only works in .NET executables! It WILL NOT work in UWP apps, including FTFUWP.
        /// FTFUWP has its own file transfer API in FileTransferHelper.cs
        /// </summary>
        public static async Task<bool> SendFileToServer(string clientFilename, string serverFilename)
        {
            if (!File.Exists(clientFilename))
            {
                // todo: logging
                return false;
            }

            return await IpcClient.InvokeAsync(x => x.SendFile(serverFilename, File.ReadAllBytes(clientFilename)));
        }

        /// <summary>
        /// Warning: This helper API only works in .NET executables! It WILL NOT work in UWP apps, including FTFUWP.
        /// FTFUWP has its own file transfer API in FileTransferHelper.cs
        /// </summary>
        public static async Task<bool> GetFileFromServer(string serverFilename, string clientFilename)
        {
            // Create target folder, if needed.
            Directory.CreateDirectory(Path.GetDirectoryName(clientFilename));

            var bytes = await IpcClient.InvokeAsync(x => x.GetFile(serverFilename));
            File.WriteAllBytes(clientFilename, bytes);

            return true;
        }

        public static IpcServiceClient<IFTFCommunication> IpcClient { get; private set; }
        public static event IPCClientOnConnected OnConnected;
        public static bool IsLocalHost { get; private set; }

        public delegate void IPCClientOnConnected();
    }
}
