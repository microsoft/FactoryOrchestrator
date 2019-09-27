using JKang.IpcServiceFramework;
using Microsoft.FactoryOrchestrator.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using TaskStatus = Microsoft.FactoryOrchestrator.Core.TaskStatus;

namespace Microsoft.FactoryOrchestrator.Client
{

    /// <summary>
    /// A helper class for Factory Orchestrator .NET clients. It wraps the inter-process calls in a more usable manner.
    /// WARNING: Use FactoryOrchestratorUWPClient for UWP clients or your UWP app will crash!
    /// </summary>
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

        public async Task SendFileToServer(string clientFilename, string serverFilename)
        {
            if (!File.Exists(clientFilename))
            {
                throw new FileNotFoundException($"{clientFilename} does not exist!");
            }

            var bytes = await ReadFileAsync(clientFilename);
            await _IpcClient.InvokeAsync(x => x.SendFile(serverFilename, bytes));
        }

        public async Task GetFileFromServer(string serverFilename, string clientFilename)
        {
            // Create target folder, if needed.
            Directory.CreateDirectory(Path.GetDirectoryName(clientFilename));

            var bytes = await _IpcClient.InvokeAsync(x => x.GetFile(serverFilename));
            await WriteFileAsync(clientFilename, bytes);
        }

        public async Task CopyDirectoryFromServer(string serverDirectory, string clientDirectory)
        {
            var files = await _IpcClient.InvokeAsync(x => x.EnumerateFiles(serverDirectory, false));
            var dirs = await _IpcClient.InvokeAsync(x => x.EnumerateDirectories(serverDirectory, false));

            if (!Directory.Exists(clientDirectory))
            {
                Directory.CreateDirectory(clientDirectory);
            }

            foreach (var file in files)
            {
                var filename = Path.GetFileName(file);
                await GetFileFromServer(file, Path.Combine(clientDirectory, filename));
            }
            foreach (var dir in dirs)
            {
                var subDirName = new DirectoryInfo(dir).Name;
                var clientsubDir = Path.Combine(clientDirectory, subDirName);

                if (!Directory.Exists(clientsubDir))
                {
                    Directory.CreateDirectory(clientsubDir);
                }

                await CopyDirectoryFromServer(dir, clientsubDir);
            }
        }

        public async Task CopyDirectoryToServer(string clientDirectory, string serverDirectory)
        {
            if (!Directory.Exists(clientDirectory))
            {
                throw new DirectoryNotFoundException($"{clientDirectory} does not exist!");
            }

            var files = Directory.EnumerateFiles(clientDirectory);
            var dirs = Directory.EnumerateDirectories(clientDirectory);

            foreach (var file in files)
            {
                await SendFileToServer(file, Path.Combine(serverDirectory, Path.GetFileName(file)));
            }

            foreach (var dir in dirs)
            {
                await CopyDirectoryToServer(dir, Path.Combine(serverDirectory, new DirectoryInfo(dir).Name));
            }
        }

        public async void ShutdownServerDevice(uint secondsUntilShutdown = 0)
        {
            await RunExecutable(@"%systemroot%\system32\shutdown.exe", $"/s /t {secondsUntilShutdown}", null);
        }

        public async void RebootServerDevice(uint secondsUntilReboot = 0)
        {
            await RunExecutable(@"%systemroot%\system32\shutdown.exe", $"/r /t {secondsUntilReboot}", null);
        }

        protected virtual async Task WriteFileAsync(string file, byte[] data)
        {
            File.WriteAllBytes(file, data);
        }

        protected virtual async Task<byte[]> ReadFileAsync(string file)
        {
            return File.ReadAllBytes(file);
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
