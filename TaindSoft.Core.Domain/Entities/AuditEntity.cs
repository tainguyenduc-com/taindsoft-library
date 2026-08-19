namespace TaindSoft.Core.Domain.Entities
{
    public abstract class AuditEntity : Entity, IAuditEntity
    {
        protected AuditEntity() { }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
