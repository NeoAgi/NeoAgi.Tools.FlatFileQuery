using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace NeoAgi.Tools.FlatFileQuery.Sqlite
{
    public class SqliteDataAccessObject : IDisposable
    {
        protected string ConnectionString { get; set; } = "Data Source=Sharable;Mode=Memory;Cache=Shared";

        protected SqliteConnection Connection { get; set; }

        public SqliteDataAccessObject(string? connectionString = null)
        {
            if (connectionString != null)
                ConnectionString = connectionString;

            Connection = new SqliteConnection(ConnectionString);
        }

        public async Task<int> NonQueryAsync(string query, Dictionary<string, string>? parameters = null)
        {
            var command = CreateCommand(query, parameters);

            return await command.ExecuteNonQueryAsync();
        }

        public async IAsyncEnumerable<Dictionary<string, string>> QueryAsync(string query, Dictionary<string, string>? parameters = null)
        {
            var command = CreateCommand(query, parameters);

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (reader.Read())
                {
                    Dictionary<string, string> record = new Dictionary<string, string>();
                    for(int i = 0; i < reader.FieldCount; i++)
                    {
                        record[reader.GetName(i)] = reader.GetValue(i).ToString() ?? string.Empty;
                    }

                    yield return record;
                }
            }
        }

        protected SqliteCommand CreateCommand(string query, Dictionary<string, string>? parameters = null)
        {
            OpenConnection();

            var command = Connection.CreateCommand();
            command.CommandText = query;
            command.CommandType = CommandType.Text;
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    command.Parameters.AddWithValue("@" + SafeParamKey(parameter.Key), parameter.Value);
                }
            }

            return command;
        }

        public string SafeParamKey(string val)
        {
            return val.Replace(' ', '_').Replace('/', '_').Replace('&', '_').Replace('-', '_');
        }

        protected void OpenConnection()
        {
            if(Connection.State != ConnectionState.Open) 
                Connection.Open();
        }

        protected void CloseConnection()
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
