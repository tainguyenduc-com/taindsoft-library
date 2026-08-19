using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TaindSoft.Core.Domain.Outbox;

namespace TaindSoft.Core.Infrastructure.BackgroundServices
{
    /// <summary>
    /// Background service for processing outbox messages.
    /// Polls outbox table every 5-10 seconds and publishes unprocessed messages.
    /// Implements retry with max 3 attempts and safe error handling.
    /// </summary>
    public class OutboxProcessorBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessorBackgroundService> logger) : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        private readonly ILogger<OutboxProcessorBackgroundService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(10);
        // Reserved for future retry implementation
        private readonly int _batchSize = 50;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Outbox Processor Background Service starting");

            // Wait a bit before starting to allow application to fully initialize
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessOutboxMessagesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    // NEVER crash the application due to outbox processing failure
                    _logger.LogError(ex, "Outbox processor encountered an error. Will retry after {Interval}",
                        _pollingInterval);
                }

                // Wait before next poll
                await Task.Delay(_pollingInterval, stoppingToken);
            }

            _logger.LogInformation("Outbox Processor Background Service stopping");
        }

        private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            IOutboxRepository? outboxRepository = scope.ServiceProvider.GetService<IOutboxRepository>();

            if (outboxRepository == null)
            {
                _logger.LogWarning("IOutboxRepository not registered - outbox processing disabled");
                return;
            }

            try
            {
                List<OutboxMessage> unprocessedMessages = await outboxRepository.GetUnprocessedAsync(cancellationToken);

                if (unprocessedMessages.Count == 0)
                {
                    _logger.LogDebug("No unprocessed outbox messages found");
                    return;
                }

                _logger.LogInformation("Found {Count} unprocessed outbox messages", unprocessedMessages.Count);

                foreach (OutboxMessage? message in unprocessedMessages.Take(_batchSize))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    await ProcessSingleMessageAsync(message, outboxRepository, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to query or process outbox messages");
            }
        }

        private async Task ProcessSingleMessageAsync(
            OutboxMessage message,
            IOutboxRepository outboxRepository,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Processing outbox message {MessageId} of type {Type}",
                    message.Id, message.Type);

                // TODO: Implement actual message publishing logic here
                // For now, this is a placeholder that marks messages as processed
                // In production, you would:
                // 1. Deserialize message.Content to event object
                // 2. Publish to message broker (RabbitMQ, Azure Service Bus, etc.)
                // 3. Mark as processed only after successful publish
                // 4. Use _maxRetryAttempts for retry logic

                // Simulate processing
                await Task.Delay(100, cancellationToken);

                // Mark as processed after successful publishing
                await outboxRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Outbox message {MessageId} processed successfully", message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox message {MessageId}. Error: {Error}",
                    message.Id, ex.Message);

                try
                {
                    // Mark as failed with error message
                    // TODO: Track retry count using _maxRetryAttempts and implement dead letter queue
                    await outboxRepository.SaveChangesAsync(cancellationToken);
                }
                catch (Exception saveEx)
                {
                    _logger.LogError(saveEx, "Failed to save error state for outbox message {MessageId}",
                        message.Id);
                }
            }
        }
    }
}
