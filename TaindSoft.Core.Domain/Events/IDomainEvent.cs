namespace TaindSoft.Core.Domain.Events
{
    /// <summary>
    /// Base interface for all domain events.
    /// Domain events represent something that happened in the domain.
    /// </summary>
    public interface IDomainEvent
    {
        /// <summary>
        /// Unique identifier for the event.
        /// </summary>
        Guid EventId { get; }

        /// <summary>
        /// When the event occurred (UTC).
        /// </summary>
        DateTime OccurredOn { get; }
    }
}
