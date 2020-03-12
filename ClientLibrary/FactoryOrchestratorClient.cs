using JKang.IpcServiceFramework;
using Microsoft.FactoryOrchestrator.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
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
            IsConnected = false;
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
            try
            {
                await _IpcClient.InvokeAsync(x => x.GetServiceVersionString());
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }

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
            catch (FactoryOrchestratorConnectionException)
            {
                return false;
            }
        }

        /// <summary>
        /// Copies a file from the client to the device running Factory Orchestrator Service. Creates directories if needed.
        /// </summary>
        /// <param name="clientFilename">Path on client PC to the file to copy.</param>
        /// <param name="serverFilename">Path on device running Factory Orchestrator Service where the file will be saved.</param>
        public async Task<long> SendFileToDevice(string clientFilename, string serverFilename)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                clientFilename = Environment.ExpandEnvironmentVariables(clientFilename);

                if (!File.Exists(clientFilename))
                {
                    throw new FileNotFoundException($"{clientFilename} does not exist!");
                }

                var bytes = await ReadFileAsync(clientFilename);
                await _IpcClient.InvokeAsync(x => x.SendFile(serverFilename, bytes));
                return bytes.Length;
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Copies a file from the device running Factory Orchestrator Service to the client. Creates directories if needed.
        /// </summary>
        /// <param name="serverFilename">Path on device running Factory Orchestrator Service to the file to copy.</param>
        /// <param name="clientFilename">Path on client PC where the file will be saved.</param>
        public async Task<long> GetFileFromDevice(string serverFilename, string clientFilename)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                clientFilename = Environment.ExpandEnvironmentVariables(clientFilename);

                // Create target folder, if needed.
                Directory.CreateDirectory(Path.GetDirectoryName(clientFilename));

                var bytes = await _IpcClient.InvokeAsync(x => x.GetFile(serverFilename));
                await WriteFileAsync(clientFilename, bytes);
                return bytes.Length;
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Copies a folder from the device running Factory Orchestrator Service to the client. Creates directories if needed.
        /// </summary>
        /// <param name="serverDirectory">Path on device running Factory Orchestrator Service to the folder to copy.</param>
        /// <param name="clientDirectory">Path on client PC where the folder will be saved.</param>
        public async Task<long> GetDirectoryFromDevice(string serverDirectory, string clientDirectory)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                var files = await _IpcClient.InvokeAsync(x => x.EnumerateFiles(serverDirectory, false));
                var dirs = await _IpcClient.InvokeAsync(x => x.EnumerateDirectories(serverDirectory, false));
                long bytesReceived = 0;

                clientDirectory = Environment.ExpandEnvironmentVariables(clientDirectory);
                if (!Directory.Exists(clientDirectory))
                {
                    Directory.CreateDirectory(clientDirectory);
                }

                foreach (var file in files)
                {
                    var filename = Path.GetFileName(file);
                    bytesReceived += await GetFileFromDevice(file, Path.Combine(clientDirectory, filename));
                }
                foreach (var dir in dirs)
                {
                    var subDirName = new DirectoryInfo(dir).Name;
                    var clientsubDir = Path.Combine(clientDirectory, subDirName);

                    if (!Directory.Exists(clientsubDir))
                    {
                        Directory.CreateDirectory(clientsubDir);
                    }

                    bytesReceived += await GetDirectoryFromDevice(dir, clientsubDir);
                }

                return bytesReceived;
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Copies a folder from the client to the device running Factory Orchestrator Service. Creates directories if needed.
        /// </summary>
        /// <param name="clientDirectory">Path on client PC to the folder to copy.</param>
        /// <param name="serverDirectory">Path on device running Factory Orchestrator Service where the folder will be saved.</param>
        public async Task<long> SendDirectoryToDevice(string clientDirectory, string serverDirectory)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                clientDirectory = Environment.ExpandEnvironmentVariables(clientDirectory);
                if (!Directory.Exists(clientDirectory))
                {
                    throw new DirectoryNotFoundException($"{clientDirectory} does not exist!");
                }

                var files = Directory.EnumerateFiles(clientDirectory);
                var dirs = Directory.EnumerateDirectories(clientDirectory);
                long bytesSent = 0;

                foreach (var file in files)
                {
                    bytesSent += await SendFileToDevice(file, Path.Combine(serverDirectory, Path.GetFileName(file)));
                }

                foreach (var dir in dirs)
                {
                    bytesSent += await SendDirectoryToDevice(dir, Path.Combine(serverDirectory, new DirectoryInfo(dir).Name));
                }

                return bytesSent;
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Shutdown the device running Factory Orchestrator Service. 
        /// </summary>
        /// <param name="secondsUntilShutdown">How long to delay shutdown, in seconds.</param>
        public async void ShutdownDevice(uint secondsUntilShutdown = 0)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                await RunExecutable(@"%systemroot%\system32\shutdown.exe", $"/s /t {secondsUntilShutdown}", null);
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Reboot the device running Factory Orchestrator Service. 
        /// </summary>
        /// <param name="secondsUntilReboot">How long to delay reboot, in seconds.</param>
        public async void RebootDevice(uint secondsUntilReboot = 0)
        {
            if (!IsConnected)
            {
                throw new FactoryOrchestratorConnectionException("Start connection first!");
            }

            try
            {
                await RunExecutable(@"%systemroot%\system32\shutdown.exe", $"/r /t {secondsUntilReboot}", null);
            }
            catch (Exception ex)
            {
                throw CreateIpcException(ex);
            }
        }

        /// <summary>
        /// Creates a FactoryOrchestratorConnectionException if needed.
        /// </summary>
        private Exception CreateIpcException(Exception ex)
        {
            if (ex.HResult == -2147467259 || (ex.GetType() == typeof(ArgumentOutOfRangeException) && ex.Message.Contains("Header length must be 4 but was ")) || (ex.InnerException != null && ex.InnerException.GetType() == typeof(System.Net.Sockets.SocketException)))
            {
                IsConnected = false;
                ex = new FactoryOrchestratorConnectionException(IpAddress);
            }

            return ex;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        /// <summary>
        /// Writes bytes to file.
        /// </summary>
        /// <param name="file">File to write.</param>
        /// <param name="data">Bytes to write to file.</param>
        /// <returns></returns>
        protected virtual async Task WriteFileAsync(string file, byte[] data)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            File.WriteAllBytes(file, data);
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        /// <summary>
        /// Read bytes from file.
        /// </summary>
        /// <param name="file">File to read.</param>
        /// <returns></returns>
        protected virtual async Task<byte[]> ReadFileAsync(string file)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            return File.ReadAllBytes(file);
        }

        /// <summary>
        /// Event raised when Client-Service connection is successfully established.
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
        /// True if the Client-Service connection is successfully established.
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

        private IpcServiceClient<IFactoryOrchestratorService> _IpcClient;
    }

    /// <summary>
    /// Signature for event handlers.
    /// </summary>
    public delegate void IPCClientOnConnected();

    /// <summary>
    /// A FactoryOrchestratorConnectionException describes a Factory Orchestrator Client-Server connection issue.
    /// </summary>
    public class FactoryOrchestratorConnectionException : FactoryOrchestratorException
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="ip">IP address Client is communicating with.</param>
        public FactoryOrchestratorConnectionException(IPAddress ip) : base($"Failed to communicate with Factory Orchestrator Service on {ip}!")
        { }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Exception text.</param>
        public FactoryOrchestratorConnectionException(string message) : base(message)
        { }
    }
}
