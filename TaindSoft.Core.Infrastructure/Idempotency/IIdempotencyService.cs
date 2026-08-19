namespace TaindSoft.Core.Infrastructure.Idempotency
{
    /// <summary>
    /// Service interface for idempotency management
    /// </summary>
    public interface IIdempotencyService
    {
        Task<(string Key, string RequestHash, string ResponseBody, int StatusCode)?> GetByKeyAsync(string key);
        Task StoreAsync(string key, string requestHash, string responseBody, int statusCode);
    }
}
