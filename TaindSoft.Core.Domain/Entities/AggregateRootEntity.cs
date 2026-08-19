using TaindSoft.Core.Domain.Events;

namespace TaindSoft.Core.Domain.Entities
{
    public abstract class AggregateRootEntity : Entity, IAggregateRootEntity
    {
        private readonly List<IDomainEvent> _domainEvents = [];
        public AggregateRootEntity() : base() { }

        /// <summary>
        /// Gets the domain events associated with this aggregate root. These events can be used for eventual consistency, integration events, or other cross-cutting concerns. 
        /// </summary>
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        /// <summary>
        /// Adds a domain event to the aggregate root's event collection. This allows the aggregate to publish events that can be handled by other parts of the system, such as event handlers or message queues.
        /// </summary>
        /// <param name="domainEvent"></param>
        public void AddDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        /// <summary>
        /// Clears all domain events from the aggregate root. This is typically called after the events have been dispatched to ensure that they are not processed multiple times.
        /// </summary>
        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }
}
