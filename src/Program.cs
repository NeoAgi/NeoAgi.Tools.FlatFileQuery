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
                    configuration.AddJsonFile("appsettings.json", optional: false);
                    configuration.AddEnvironmentVariables(prefix: "APP_");
                    configuration.AddOpts<ServiceConfig>(args, "ServiceConfig", outputStream: Console.Out);
                })
                .ConfigureLogging((hostContext, logBuilder) =>
                {
                    logBuilder.ClearProviders();
                    logBuilder.SetMinimumLevel(Enum<Microsoft.Extensions.Logging.LogLevel>.ParseOrDefault(
                        hostContext.Configuration.GetValue<string>("ServiceConfig:LogLevel"), Microsoft.Extensions.Logging.LogLevel.Debug));

                    ServiceConfig serviceConfig = hostContext.Configuration.GetSection("ServiceConfig").Get<ServiceConfig>();

                    logBuilder.AddNLog(serviceConfig.LoggingConfigurationFile);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<ServiceConfig>(hostContext.Configuration.GetSection("ServiceConfig"));
                    services.AddHostedService<Worker>();
                });
    }
}
