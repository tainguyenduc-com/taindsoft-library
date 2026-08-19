using Microsoft.Extensions.Caching.Memory;
using TaindSoft.Core.Caching.Abstractions;

namespace TaindSoft.Core.Caching.Memory
{
    /// <summary>
    /// TODO: Document class MemoryCacheProvider
    /// </summary>
    public class MemoryCacheProvider(IMemoryCache cache) : ICacheProvider
    {
        private readonly IMemoryCache _cache = cache;

        public Task<T?> GetAsync<T>(string key) where T : class
        {
            return _cache.TryGetValue(key, out T? value) ? Task.FromResult(value) : Task.FromResult<T?>(null);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null) where T : class
        {
            MemoryCacheEntryOptions options = new();
            if (ttl.HasValue)
            {
                _ = options.SetAbsoluteExpiration(ttl.Value);
            }

            _ = _cache.Set(key, value, options);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            _cache.Remove(key);
            return Task.CompletedTask;
        }
    }
}
