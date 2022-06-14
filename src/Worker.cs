using System.Globalization;
using CsvHelper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NeoAgi.Data.Sql.Sqlite;

namespace NeoAgi.Tools.FlatFileQuery
{
    public class Worker : IHostedService
    {
        private readonly ILogger Logger;
        private readonly IHostApplicationLifetime AppLifetime;
        private readonly ServiceConfig Config;

        protected const string SQL_DSN = "sqlite://localhost/?Data%20Source=Sharable&Mode=Memory&Cache=Shared";

        private SqliteDataAccessObject? _DAO = null;
        protected SqliteDataAccessObject DAO
        {
            get
            {
                if (_DAO == null)
                    _DAO = SqliteDataAccessObject.Create(SQL_DSN);

                return _DAO;
            }
        }

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

            // If the file exists... load the file
            if (System.IO.File.Exists(tableInfo.Item1))
            {
                await LoadFile(tableInfo.Item1, tableInfo.Item2);
            }
            else
            {
                throw new StopApplicationException($"File {tableInfo.Item1} cannot be found.");
            }

            // Perform our query
            await ExecuteQuery(tableInfo.Item3);

            AppLifetime.StopApplication();

            await Task.CompletedTask;

            return;
        }

        public Tuple<string, string, string> ParseDataFileFromQuery(string query)
        {
            string modifiedQuery = query;
            string? tableName = null;

            int fromIdx = Config.Query.IndexOf("FROM ");
            int whereIdx = Config.Query.IndexOf("WHERE ");

            if (whereIdx == -1)
                whereIdx = Config.Query.Length;

            string location = Config.Query.Substring(fromIdx, whereIdx - fromIdx).Substring(5);

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

        public async Task LoadFile(string dataFileLocation, string tableName)
        {
            using (var reader = new StreamReader(dataFileLocation))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Read();
                csv.ReadHeader();

                string[] headers = csv.HeaderRecord;

                CreateTableFromFieldNames(tableName, headers);

                Dictionary<string, string> row = new Dictionary<string, string>();

                while (csv.Read())
                {
                    foreach (string columnName in headers)
                    {
                        row[columnName] = csv.GetField(columnName);
                    }

                    InsertRow(tableName, row);
                }
            }

            await Task.CompletedTask;
        }

        public void CreateTableFromFieldNames(string tableName, string[] fields)
        {
            List<string> columns = new List<string>(fields.Length);
            foreach (string columnName in fields)
            {
                columns.Add($"{columnName} TEXT NOT NULL");
            }

            string query = $"CREATE TABLE {tableName}({string.Join(',', columns)});";
            DAO.NonQuery(query);
        }

        public bool InsertRow(string tableName, Dictionary<string, string> values)
        {
            List<string> fieldNames = new List<string>(values.Count);
            List<string> paramNames = new List<string>(values.Count);

            SqliteDataAccessObject dao = SqliteDataAccessObject.Create(SQL_DSN);

            foreach (KeyValuePair<string, string> kvp in values)
            {
                dao.AddParam("@" + kvp.Key, kvp.Value);
                fieldNames.Add(kvp.Key);
                paramNames.Add("@" + kvp.Key);
            }

            string query = $"INSERT INTO {tableName} ({string.Join(',', fieldNames)}) VALUES({string.Join(',', paramNames)})";
            int affected = dao.NonQuery(query);

            return (affected > 0);
        }

        public async Task ExecuteQuery(string query)
        {
            Logger.LogInformation("Executing Query: {query}", query);
            foreach (Simple record in DAO.Query<Simple>(query))
            {
                Console.WriteLine(record.Id + ": " + record.Name);
            }

            await Task.CompletedTask;
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

    public class Simple
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
