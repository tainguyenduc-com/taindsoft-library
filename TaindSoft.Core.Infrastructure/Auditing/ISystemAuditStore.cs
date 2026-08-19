namespace TaindSoft.Core.Infrastructure.Auditing
{
    /// <summary>
    /// TODO: Document interface ISystemAuditStore
    /// </summary>
    public interface ISystemAuditStore
    {
        Task SaveAsync(IEnumerable<AuditRequest> logs, CancellationToken cancellationToken = default);
    }
}
