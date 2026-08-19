namespace TaindSoft.Core.Infrastructure.Auditing
{
    /// <summary>
    /// TODO: Document class AuditRequest
    /// </summary>
    public class AuditRequest
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public DateTime ExecutionTime { get; set; } = DateTime.UtcNow;
    }
}
