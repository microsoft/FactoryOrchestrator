using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using PeterKottas.DotNetCore.WindowsService;
using JKang.IpcServiceFramework;
using System.Net;
using System.Threading.Tasks;

namespace FTFService
{
    public interface IComputingService
    {
        float AddFloat(float x, float y);
    }

    class ComputingService : IComputingService
    {
        public float AddFloat(float x, float y)
        {
            return x + y;
        }
    }

    class FTFExecutable
    {
        public static void Main(string[] args)
        {
#if DEBUG
            var _logLevel = LogLevel.Debug;
#else
            var _logLevel = LogLevel.Information;
#endif
            // Create service collection
            var services = new ServiceCollection();

            // Configure service provider for logger creation and managment
            ServiceProvider svcProvider = services
                .AddLogging(builder =>
                {
                    builder
                    .SetMinimumLevel(_logLevel);

                })
                .AddOptions()
                .AddSingleton(new LoggerFactory())
                .BuildServiceProvider();

            // Enable both console logging and file logging
            svcProvider.GetService<ILoggerFactory>().AddConsole();
            svcProvider.GetRequiredService<ILoggerFactory>().AddProvider(new LogFileProvider());

            // Configure IPC service framework server
            services.AddIpc(Troll);


            IIpcServiceHost ipcHost = new IpcServiceHostBuilder(svcProvider).AddTcpEndpoint<IComputingService>(name: "tcp45684", ipEndpoint: IPAddress.Loopback, port: 45684)
                                                                            .Build();

            var _logger = svcProvider.GetRequiredService<ILoggerFactory>().CreateLogger<FTFExecutable>();

            // FTFService handler
            ServiceRunner<FTFService>.Run(config =>
            {
                var name = config.GetDefaultName();
                config.Service(serviceConfig =>
                {
                    serviceConfig.ServiceFactory((extraArguments, controller) =>
                    {
                        return new FTFService(controller, svcProvider.GetRequiredService<ILoggerFactory>().CreateLogger<FTFService>());
                    });

                    serviceConfig.OnStart((service, extraParams) =>
                    {
                        _logger.LogInformation("Service {0} started", name);
                        service.Start();
                    });

                    serviceConfig.OnStop(service =>
                    {
                        _logger.LogInformation("Service {0} stopped", name);
                        service.Stop();
                    });

                    serviceConfig.OnError(e =>
                    {
                        _logger.LogError(e, string.Format("Service {0} errored with exception", name));
                    });
                });
            });

            // Dispose of loggers, this needs to be done manually
            svcProvider.GetService<ILoggerFactory>().Dispose();
        }

        private static void Troll(IIpcServiceBuilder builder)
        {
            builder
                .AddTcp()
                .AddService<IComputingService, ComputingService>();
        }
    }
}
