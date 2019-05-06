using JKang.IpcServiceFramework;
using Microsoft.FactoryTestFramework.Core;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Microsoft.FactoryTestFramework.Client
{
    public static class IPCClientHelper
    {
        static IPCClientHelper()
        {
            OnConnected = null;
            IpcClient = null;
        }

        public static async void StartIPCConnection(IPAddress host, int port)
        {
            IsLocalHost = (host == IPAddress.Loopback) ? true : false;

            IpcClient = new IpcServiceClientBuilder<IFTFCommunication>()
                .UseTcp(host, port)
                .Build();

            // Test a command to make sure connection works
            await IpcClient.InvokeAsync(x => x.GetServiceVersionString());

            OnConnected?.Invoke();
        }

        public static IpcServiceClient<IFTFCommunication> IpcClient { get; private set; }
        public static event IPCClientOnConnected OnConnected;
        public static bool IsLocalHost { get; private set; }

        public delegate void IPCClientOnConnected();
    }
}
