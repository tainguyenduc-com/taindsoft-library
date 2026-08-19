using Microsoft.EntityFrameworkCore;
using TaindSoft.Core.Infrastructure.EntityFramework;

namespace TaindSoft.Core.Infrastructure.Idempotency
{
    /// <summary>
    /// Database-backed idempotency service using EF Core
    /// </summary>
    public class IdempotencyService(BaseDbContext dbContext) : IIdempotencyService
    {
        private readonly BaseDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

        public async Task<(string Key, string RequestHash, string ResponseBody, int StatusCode)?> GetByKeyAsync(string key)
        {
            IdempotencyRecord? record = await _dbContext.Set<IdempotencyRecord>()
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Key == key);

            if (record == null)
            {
                return null;
            }

            return (record.Key, record.RequestHash, record.ResponseBody, record.StatusCode);
        }

        public async Task StoreAsync(string key, string requestHash, string responseBody, int statusCode)
        {
            IdempotencyRecord record = new(
                key,
                requestHash,
                responseBody,
                statusCode,
                DateTime.UtcNow);

            _dbContext.Set<IdempotencyRecord>().Add(record);
            await _dbContext.SaveChangesAsync();
        }
    }
}
