using FTFInterfaces;
using JKang.IpcServiceFramework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace FTFClient
{
    public static class IPCClientHelper
    {
        public static void StartIPCConnection(int port)
        {
            _ipcClient = new IpcServiceClientBuilder<IFTFCommunication>()
                .UseTcp(IPAddress.Loopback, port)
                .Build();
        }

        public static IpcServiceClient<IFTFCommunication> IpcClient { get => _ipcClient; }

        private static IpcServiceClient<IFTFCommunication> _ipcClient = null;
    }
}
