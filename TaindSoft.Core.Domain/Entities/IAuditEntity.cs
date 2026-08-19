namespace TaindSoft.Core.Domain.Entities
{
    /// <summary>
    /// TODO: Document interface IAuditEntity
    /// </summary>
    public interface IAuditEntity
    {
        DateTime CreatedAt { get; set; }
        DateTime? UpdatedAt { get; set; }
        string? CreatedBy { get; set; }
        string? UpdatedBy { get; set; }
    }
}
