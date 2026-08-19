using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;
using TaindSoft.Core.Domain.Dispatchers;
using TaindSoft.Core.Domain.Repositories;
using TaindSoft.Core.Domain.UnitOfworks;
using TaindSoft.Core.Infrastructure.Dispatchers;
using TaindSoft.Core.Infrastructure.EntityFramework;
using TaindSoft.Core.Infrastructure.Extensions;

namespace TaindSoft.Core.Infrastructure
{
    public record InfrastructureOptions(
        string ConnectionString = "",
        bool EnsureDatabaseExists = false
    );
    /// <summary>
    /// Service collection extensions for Core.Infrastructure
    /// </summary>
    public static class InfrastructureServiceCollectionExtensions
    {
        /// <summary>
        /// Register all infrastructure services together from configuration
        /// </summary>
        public static IServiceCollection AddCoreInfrastructure<TDbContext>(
            this IServiceCollection services,
            IConfiguration config,
            string section = "Infrastructure")
            where TDbContext : BaseDbContext
        {
            var connectionString = config.GetSection(section).GetValue<string>("ConnectionString");
            var ensureDb = config.GetSection(section).GetValue<bool>("EnsureDatabaseExists");
            return services.AddCoreInfrastructure<TDbContext>(() =>
                new InfrastructureOptions(
                    ConnectionString: connectionString ?? "",
                    EnsureDatabaseExists: ensureDb
                ));
        }

        /// <summary>
        /// Register all infrastructure services together
        /// </summary>
        public static IServiceCollection AddCoreInfrastructure<TDbContext>(
            this IServiceCollection services,
            Func<InfrastructureOptions> configOptions)
            where TDbContext : BaseDbContext
        {
            InfrastructureOptions options = configOptions();
            if (string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                throw new InvalidOperationException("Connection string is required for AddCoreInfrastructure.");
            }



            // Only ensure database exists if explicitly requested (not recommended for Development)
            if (options.EnsureDatabaseExists)
            {
                DatabaseExtensions.EnsurePostgresDatabaseExists(options.ConnectionString);
            }

            _ = services.AddDbContext<TDbContext>((serviceProvider, contextOptions) =>
            {
                _ = contextOptions.UseNpgsql(options.ConnectionString);
                _ = contextOptions.ApplyOptimalConfiguration();
                AuditLoggingInterceptor? interceptor = serviceProvider.GetService<AuditLoggingInterceptor>();
                if (interceptor != null)
                {
                    contextOptions.AddInterceptors(interceptor);
                }
            });

            // Register default time provider implementation used by repositories



            return services;
        }

        /// <summary>
        /// Register all infrastructure services together
        /// </summary>
        public static IServiceCollection AddRepository<TDbContext>(
            this IServiceCollection services,
            Assembly assembly)
            where TDbContext : BaseDbContext
        {
            Type irepoType = typeof(IRepository<>);

            _ = services.AddScoped<IUnitOfWork<TDbContext>, UnitOfWork<TDbContext>>();
            // Also register non-generic IUnitOfWork to the generic implementation for easier DI in handlers
            _ = services.AddScoped(serviceType: typeof(IUnitOfWork), implementationFactory: sp => sp.GetRequiredService<IUnitOfWork<TDbContext>>());
            // Note: This registration ensures any module that calls AddRepository<TDbContext> will also have a mapping
            // for the non-generic IUnitOfWork to the generic UnitOfWork<TDbContext> implementation.

            // Register domain event dispatcher (scoped so it shares the DI scope with DbContext/repositories)
            services.TryAddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

            IEnumerable<Type> repoTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition);

            foreach (Type? type in repoTypes)
            {
                Type[] interfaces = type.GetInterfaces();
                foreach (Type iface in interfaces)
                {
                    // Skip framework/base interfaces
                    if (iface == typeof(IUnitOfWork) || iface == irepoType)
                        continue;

                    // Pattern 1: Direct IRepository<> generic interface
                    if (iface.IsGenericType && iface.GetGenericTypeDefinition() == irepoType)
                    {
                        _ = services.AddScoped(iface, type);
                        continue;
                    }

                    // Pattern 2: Domain-specific interface that inherits from IRepository<>
                    Type[] baseIfaces = iface.GetInterfaces();
                    if (baseIfaces.Any(b => b.IsGenericType && b.GetGenericTypeDefinition() == irepoType))
                    {
                        _ = services.AddScoped(iface, type);
                        continue;
                    }

                    // Pattern 3: Custom non-generic repository interface (e.g., ISiteSettingRepository, IStorageRepository)
                    // Match Domain ↔ Infrastructure by namespace convention
                    if (iface.IsInterface &&
                        iface.Name.EndsWith("Repository", StringComparison.Ordinal))
                    {
                        // E.g., TaindSoft.SystemManagement.Infrastructure → TaindSoft.SystemManagement.Domain
                        string implNamespace = type.Namespace ?? string.Empty;
                        string expectedDomainNamespace = implNamespace.Replace(".Infrastructure", ".Domain");

                        if (iface.Namespace != null &&
                            iface.Namespace.StartsWith(expectedDomainNamespace, StringComparison.Ordinal))
                        {
                            _ = services.AddScoped(iface, type);
                        }
                    }
                }
            }
            return services;
        }
    }
}
