using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaindSoft.Core.Caching.Abstractions;
using TaindSoft.Core.Caching.DistributedCaching;
using TaindSoft.Core.Caching.Redis;

namespace TaindSoft.Core.Caching
{
    /// <summary>
    /// TODO: Document class ServiceCollectionExtensions
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        // Registers the shared ICacheProvider and, for compatibility, registers
        // IDistributedCache via AddStackExchangeRedisCache when a connection
        // string is provided. Modules that rely on IDistributedCache (existing
        // code) will keep working; new code can consume ICacheProvider.
        public static IServiceCollection AddRedisCacheProvider(this IServiceCollection services, string? connectionString)
        {
            // Register IDistributedCache
            _ = services.AddStackExchangeRedisCache(opts => opts.Configuration = connectionString);

            // Register shared ICacheProvider
            _ = services.AddSingleton<ICacheProvider>(sp => new RedisCacheProvider(sp.GetRequiredService<IDistributedCache>()));

            return services;
        }

        public static IServiceCollection AddRedisCacheProvider(this IServiceCollection services, IConfiguration configuration, string configKey = "Redis:Connection")
        {
            string? connectionString = configuration.GetSection(configKey).Value ?? configuration.GetConnectionString("Redis");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                _ = services.AddStackExchangeRedisCache(opts => opts.Configuration = connectionString);
                _ = services.AddSingleton<ICacheProvider>(sp => new RedisCacheProvider(sp.GetRequiredService<IDistributedCache>()));
            }
            else
            {
                // Fallback to in-memory distributed cache when Redis not configured
                _ = services.AddDistributedMemoryCache();
                _ = services.AddSingleton<ICacheProvider, RedisCacheProvider>();
            }

            return services;
        }

        public static IServiceCollection AddCacheService(this IServiceCollection services)
        {
            _ = services.AddScoped<IDistributedCacheService, DistributedCacheService>();
            return services;
        }
    }
}
