using NeoAgi.CommandLine;

namespace NeoAgi.Tools.FlatFileQuery
{
    public class ServiceConfig
    {
        [Option(FriendlyName = "Service Name", ShortName = "s", LongName = "serviceName", Description = "DisplayName for the Service", Required = false)]
        public string ServiceName { get; set; } = "NeoAgi.Tools.FlatFileQuery";

        [Option(FriendlyName = "Logging Configuration File", ShortName = "l", LongName = "logConfigFile", Description = "Filename of the Logging Configuration File", Required = false)]
        public string LoggingConfigurationFile { get; set; } = "nlog.console.config.xml";

        [Option(FriendlyName = "Logging Level", LongName = "logLevel", Description = "Omits logs lower than the level set.  Valid Options are Trace, Debug, Information, Warning, Critial.")]
        public string LogLevel { get; set; } = "Warning";

        [Option(FriendlyName = "Query to Perform", ShortName = "q", LongName = "query", Description = "Query to Perform against the specified data file", Required = true)]
        public string Query { get; set; } = string.Empty;
    }
}
