namespace TaindSoft.Core.Caching.Builders
{

    /// <summary>
    /// Cache key builder for consistent key generation
    /// </summary>
    public static class CacheKeyBuilder
    {
        public static string Build(string module, string entity, string operation, params object[] parameters)
        {
            string paramStr = parameters.Length > 0 ? string.Join(":", parameters) : "";
            return $"{module}:{entity}:{operation}:{paramStr}".ToLowerInvariant();
        }

    }
}
