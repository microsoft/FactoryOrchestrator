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
        public static void StartIPCConnection(IPAddress host, int port)
        {
            _ipcClient = new IpcServiceClientBuilder<IFTFCommunication>()
                .UseTcp(host, port)
                .Build();
        }

        public static IpcServiceClient<IFTFCommunication> IpcClient { get => _ipcClient; }

        private static IpcServiceClient<IFTFCommunication> _ipcClient = null;
    }
}
