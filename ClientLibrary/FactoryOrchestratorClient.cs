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
        /// <summary>
        /// Creates a new FactoryOrchestratorClient instance. WARNING: Use FactoryOrchestratorUWPClient for UWP clients or your UWP app will crash!
        /// </summary>
        /// <param name="host">IP address of the device running Factory Orchestrator Service. Use IPAddress.Loopback for local device.</param>
        /// <param name="port">Port to use. Factory Orchestrator Service defaults to 45684.</param>
        public FactoryOrchestratorClient(IPAddress host, int port = 45684)
        {
            OnConnected = null;
            _IpcClient = null;
            IsConnected = true;
            IpAddress = host;
            Port = port;
        }

        /// <summary>
        /// Establishes a connection to the Factory Orchestrator Service.
        /// Throws an exception if it cannot connect.
        /// </summary>
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

        /// <summary>
        /// Attempts to establish a connection to the Factory Orchestrator Service.
        /// </summary>
        /// <returns>true if it was able to connect.</returns>
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
        /// Copies a file from the client to the device running Factory Orchestrator Service. Creates directories if needed.
        /// </summary>
        /// <param name="clientFilename">Path on client PC to the file to copy.</param>
        /// <param name="serverFilename">Path on device running Factory Orchestrator Service where the file will be saved.</param>
        public async Task SendFileToDevice(string clientFilename, string serverFilename)
        {
            if (!File.Exists(clientFilename))
            {
                throw new FileNotFoundException($"{clientFilename} does not exist!");
            }

            var bytes = await ReadFileAsync(clientFilename);
            await _IpcClient.InvokeAsync(x => x.SendFile(serverFilename, bytes));
        }

        /// <summary>
        /// Copies a file from the device running Factory Orchestrator Service to the client. Creates directories if needed.
        /// </summary>
        /// <param name="serverFilename">Path on device running Factory Orchestrator Service to the file to copy.</param>
        /// <param name="clientFilename">Path on client PC where the file will be saved.</param>
        public async Task GetFileFromDevice(string serverFilename, string clientFilename)
        {
            // Create target folder, if needed.
            Directory.CreateDirectory(Path.GetDirectoryName(clientFilename));

            var bytes = await _IpcClient.InvokeAsync(x => x.GetFile(serverFilename));
            await WriteFileAsync(clientFilename, bytes);
        }

        /// <summary>
        /// Copies a folder from the device running Factory Orchestrator Service to the client. Creates directories if needed.
        /// </summary>
        /// <param name="serverDirectory">Path on device running Factory Orchestrator Service to the folder to copy.</param>
        /// <param name="clientDirectory">Path on client PC where the folder will be saved.</param>
        public async Task GetDirectoryFromDevice(string serverDirectory, string clientDirectory)
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
                await GetFileFromDevice(file, Path.Combine(clientDirectory, filename));
            }
            foreach (var dir in dirs)
            {
                var subDirName = new DirectoryInfo(dir).Name;
                var clientsubDir = Path.Combine(clientDirectory, subDirName);

                if (!Directory.Exists(clientsubDir))
                {
                    Directory.CreateDirectory(clientsubDir);
                }

                await GetDirectoryFromDevice(dir, clientsubDir);
            }
        }

        /// <summary>
        /// Copies a folder from the client to the device running Factory Orchestrator Service. Creates directories if needed.
        /// </summary>
        /// <param name="clientDirectory">Path on client PC to the folder to copy.</param>
        /// <param name="serverDirectory">Path on device running Factory Orchestrator Service where the folder will be saved.</param>
        public async Task SendDirectoryToDevice(string clientDirectory, string serverDirectory)
        {
            if (!Directory.Exists(clientDirectory))
            {
                throw new DirectoryNotFoundException($"{clientDirectory} does not exist!");
            }

            var files = Directory.EnumerateFiles(clientDirectory);
            var dirs = Directory.EnumerateDirectories(clientDirectory);

            foreach (var file in files)
            {
                await SendFileToDevice(file, Path.Combine(serverDirectory, Path.GetFileName(file)));
            }

            foreach (var dir in dirs)
            {
                await SendDirectoryToDevice(dir, Path.Combine(serverDirectory, new DirectoryInfo(dir).Name));
            }
        }

        /// <summary>
        /// Shutdown the device running Factory Orchestrator Service. 
        /// </summary>
        /// <param name="secondsUntilShutdown">How long to delay shutdown, in seconds.</param>
        public async void ShutdownDevice(uint secondsUntilShutdown = 0)
        {
            await RunExecutable(@"%systemroot%\system32\shutdown.exe", $"/s /t {secondsUntilShutdown}", null);
        }

        /// <summary>
        /// Reboot the device running Factory Orchestrator Service. 
        /// </summary>
        /// <param name="secondsUntilShutdown">How long to delay reboot, in seconds.</param>
        public async void RebootDevice(uint secondsUntilReboot = 0)
        {
            await RunExecutable(@"%systemroot%\system32\shutdown.exe", $"/r /t {secondsUntilReboot}", null);
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        protected virtual async Task WriteFileAsync(string file, byte[] data)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            File.WriteAllBytes(file, data);
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        protected virtual async Task<byte[]> ReadFileAsync(string file)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            return File.ReadAllBytes(file);
        }

        private IpcServiceClient<IFactoryOrchestratorService> _IpcClient;

        /// <summary>
        /// Event thrown when Client<=>Service connection is successfully established.
        /// </summary>
        public event IPCClientOnConnected OnConnected;
        
        /// <summary>
        /// True if Factory Orchestrator Service is running on the local device.
        /// </summary>
        public bool IsLocalHost
        {
            get
            {
                return (IpAddress == IPAddress.Loopback) ? true : false;
            }
        }

        /// <summary>
        /// True if the Client<=>Service connection is successfully established.
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// The IP address of the connected device.
        /// </summary>
        public IPAddress IpAddress { get; private set; }

        /// <summary>
        /// The port of the connected device used. Factory Orchestrator Service defaults to 45684.
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// Signature for OnConnected event handlers.
        /// </summary>
        public delegate void IPCClientOnConnected(); 
    }
}
