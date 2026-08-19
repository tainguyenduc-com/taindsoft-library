using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TaindSoft.Core.Host.BackgroundTasks
{
    /// <summary>
    /// TODO: Document class QueuedHostedService
    /// </summary>
    public class QueuedHostedService(IBackgroundTaskQueue taskQueue, ILogger<QueuedHostedService> logger) : BackgroundService
    {
        private readonly IBackgroundTaskQueue _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
        private readonly ILogger<QueuedHostedService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    Func<CancellationToken, ValueTask>? workItem = await _taskQueue.DequeueAsync(stoppingToken).ConfigureAwait(false);
                    if (workItem == null)
                    {
                        continue;
                    }

                    await workItem(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing background work item.");
                }
            }
        }
    }
}
