using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace JKang.IpcServiceFramework.Client.Tcp
{
    public class TcpIpcClientOptions : IpcClientOptions
    {
        public IPAddress ServerIp { get; set; } = IPAddress.Loopback;
        public int ServerPort { get; set; } = 11843;
        public bool EnableSsl { get; set; }
        public string SslServerIdentity { get; set; }
        public RemoteCertificateValidationCallback SslValidationCallback { get; set; }
        public X509Certificate ClientCertificate { get; set; }
        public bool CheckSslCertificateRevocation { get; set; } = false;
    }
}
