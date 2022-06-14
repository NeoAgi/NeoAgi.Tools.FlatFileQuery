using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace NeoAgi.Tools.FlatFileQuery.Sqlite
{
    internal class SqliteDataAccessObject : IDisposable
    {
        protected string ConnectionString { get; set; } = "Data Source=Sharable;Mode=Memory;Cache=Shared";

        protected SqliteConnection Connection { get; set; }

        public SqliteDataAccessObject(string? connectionString = null)
        {
            if (connectionString != null)
                ConnectionString = connectionString;

            Connection = new SqliteConnection();
        }

        public async Task<int> NonQueryAsync(string query, Dictionary<string, string> parameters)
        {
            var command = CreateCommand(query, parameters);

            return await command.ExecuteNonQueryAsync();
        }

        public async IAsyncEnumerable<Dictionary<string, string>> QueryAsync(string query, Dictionary<string, string> parameters)
        {
            var command = CreateCommand(query, parameters);

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (reader.Read())
                {
                    Dictionary<string, string> record = new Dictionary<string, string>();
                    record["Name"] = reader.GetString(0);

                    yield return record;
                }
            }
        }

        public SqliteCommand CreateCommand(string query, Dictionary<string, string> parameters)
        {
            OpenConnection();

            var command = Connection.CreateCommand();
            command.CommandText = query;
            command.CommandType = CommandType.Text;
            foreach (var parameter in parameters)
            {
                command.Parameters.AddWithValue("@" + parameter.Key, parameter.Value);
            }

            return command;
        }

        public void OpenConnection()
        {
            if(Connection.State != ConnectionState.Open) 
                Connection.Open();
        }

        public void CloseConnection()
        {
            if (Connection.State != System.Data.ConnectionState.Closed)
                Connection.Close();
        }

        #region IDisposeable
        public void Dispose()
        {
            CloseConnection();
        }
        #endregion
    }
}
