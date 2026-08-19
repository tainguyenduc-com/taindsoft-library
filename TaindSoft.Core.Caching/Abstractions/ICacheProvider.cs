namespace TaindSoft.Core.Caching.Abstractions
{
    /// <summary>
    /// TODO: Document interface ICacheProvider
    /// </summary>
    public interface ICacheProvider
    {
        Task<T?> GetAsync<T>(string key) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan? ttl = null) where T : class;
        Task RemoveAsync(string key);
    }
}
