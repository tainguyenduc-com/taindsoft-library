using TaindSoft.Core.Domain.Events;

namespace TaindSoft.Core.Domain.Dispatchers
{
    /// <summary>
    /// Dispatcher for domain events.
    /// Supports in-memory handlers (before commit) and outbox publishing (after commit).
    /// </summary>
    public interface IDomainEventDispatcher
    {
        /// <summary>
        /// Dispatches domain events to registered in-memory handlers.
        /// Called BEFORE database transaction commit.
        /// </summary>
        Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes domain events to outbox for eventual delivery.
        /// Called AFTER database transaction commit.
        /// </summary>
        Task PublishToOutboxAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default);
    }
}
