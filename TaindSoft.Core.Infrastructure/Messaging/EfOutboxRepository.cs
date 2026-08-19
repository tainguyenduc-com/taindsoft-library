using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaindSoft.Core.Domain.Outbox;
using TaindSoft.Core.Infrastructure.EntityFramework;

namespace TaindSoft.Core.Infrastructure.Messaging;

/// <summary>
/// Generic EF Core implementation of IOutboxRepository.
/// </summary>
/// <summary>
/// Repository responsible for persisting outbox messages into the EF-backed outbox table.
/// </summary>
public class EfOutboxRepository<TDbContext>(TDbContext dbContext, ILogger<Repository<TDbContext, OutboxMessage>>? logger = null) : Repository<TDbContext, OutboxMessage>(dbContext, logger), IOutboxRepository
    where TDbContext : BaseDbContext
{
    public async Task<List<OutboxMessage>> GetUnprocessedAsync(CancellationToken ct)
    {
        return await _dbContext.Set<OutboxMessage>()
            .Where(o => o.ProcessedOnUtc == null)
            .OrderBy(o => o.OccurredOnUtc)
            .ToListAsync(ct);
    }

    public async Task MarkProcessedAsync(int id, CancellationToken cancellationToken = default)
    {
        OutboxMessage? message = await _dbContext.Set<OutboxMessage>()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (message == null)
        {
            return;
        }

        message.ProcessedOnUtc = DateTime.UtcNow;
        message.Error = null;
        _ = await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(int id, string error, CancellationToken cancellationToken = default)
    {
        OutboxMessage? message = await _dbContext.Set<OutboxMessage>()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (message == null)
        {
            return;
        }

        message.Error = error;
        _ = await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
