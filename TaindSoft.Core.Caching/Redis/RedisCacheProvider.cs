using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using TaindSoft.Core.Caching.Abstractions;

namespace TaindSoft.Core.Caching.Redis
{
    public class RedisCacheProvider(IDistributedCache cache) : ICacheProvider
    {
        private readonly IDistributedCache _cache = cache;

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            string? cachedValue = await _cache.GetStringAsync(key);
            return cachedValue == null ? null : JsonSerializer.Deserialize<T>(cachedValue);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null) where T : class
        {
            DistributedCacheEntryOptions options = new();
            if (ttl.HasValue)
            {
                options.SetAbsoluteExpiration(ttl.Value);
            }

            string serializedValue = JsonSerializer.Serialize(value);
            await _cache.SetStringAsync(key, serializedValue, options);
        }

        public async Task RemoveAsync(string key)
        {
            await _cache.RemoveAsync(key);
        }
    }
}
