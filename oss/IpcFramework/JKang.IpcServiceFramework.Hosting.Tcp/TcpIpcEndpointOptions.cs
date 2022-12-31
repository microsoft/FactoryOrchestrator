using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace JKang.IpcServiceFramework.Hosting.Tcp
{
    public class TcpIpcEndpointOptions : IpcEndpointOptions
    {
        public IPAddress IpEndpoint { get; set; } = IPAddress.Loopback;
        public int Port { get; set; }
        public bool EnableSsl { get; set; }
        public X509Certificate SslCertificate { get; set; }
        public RemoteCertificateValidationCallback RemoteSslCertificateValidationCallback { get; set; } = null;
        public bool CheckSslCertificateRevocation { get; set; } = false;
        public bool AlwaysAllowLocalhostSslClients { get; set; } = true;
    }
}
