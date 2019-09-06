﻿using JKang.IpcServiceFramework;
using Microsoft.FactoryOrchestrator.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TaskStatus = Microsoft.FactoryOrchestrator.Core.TaskStatus;

namespace Microsoft.FactoryOrchestrator.Client
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
            IpAddress = host;

            IpcClient = new IpcServiceClientBuilder<IFOCommunication>()
                .UseTcp(host, port)
                .Build();

            // Test a command to make sure connection works
            await IpcClient.InvokeAsync(x => x.GetServiceVersionString());

            OnConnected?.Invoke();
        }

        public static async Task<bool> TryStartIPCConnection(IPAddress host, int port)
        {
            try
            {
                await StartIPCConnection(host, port);
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
        /// Warning: This helper API only works in .NET executables! It WILL NOT work in UWP apps, including FactoryOrchestratorApp.
        /// FactoryOrchestratorApp has its own file transfer API in FileTransferHelper.cs
        /// </summary>
        public static async Task<bool> GetFileFromServer(string serverFilename, string clientFilename)
        {
            // Create target folder, if needed.
            Directory.CreateDirectory(Path.GetDirectoryName(clientFilename));

            var bytes = await IpcClient.InvokeAsync(x => x.GetFile(serverFilename));
            File.WriteAllBytes(clientFilename, bytes);

            return true;
        }

        public static async void ShutdownServerDevice(uint secondsUntilShutdown = 2)
        {
            await IpcClient.InvokeAsync(x => x.RunExecutable(@"%systemroot%\system32\shutdown.exe", $"/s /t {secondsUntilShutdown}", null));
        }

        public static async void RebootServerDevice(uint secondsUntilReboot = 2)
        {
            await IpcClient.InvokeAsync(x => x.RunExecutable(@"%systemroot%\system32\shutdown.exe", $"/r /t {secondsUntilReboot}", null));
        }

        public static IpcServiceClient<IFOCommunication> IpcClient { get; private set; }
        public static event IPCClientOnConnected OnConnected;
        public static bool IsLocalHost { get; private set; }
        public static IPAddress IpAddress { get; private set; }

        public delegate void IPCClientOnConnected();
    }
}
