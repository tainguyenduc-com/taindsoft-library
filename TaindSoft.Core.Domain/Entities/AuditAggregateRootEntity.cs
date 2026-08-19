namespace TaindSoft.Core.Domain.Entities
{
    /// <summary>
    /// Base class for audit-enabled aggregate roots.
    /// Provides audit timestamps and user tracking.
    /// </summary>
    public abstract class AuditAggregateRootEntity : AggregateRootEntity, IAuditEntity
    {
        public AuditAggregateRootEntity() : base() { }
        /// <summary>
        /// The date and time when the entity was created (in UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The date and time when the entity was last updated (in UTC).
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// The identifier of the user who created the entity.
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// The identifier of the user who last updated the entity.
        /// </summary>
        public string? UpdatedBy { get; set; }
    }
}
