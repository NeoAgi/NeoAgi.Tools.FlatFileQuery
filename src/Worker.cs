using System.Globalization;
using CsvHelper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NeoAgi.Tools.FlatFileQuery.Sqlite;

namespace NeoAgi.Tools.FlatFileQuery
{
    public class Worker : IHostedService
    {
        private readonly ILogger Logger;
        private readonly IHostApplicationLifetime AppLifetime;
        private readonly ServiceConfig Config;

        protected string? InsertColumnNames { get; set; }

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

            // We have a raw query, extract out the path of the data file
            Tuple<string, string, string> tableInfo = ParseDataFileFromQuery(Config.Query);

            // If the file exists...
            if (System.IO.File.Exists(tableInfo.Item1))
            {
                using (SqliteDataAccessObject dao = new SqliteDataAccessObject())
                {
                    // Load the file into Sqlite
                    await LoadFile(dao, tableInfo.Item1, tableInfo.Item2);

                    // Perform our query
                    await ExecuteQueryAsync(dao, tableInfo.Item3);
                }
            }
            else
            {
                throw new StopApplicationException($"File {tableInfo.Item1} cannot be found.");
            }

            AppLifetime.StopApplication();

            await Task.CompletedTask;

            return;
        }

        public Tuple<string, string, string> ParseDataFileFromQuery(string query)
        {
            if(query.Trim().EndsWith(";"))
                query = query.Trim().Substring(0, query.Length - 1);

            string modifiedQuery = query;
            string? tableName = null;

            int fromIdx = query.IndexOf("FROM ");
            int whereIdx = query.IndexOf("WHERE ");

            if (whereIdx == -1)
                whereIdx = query.Length;

            string location = query.Substring(fromIdx, whereIdx - fromIdx).Substring(5);

            // At this point the location may look like "/some/path/file.csv AS someTable"
            int asIdx = Config.Query.IndexOf("AS ");
            if (asIdx > -1 && asIdx > fromIdx && asIdx < whereIdx)
            {
                string[] parts = location.Split("AS ");
                tableName = parts[1];

                // This replaces "/some/path/file.csv AS someTable" with "someTable"
                modifiedQuery = Config.Query.Replace(location, tableName);

                location = parts[0];
            }
            else
            {
                // Note, the table name could be inferred from the file name, but would that be obvious?
                throw new StopApplicationException("No AS clause found in FROM Predicate.  An AS Clause must be added to defrefence the table name.");
            }

            return new Tuple<string, string, string>(location.Trim(), tableName.Trim(), modifiedQuery);
        }

        public async Task LoadFile(SqliteDataAccessObject dao, string dataFileLocation, string tableName)
        {
            using (var reader = new StreamReader(dataFileLocation))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Read();
                csv.ReadHeader();

                string[] headers = csv.HeaderRecord;

                await CreateTableFromFieldNamesAsync(dao, tableName, headers);

                Dictionary<string, string> row = new Dictionary<string, string>();

                while (csv.Read())
                {
                    foreach (string columnName in headers)
                    {
                        row[columnName] = csv.GetField(columnName);
                    }

                    await InsertRowAsync(dao, tableName, row);
                }
            }
        }

        public async Task CreateTableFromFieldNamesAsync(SqliteDataAccessObject dao, string tableName, string[] fields)
        {
            List<string> columns = new List<string>(fields.Length);
            foreach (string columnName in fields)
            {
                columns.Add($"{columnName} TEXT NOT NULL");
            }

            string query = $"CREATE TABLE {tableName}({string.Join(',', columns)});";
            await dao.NonQueryAsync(query);
        }

        public async Task<bool> InsertRowAsync(SqliteDataAccessObject dao, string tableName, Dictionary<string, string> values)
        {
            List<string> fieldNames = new List<string>(values.Count);
            List<string> paramNames = new List<string>(values.Count);

            foreach (KeyValuePair<string, string> kvp in values)
            {
                fieldNames.Add(kvp.Key);
                paramNames.Add("@" + kvp.Key);
            }

            string query = $"INSERT INTO {tableName} ({string.Join(',', fieldNames)}) VALUES({string.Join(',', paramNames)})";
            int affected = await dao.NonQueryAsync(query, values);

            return (affected > 0);
        }

        public async Task ExecuteQueryAsync(SqliteDataAccessObject dao, string query)
        {
            Logger.LogInformation("Executing Query: {query}", query);
            var results = dao.QueryAsync(query);

            int recordIdx = 0;
            await foreach(var result in results)
            {
                Console.Write($"Record {recordIdx}| ");
                foreach(var kvp in result)
                {
                    Console.Write(kvp.Key + ": " + kvp.Value + " ");
                }

                Console.WriteLine();
                recordIdx++;
            }
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
