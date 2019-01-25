using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using PeterKottas.DotNetCore.WindowsService.Base;
using PeterKottas.DotNetCore.WindowsService.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FTFService
{
    public class FTFService : MicroService, IMicroService
    {
        private IMicroServiceController _controller;
        private ILogger<FTFService> _logger;

        public FTFService()
        {
            _controller = null;
        }

        public FTFService(IMicroServiceController controller, ILogger<FTFService> logger)
        {
            _controller = controller;
            _logger = logger;
        }

        
        public void Start()
        {
            StartBase();
            Timers.Start("Poller", 1000, () =>
            {
            _logger.LogInformation(string.Format("Polling at {0}\n", DateTime.Now.ToString("o")));
            });
            _logger.LogTrace("Started\n");
        }

        public void Stop()
        {
            StopBase();
            _logger.LogTrace("Stopped\n");
        }
    }
}
