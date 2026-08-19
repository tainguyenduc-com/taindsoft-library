using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using TaindSoft.Core.Infrastructure.Attributes;

namespace TaindSoft.Core.Infrastructure.EntityFramework
{
    public abstract class DesignTimeDbContextFactory<TDbContext> : IDesignTimeDbContextFactory<TDbContext>
        where TDbContext : BaseDbContext
    {
        private string ConnectionString { get; } = "";
        protected virtual IConfiguration GetConfiguration()
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables();

            return builder.Build();
        }

        public TDbContext CreateDbContext(string[] args)
        {
            IConfiguration configuration = GetConfiguration() ?? throw new InvalidOperationException("Configuration not found for design-time DbContext creation.");

            // Resolve connection string for design-time. Try module-specific key, then Default, then PostgreSql for backward-compat.
            string? connectionString = null;

            var connectionAttribute = typeof(TDbContext).GetCustomAttribute<ConnectionAttribute>();
            if (connectionAttribute != null && !string.IsNullOrWhiteSpace(connectionAttribute.ConnectionString))
            {
                connectionString = connectionAttribute.ConnectionString;
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                connectionString = configuration.GetConnectionString("Default");
            }

            DbContextOptionsBuilder<TDbContext> optionsBuilder = new();
            optionsBuilder.UseNpgsql(connectionString, npg => npg.MigrationsAssembly(typeof(TDbContext).Assembly.GetName().Name));

            // Find a matching constructor. DbContexts may use primary constructors with optional
            // parameters (e.g. ITimeProvider, IDomainEventDispatcher). Pick the first constructor
            // whose first parameter is DbContextOptions<TDbContext>, and fill remaining params with defaults.
            ConstructorInfo? ctor = typeof(TDbContext).GetConstructors()
                .FirstOrDefault(c =>
                    c.GetParameters().Length >= 1 &&
                    c.GetParameters()[0].ParameterType == typeof(DbContextOptions<TDbContext>));

            if (ctor == null)
            {
                throw new InvalidOperationException(
                    $"No constructor found on '{typeof(TDbContext).FullName}' that accepts DbContextOptions<{typeof(TDbContext).Name}> as the first parameter.");
            }

            ParameterInfo[] paramInfos = ctor.GetParameters();
            object?[] ctorArgs = new object[paramInfos.Length];
            ctorArgs[0] = optionsBuilder.Options;
            for (int i = 1; i < paramInfos.Length; i++)
            {
                ctorArgs[i] = paramInfos[i].HasDefaultValue ? paramInfos[i].DefaultValue : null;
            }

            return (TDbContext)ctor.Invoke(ctorArgs)!;
        }
    }
}
