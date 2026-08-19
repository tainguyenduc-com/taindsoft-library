namespace TaindSoft.Core.Domain.Entities
{
    /// <summary>
    /// Compatibility shim — deprecated. Use <see cref="AuditAggregateRootEntity"/> instead.
    /// </summary>
    [Obsolete("AggregateRoot is deprecated. Use AuditAggregateRootEntity instead.")]
    public abstract class AggregateRoot : AuditAggregateRootEntity
    {
        protected AggregateRoot() { }

        // AggregateRoot previously exposed domain events and audit properties directly.
        // Those members now live on AuditAggregateRootEntity/IAggregateRootEntity/IAuditEntity.
    }
}
