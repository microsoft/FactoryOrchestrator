
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FactoryOrchestrator.Core;

namespace Microsoft.FactoryOrchestrator.Service
{
    public sealed class Worker : IHostedService, IDisposable
    {
        private readonly ILogger<Worker> _logger;
        private FOService _svc;
        private bool disposedValue;
        private const string _name = "Microsoft.FactoryOrchestrator.Service";

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            _svc = new FOService(_logger);
        }


#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(Resources.ServiceStarting, _name);
            _svc.Start(cancellationToken);
            _logger.LogInformation(Resources.ServiceStarted, _name);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(Resources.ServiceStopping, _name);
            _svc.Stop();
            _logger.LogInformation(Resources.ServiceStopped, _name);
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _svc.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
