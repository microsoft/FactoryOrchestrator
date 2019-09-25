using JKang.IpcServiceFramework;
using Microsoft.FactoryOrchestrator.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using TaskStatus = Microsoft.FactoryOrchestrator.Core.TaskStatus;

namespace Microsoft.FactoryOrchestrator.Client
{
    public partial class FactoryOrchestratorClient
    {
        public FactoryOrchestratorClient(IPAddress host, int port)
        {
            OnConnected = null;
            _IpcClient = null;
            IsConnected = true;
            IpAddress = host;
            Port = port;
        }

        public async Task Connect()
        {
            _IpcClient = new IpcServiceClientBuilder<IFactoryOrchestratorService>()
                .UseTcp(IpAddress, Port)
                .Build();

            // Test a command to make sure connection works
            await _IpcClient.InvokeAsync(x => x.GetServiceVersionString());

            IsConnected = true;
            OnConnected?.Invoke();
        }

        public async Task<bool> TryConnect()
        {
            try
            {
                await Connect();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Warning: This helper API only works in .NET executables! It WILL NOT work in UWP apps, including FactoryOrchestratorApp.
        /// FactoryOrchestratorApp has its own file transfer API in FileTransferHelper.cs
        /// </summary>
        public async Task<bool> SendFileToServer(string clientFilename, string serverFilename)
        {
            if (!File.Exists(clientFilename))
            {
                // todo: logging
                return false;
            }

            return await _IpcClient.InvokeAsync(x => x.SendFile(serverFilename, File.ReadAllBytes(clientFilename)));
        }

        /// <summary>
        /// Warning: This helper API only works in .NET executables! It WILL NOT work in UWP apps, including FactoryOrchestratorApp.
        /// FactoryOrchestratorApp has its own file transfer API in FileTransferHelper.cs
        /// </summary>
        public async Task<bool> GetFileFromServer(string serverFilename, string clientFilename)
        {
            // Create target folder, if needed.
            Directory.CreateDirectory(Path.GetDirectoryName(clientFilename));

            var bytes = await _IpcClient.InvokeAsync(x => x.GetFile(serverFilename));
            File.WriteAllBytes(clientFilename, bytes);
            return true;
        }

        public async void ShutdownServerDevice(uint secondsUntilShutdown = 0)
        {
            await RunExecutable(@"%systemroot%\system32\shutdown.exe", $"/s /t {secondsUntilShutdown}", null);
        }

        public async void RebootServerDevice(uint secondsUntilReboot = 0)
        {
            await RunExecutable(@"%systemroot%\system32\shutdown.exe", $"/r /t {secondsUntilReboot}", null);
        }

        private IpcServiceClient<IFactoryOrchestratorService> _IpcClient;

        public event IPCClientOnConnected OnConnected;
        public bool IsLocalHost
        {
            get
            {
                return (IpAddress == IPAddress.Loopback) ? true : false;
            }
        }
        public bool IsConnected { get; private set; }
        public IPAddress IpAddress { get; private set; }
        public int Port { get; private set; }

        public delegate void IPCClientOnConnected();
    }
}
