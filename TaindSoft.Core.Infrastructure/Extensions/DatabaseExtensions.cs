using Microsoft.Extensions.Logging;
using Npgsql;

namespace TaindSoft.Core.Infrastructure.Extensions
{
    /// <summary>
    /// TODO: Document class DatabaseExtensions
    /// </summary>
    public static class DatabaseExtensions
    {
        /// <summary>
        /// Ensure the PostgreSQL database specified in the connection string exists.
        /// This will attempt to connect to the server's 'postgres' maintenance DB and create
        /// the target database if it does not exist. Errors are logged and swallowed.
        /// </summary>
        public static void EnsurePostgresDatabaseExists(string connectionString, ILogger? logger = null)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                logger?.LogWarning("Empty connection string passed to EnsurePostgresDatabaseExists");
                return;
            }

            try
            {
                NpgsqlConnectionStringBuilder builder = new(connectionString);
                string? dbName = builder.Database;
                if (string.IsNullOrWhiteSpace(dbName))
                {
                    logger?.LogWarning("No Database specified in connection string");
                    return;
                }

                // Connect to the maintenance DB (postgres) to check/create the target DB
                NpgsqlConnectionStringBuilder masterBuilder = new(connectionString)
                {
                    Database = "postgres",
                    // Do not assign pooling for the short-lived admin connection
                    Pooling = false
                };

                using NpgsqlConnection conn = new(masterBuilder.ConnectionString);
                conn.Open();

                using NpgsqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = @name";
                _ = cmd.Parameters.AddWithValue("name", dbName);
                object? exists = cmd.ExecuteScalar();
                if (exists == null)
                {
                    logger?.LogInformation("Database '{Database}' does not exist. Creating...", dbName);
                    cmd.Parameters.Clear();
                    // Use quoted identifier to handle unusual names
                    cmd.CommandText = $"CREATE DATABASE \"{dbName.Replace("\"", "\"\"")}\"";
                    _ = cmd.ExecuteNonQuery();
                    logger?.LogInformation("Database '{Database}' created successfully.", dbName);
                }
                else
                {
                    logger?.LogDebug("Database '{Database}' already exists.", dbName);
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to ensure Postgres database exists for connection string.");
            }
        }
    }
}
