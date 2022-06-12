using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

using System.Threading.Tasks;
using System.Threading;

namespace NeoAgi.Tools.FlatFileQuery
{
    public class Worker : IHostedService
    {
        private readonly ILogger Logger;
        private readonly IHostApplicationLifetime AppLifetime;
        private readonly ServiceConfig Config;

        public Worker(ILogger<Worker> logger, IOptions<ServiceConfig> config, IHostApplicationLifetime appLifetime)
        {
            Logger = logger;
            AppLifetime = appLifetime;
            Config = config.Value;

            appLifetime.ApplicationStarted.Register(OnStarted);
            appLifetime.ApplicationStopping.Register(OnStopping);
            appLifetime.ApplicationStopped.Register(OnStopped);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("{serviceName} Started", Config.ServiceName);
            
            /* Begin Service/Worker Logic */

            AppLifetime.StopApplication();

            await Task.CompletedTask;

            return;
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("{serviceName} Stopping", Config.ServiceName);

            return Task.CompletedTask;
        }

        private void OnStarted() { }
        private void OnStopping() { }
        private void OnStopped() { }
    }
}
