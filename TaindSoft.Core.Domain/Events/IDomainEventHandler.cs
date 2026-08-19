namespace TaindSoft.Core.Domain.Events
{
    /// <summary>
    /// Handler for domain events.
    /// Executed in-memory before transaction commit.
    /// </summary>
    public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
    {
        Task Handle(TEvent domainEvent, CancellationToken cancellationToken = default);
    }
}
