using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace JKang.IpcServiceFramework.Client.Tcp
{
    internal class TcpIpcClient<TInterface> : IpcClient<TInterface>
        where TInterface : class
    {
        private readonly TcpIpcClientOptions _options;
        private readonly X509CertificateCollection _clientCertificateCollection = new X509CertificateCollection();

        public TcpIpcClient(string name, TcpIpcClientOptions options)
            : base(name, options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        protected override Task<IpcStreamWrapper> ConnectToServerAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
#pragma warning disable CA2000 // Dispose objects before losing scope. Disposed by IpcStreamWrapper
            TcpClient client = new TcpClient();
#pragma warning restore CA2000 // Dispose objects before losing scope

            if (!client.ConnectAsync(_options.ServerIp, _options.ServerPort)
                .Wait(_options.ConnectionTimeout, cancellationToken))
            {
                client.Close();
                cancellationToken.ThrowIfCancellationRequested();
                throw new TimeoutException();
            }

            Stream stream = client.GetStream();

            // if SSL is enabled, wrap the stream in an SslStream in client mode
            if (_options.EnableSsl)
            {
                SslStream ssl;
                if (_options.SslValidationCallback == null)
                {
                    ssl = new SslStream(stream, false);
                }
                else
                {
                    ssl = new SslStream(stream, false, _options.SslValidationCallback);
                }

                // set client mode and specify the common name(CN) of the server
                if (_options.SslServerIdentity != null)
                {
                    if (_options.ClientCertificate != null && _clientCertificateCollection.Count != 1)
                    {
                        _clientCertificateCollection.Clear();
                        _clientCertificateCollection.Add(_options.ClientCertificate);
                    }

                    ssl.AuthenticateAsClient(_options.SslServerIdentity, _clientCertificateCollection, System.Security.Authentication.SslProtocols.None, _options.CheckSslCertificateRevocation);
                }
                stream = ssl;
            }

            return Task.FromResult(new IpcStreamWrapper(stream, client));
        }
    }
}
