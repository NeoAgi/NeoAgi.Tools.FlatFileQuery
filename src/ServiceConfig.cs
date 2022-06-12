using NeoAgi.CommandLine;

namespace NeoAgi.Tools.FlatFileQuery
{
    public class ServiceConfig
    {
        [Option(FriendlyName = "Service Name", ShortName = "s", LongName = "serviceName", Description = "DisplayName for the Service", Required = false)]
        public string ServiceName { get; set; } = "NeoAgi.Tools.FlatFileQuery";
        
        [Option(FriendlyName = "Logging Configuration File", ShortName = "l", LongName = "logConfigFile", Description = "Filename of the Logging Configuration File", Required = false)]
        public string LoggingConfigurationFile { get; set; } = "nlog.console.config.xml";
    }
}
