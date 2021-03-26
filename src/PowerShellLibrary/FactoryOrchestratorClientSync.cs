// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
using Microsoft.FactoryOrchestrator.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Net;
// Keep in sync with CoreLibrary\FactoryOrchestratorClient.cs
namespace Microsoft.FactoryOrchestrator.Client
{
    /// <summary>
    /// A fully synchronous class for Factory Orchestrator .NET clients. Use instances of this class to communicate with Factory Orchestrator Service(s) via PowerShell.
    /// .NET clients are recommended to use FactoryOrchestratorClient instead which is fully asynchronous.
    /// </summary>
    public partial class FactoryOrchestratorClientSync : IFactoryOrchestratorService
    {
        /// <summary>
        /// Creates a new FactoryOrchestratorSyncClient instance. Most .NET clients are recommended to use FactoryOrchestratorClient instead which is fully asynchronous.
        /// </summary>
        /// <param name="host">IP address of the device running Factory Orchestrator Service. Use IPAddress.Loopback for local device.</param>
        /// <param name="port">Port to use. Factory Orchestrator Service defaults to 45684.</param>
        /// <param name="serverIdentity">Distinguished name for the server defaults to FactoryServer.</param>
        /// <param name="certhash">Hash value for the server certificate defaults to E8BF0011168803E6F4AF15C9AFE8C9C12F368C8F.</param>
        public FactoryOrchestratorClientSync(IPAddress host, int port = 45684, string serverIdentity = "FactoryServer", string certhash = "E8BF0011168803E6F4AF15C9AFE8C9C12F368C8F")
        {
            OnConnected = null;
            AsyncClient = new FactoryOrchestratorClient(host, port, serverIdentity, certhash);
        }

        /// <summary>
        /// Establishes a connection to the Factory Orchestrator Service.
        /// Throws an exception if it cannot connect.
        /// </summary>
        /// <param name="ignoreVersionMismatch">If true, ignore a Client-Service version mismatch.</param>
        public void Connect(bool ignoreVersionMismatch = false)
        {
            AsyncClient.Connect(ignoreVersionMismatch).Wait();
            OnConnected?.Invoke();
        }

        /// <summary>
        /// Attempts to establish a connection to the Factory Orchestrator Service.
        /// </summary>
        /// <param name="ignoreVersionMismatch">If true, ignore a Client-Service version mismatch.</param>
        /// <returns>true if it was able to connect.</returns>
        public bool TryConnect(bool ignoreVersionMismatch = false)
        {
            try
            {
                Connect(ignoreVersionMismatch);
                return true;
            }
            catch (FactoryOrchestratorConnectionException)
            {
                return false;
            }
        }

        /// <summary>
        /// Copies an app package to the Service and installs it. Requires Windows Device Portal.
        /// If the app package is already on the Service's computer, use InstallApp() instead.
        /// </summary>
        /// <param name="appFilename">Path on the Client's computer to the app package (.appx, .appxbundle, .msix, .msixbundle).</param>
        /// <param name="dependentPackages">List of paths on the Client's computer to the app's dependent packages.</param>
        /// <param name="certificateFile">Path on the Client's computer to the app's certificate file, if needed. Microsoft Store signed apps do not need a certificate.</param>
        public void SendAndInstallApp(string appFilename, List<string> dependentPackages = null, string certificateFile = null)
        {
            AsyncClient.SendAndInstallApp(appFilename, dependentPackages, certificateFile).Wait();
        }

        /// <summary>
        /// Copies a file from the client to the device running Factory Orchestrator Service. Creates directories if needed.
        /// </summary>
        /// <param name="clientFilename">Path on client PC to the file to copy.</param>
        /// <param name="serverFilename">Path on device running Factory Orchestrator Service where the file will be saved.</param>
        /// <param name="sendToContainer">If true, send the file to the container running on the connected device.</param>
        public long SendFileToDevice(string clientFilename, string serverFilename, bool sendToContainer = false)
        {
            return AsyncClient.SendFileToDevice(clientFilename, serverFilename, sendToContainer).Result;
        }

        /// <summary>
        /// Copies a file from the device running Factory Orchestrator Service to the client. Creates directories if needed.
        /// </summary>
        /// <param name="serverFilename">Path on running Factory Orchestrator Service to the file to copy.</param>
        /// <param name="clientFilename">Path on client PC where the file will be saved.</param>
        /// <param name="getFromContainer">If true, get the file from the container running on the connected device.</param>
        public long GetFileFromDevice(string serverFilename, string clientFilename, bool getFromContainer = false)
        {
            return AsyncClient.GetFileFromDevice(serverFilename, clientFilename, getFromContainer).Result;
        }

