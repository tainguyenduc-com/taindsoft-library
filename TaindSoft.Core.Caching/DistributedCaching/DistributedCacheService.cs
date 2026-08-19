using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace TaindSoft.Core.Caching.DistributedCaching
{
    /// <summary>
    /// Query caching for expensive database/external API calls
    /// </summary>
    public interface IDistributedCacheService
    {
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Default implementation using IDistributedCache (Redis or in-memory)
    /// </summary>
    public class DistributedCacheService(IDistributedCache cache, ILogger<DistributedCacheService> logger) : IDistributedCacheService
    {
        private readonly IDistributedCache _cache = cache;
        private readonly ILogger<DistributedCacheService> _logger = logger;

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                string? cachedData = await _cache.GetStringAsync(key, cancellationToken);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    T? result = JsonSerializer.Deserialize<T>(cachedData);
                    _logger.LogDebug("Cache hit for key: {Key}", key);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving from cache for key: {Key}", key);
            }

            return null;
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                string serialized = JsonSerializer.Serialize(value);
                DistributedCacheEntryOptions options = new();

                if (expiration.HasValue)
                {
                    options.AbsoluteExpirationRelativeToNow = expiration.Value;
                }
                else
                {
                    // Default 15 minutes
                    options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
                }

                await _cache.SetStringAsync(key, serialized, options, cancellationToken);
                _logger.LogDebug("Cache set for key: {Key} with expiration: {Expiration}", key, options.AbsoluteExpirationRelativeToNow);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error setting cache for key: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                await _cache.RemoveAsync(key, cancellationToken);
                _logger.LogDebug("Cache cleared for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error clearing cache for key: {Key}", key);
            }
        }
    }
}
