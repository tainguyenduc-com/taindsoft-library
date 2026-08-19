using TaindSoft.Core.Domain.Events;

namespace TaindSoft.Core.Domain.Entities
{
    /// <summary>
    /// TODO: Document interface IAggregateRootEntity
    /// </summary>
    public interface IAggregateRootEntity
    {
        IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
        void AddDomainEvent(IDomainEvent domainEvent);
        void ClearDomainEvents();
    }
}
