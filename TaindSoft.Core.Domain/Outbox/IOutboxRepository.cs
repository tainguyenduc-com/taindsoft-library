using TaindSoft.Core.Domain.Repositories;

namespace TaindSoft.Core.Domain.Outbox
{
    /// <summary>
    /// TODO: Document interface IOutboxRepository
    /// </summary>
    public interface IOutboxRepository : IRepository<OutboxMessage>
    {
        Task<List<OutboxMessage>> GetUnprocessedAsync(CancellationToken ct);
    }
}