        /// <summary>
        /// Tries to delete a local file.
        /// </summary>
        /// <param name="clientFilename">The file to delete.</param>
        /// <returns></returns>
        protected virtual bool TryDeleteLocalFile(string clientFilename)
        {
            try
            {
                File.Delete(clientFilename);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Copies a folder from the device running Factory Orchestrator Service to the client. Creates directories if needed.
        /// </summary>
        /// <param name="serverDirectory">Path on device running Factory Orchestrator Service to the folder to copy.</param>
        /// <param name="clientDirectory">Path on client PC where the folder will be saved.</param>
        /// <param name="getFromContainer">If true, get the file from the container running on the connected device.</param>
        public long GetDirectoryFromDevice(string serverDirectory, string clientDirectory, bool getFromContainer = false)
        {
            return AsyncClient.GetDirectoryFromDevice(serverDirectory, clientDirectory, getFromContainer).Result;
        }

        /// <summary>
        /// Copies a folder from the client to the device running Factory Orchestrator Service. Creates directories if needed.
        /// </summary>
        /// <param name="clientDirectory">Path on client PC to the folder to copy.</param>
        /// <param name="serverDirectory">Path on device running Factory Orchestrator Service where the folder will be saved.</param>
        /// <param name="sendToContainer">If true, copy the folder to the container running on the connected device.</param>
        public long SendDirectoryToDevice(string clientDirectory, string serverDirectory, bool sendToContainer = false)
        {
            return AsyncClient.SendDirectoryToDevice(clientDirectory, serverDirectory, sendToContainer).Result;
        }


        /// <summary>
        /// Shutdown the device running Factory Orchestrator Service. 
        /// </summary>
        /// <param name="secondsUntilShutdown">How long to delay shutdown, in seconds.</param>
        public void ShutdownDevice(uint secondsUntilShutdown = 0)
        {
            AsyncClient.ShutdownDevice(secondsUntilShutdown);
        }

        /// <summary>
        /// Reboot the device running Factory Orchestrator Service. 
        /// </summary>
        /// <param name="secondsUntilReboot">How long to delay reboot, in seconds.</param>
        public void RebootDevice(uint secondsUntilReboot = 0)
        {
            AsyncClient.RebootDevice(secondsUntilReboot);
        }

        /// <summary>
        /// Gets the build number of FactoryOrchestratorClient.
        /// </summary>
        /// <returns></returns>
        public static string GetClientVersionString()
        {
            return FactoryOrchestratorClient.GetClientVersionString();
        }

        /// <summary>
        /// Creates a directory on the client.
        /// </summary>
        /// <param name="path">Directory to create.</param>
        /// <returns></returns>
        protected virtual void CreateDirectory(string path)
        {
            if (path != null)
            {
                Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// Creates the client file reader.
        /// </summary>
        /// <param name="clientFilename">The client filename.</param>
        /// <returns></returns>
        protected virtual Stream CreateClientFileReader(string clientFilename)
        {
            return File.OpenRead(clientFilename);
        }

        /// <summary>
        /// Creates the client file writer. File is overwritten if it exists.
        /// </summary>
        /// <param name="clientFilename">The client filename.</param>
        /// <returns></returns>
        protected virtual Stream CreateClientFileWriter(string clientFilename)
        {
            return File.Create(clientFilename);
        }

        /// <summary>
        /// Enumerates local directories in a given folder.
        /// </summary>
        /// <param name="path">The directory to eumerate.</param>
        /// <returns></returns>
        protected virtual IEnumerable<string> EnumerateLocalDirectories(string path)
        {
            return Directory.EnumerateDirectories(path);
        }

        /// <summary>
        /// Enumerates local files in a given folder.
        /// </summary>
        /// <param name="path">The directory to eumerate.</param>
        /// <returns></returns>
        protected virtual IEnumerable<string> EnumerateLocalFiles(string path)
        {
            return Directory.EnumerateFiles(path);
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
                return (IpAddress == IPAddress.Loopback);
            }
        }

        /// <summary>
        /// True if the Client-Service connection is successfully established.
        /// </summary>
        public bool IsConnected { get => AsyncClient.IsConnected; }

        /// <summary>
        /// The IP address of the connected device.
        /// </summary>
        public IPAddress IpAddress { get => AsyncClient.IpAddress; }

        /// <summary>
        /// The port of the connected device used. Factory Orchestrator Service defaults to 45684.
        /// </summary>
        public int Port { get => AsyncClient.Port; }

        /// <summary>
        /// The async client used to communicate with the service. Needed for using the ServerPoller class in PowerShell.
        /// </summary>
        public FactoryOrchestratorClient AsyncClient { get; private set; }

    }
}
