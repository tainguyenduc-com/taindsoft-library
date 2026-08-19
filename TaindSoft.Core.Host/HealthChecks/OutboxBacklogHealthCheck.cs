using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using TaindSoft.Core.Domain.Outbox;

namespace TaindSoft.Core.Host.HealthChecks
{
    /// <summary>
    /// Health check for outbox message backlog.
    /// Reports Degraded if backlog exceeds threshold.
    /// </summary>
    public class OutboxBacklogHealthCheck(
        IOutboxRepository? outboxRepository,
        ILogger<OutboxBacklogHealthCheck> logger) : IHealthCheck
    {
        private readonly IOutboxRepository? _outboxRepository = outboxRepository;
        private readonly ILogger<OutboxBacklogHealthCheck> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private const int BacklogWarningThreshold = 1000;
        private const int BacklogCriticalThreshold = 10000;

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            if (_outboxRepository == null)
            {
                return HealthCheckResult.Healthy("Outbox repository not configured");
            }

            try
            {
                List<OutboxMessage> unprocessedMessages = await _outboxRepository.GetUnprocessedAsync(cancellationToken);
                int backlogCount = unprocessedMessages.Count;

                if (backlogCount >= BacklogCriticalThreshold)
                {
                    _logger.LogWarning("Outbox backlog critical: {BacklogCount} messages", backlogCount);
                    return HealthCheckResult.Unhealthy(
                        $"Outbox backlog critical: {backlogCount} unprocessed messages (threshold: {BacklogCriticalThreshold})");
                }

                if (backlogCount >= BacklogWarningThreshold)
                {
                    _logger.LogWarning("Outbox backlog high: {BacklogCount} messages", backlogCount);
                    return HealthCheckResult.Degraded(
                        $"Outbox backlog high: {backlogCount} unprocessed messages (threshold: {BacklogWarningThreshold})");
                }

                return HealthCheckResult.Healthy($"Outbox backlog normal: {backlogCount} messages");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox health check failed");
                return HealthCheckResult.Unhealthy("Outbox health check failed", ex);
            }
        }
    }
}
