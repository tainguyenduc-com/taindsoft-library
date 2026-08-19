using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TaindSoft.Core.Infrastructure.Extensions
{
    /// <summary>
    /// Production-safe EF Core configuration extensions.
    /// Configures retry policies, timeouts, logging, and tracking behavior.
    /// </summary>
    public static class EfCoreProductionExtensions
    {
        /// <summary>
        /// Configures DbContext with production-safe settings for PostgreSQL.
        /// </summary>
        public static IServiceCollection AddProductionSafeDbContext<TContext>(
            this IServiceCollection services,
            IConfiguration configuration,
            string connectionStringName = "DefaultConnection")
            where TContext : DbContext
        {
            string? connectionString = configuration.GetConnectionString(connectionStringName);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"Connection string '{connectionStringName}' not found in configuration");
            }

            services.AddDbContext<TContext>((serviceProvider, options) =>
            {
                ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                string environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";
                bool isProduction = environment.Equals("Production", StringComparison.OrdinalIgnoreCase);

                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    // Enable automatic retry on transient failures
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);

                    // Set command timeout (30 seconds default)
                    npgsqlOptions.CommandTimeout(30);

                    // Enable connection resilience
                    npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                });

                // Disable sensitive data logging in production
                if (isProduction)
                {
                    options.EnableSensitiveDataLogging(false);
                    options.EnableDetailedErrors(false);
                }
                else
                {
                    options.EnableSensitiveDataLogging(true);
                    options.EnableDetailedErrors(true);
                }

                // Enable logging factory
                options.UseLoggerFactory(loggerFactory);

                // Log slow queries (>500ms)
                options.LogTo(
                    (eventId, level) => level >= LogLevel.Warning || eventId.Name == "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted",
                    (eventData) =>
                    {
                        if (eventData is Microsoft.EntityFrameworkCore.Diagnostics.CommandExecutedEventData commandData)
                        {
                            if (commandData.Duration.TotalMilliseconds > 500)
                            {
                                ILogger logger = loggerFactory.CreateLogger("EFCore.SlowQuery");
                                logger.LogWarning(
                                    "Slow query detected: {Sql} took {Duration}ms",
                                    commandData.Command.CommandText,
                                    commandData.Duration.TotalMilliseconds);
                            }
                        }
                    });

                // Use default no-tracking for query handlers (better performance)
                // Override with .AsTracking() when needed
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            });

            return services;
        }
    }
}
