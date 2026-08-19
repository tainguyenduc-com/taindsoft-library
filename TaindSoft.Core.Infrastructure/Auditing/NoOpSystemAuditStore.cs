namespace TaindSoft.Core.Infrastructure.Auditing
{
    /// <summary>
    /// TODO: Document class NoOpSystemAuditStore
    /// </summary>
    public class NoOpSystemAuditStore : ISystemAuditStore
    {
        public Task SaveAsync(IEnumerable<AuditRequest> logs, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
