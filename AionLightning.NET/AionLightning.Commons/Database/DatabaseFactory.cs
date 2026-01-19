using AionLightning.Commons.Configs;

using System;
using System.Data.Common;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace AionLightning.Commons.Database
{
    public class DatabaseFactory
    {
        private readonly ILogger<DatabaseFactory> _log;
        private readonly string _connectionString;

        public DatabaseFactory(ILogger<DatabaseFactory> log, string driver, string url, string user, string password, int minConnections, int maxConnections)
        {
            _log = log;
            _connectionString = $"Server={url};Database=your_database;Uid={user};Pwd={password};"; // Adjust database name
            // Connection pooling is handled by ADO.NET provider
        }

        public DbConnection GetConnection()
        {
            try
            {
                var connection = new MySqlConnection(_connectionString);
                connection.Open();
                return connection;
            }
            catch (MySqlException e)
            {
                _log.LogError(e, "Failed to get database connection");
                throw;
            }
        }

        public void Shutdown()
        {
            // ADO.NET manages the connection pool automatically.
            // MySqlConnection.ClearAllPools(); can be used to clear pools if necessary.
            _log.LogInformation("Database connection pools are managed by the provider.");
        }

        public DbCommand CreateStatement(DbConnection connection)
        {
            return connection.CreateCommand();
        }
    }
}
