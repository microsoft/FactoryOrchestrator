using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using PeterKottas.DotNetCore.WindowsService;

namespace FTFService
{
    class FTFExecutable
    {
        public static void Main(string[] args)
        {
#if DEBUG
            var _logLevel = LogLevel.Debug;
#else
            var _logLevel = LogLevel.Information;
#endif
            // Configure service provider for logger creation and managment
            var svcProvider = new ServiceCollection()
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

            var _logger = svcProvider.GetRequiredService<ILoggerFactory>().CreateLogger<FTFExecutable>();

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

            // Dispose of loggers
            svcProvider.GetService<ILoggerFactory>().Dispose();
        }
    }
}
