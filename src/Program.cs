using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeoAgi.CommandLine.Exceptions;
using NLog.Extensions.Logging;

namespace NeoAgi.Tools.FlatFileQuery
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                CreateHostBuilder(args).Build().Run();
            }
            catch (StopApplicationException stopex)
            {
                Console.WriteLine("Error: " + stopex.Message);
            }
            catch (CommandLineOptionParseException)
            {
                // Squelch the exception.  Output is captured below.
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(configuration =>
                {
                    configuration.Sources.Clear();
                    configuration.AddJsonFile("appsettings.json", optional: true);
                    configuration.AddEnvironmentVariables(prefix: "APP_");
                    configuration.AddOpts<ServiceConfig>(args, "ServiceConfig", outputStream: Console.Out);
                })
                .ConfigureLogging((hostContext, logBuilder) =>
                {
                    ServiceConfig serviceConfig = hostContext.Configuration.GetSection("ServiceConfig").Get<ServiceConfig>();

                    logBuilder.ClearProviders();
                    // Note: This has no effect at this time
                    // logBuilder.SetMinimumLevel(Enum.Parse<LogLevel>(serviceConfig.LogLevel));
                    logBuilder.AddNLog(serviceConfig.LoggingConfigurationFile);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<ServiceConfig>(hostContext.Configuration.GetSection("ServiceConfig"));
                    services.AddHostedService<Worker>();
                });
    }
}
