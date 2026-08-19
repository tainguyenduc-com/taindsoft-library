using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;
using TaindSoft.Core.Domain.Dispatchers;
using TaindSoft.Core.Domain.Events;
using TaindSoft.Core.Domain.Outbox;

namespace TaindSoft.Core.Infrastructure.Dispatchers
{
    /// <summary>
    /// Production-safe domain event dispatcher.
    /// Dispatches to in-memory handlers synchronously before commit.
    /// Publishes to outbox for reliable eventual delivery after commit.
    /// </summary>
    /// <summary>
    /// Dispatches domain events collected from aggregate roots to registered handlers.
    /// </summary>
    public class DomainEventDispatcher(
        IServiceProvider serviceProvider,
        ILogger<DomainEventDispatcher> logger) : IDomainEventDispatcher
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        private readonly ILogger<DomainEventDispatcher> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

        public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default)
        {
            if (events == null || !events.Any())
            {
                return;
            }

            foreach (IDomainEvent domainEvent in events)
            {
                Type eventType = domainEvent.GetType();
                _logger.LogDebug("Dispatching domain event {EventType} with ID {EventId}",
                    eventType.Name, domainEvent.EventId);

                try
                {
                    // Resolve handler dynamically: IDomainEventHandler<TEvent>
                    Type handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
                    IEnumerable<object?> handlers = _serviceProvider.GetServices(handlerType);

                    foreach (object? handler in handlers)
                    {
                        MethodInfo? handleMethod = handlerType.GetMethod(nameof(IDomainEventHandler<>.Handle));
                        if (handleMethod != null)
                        {
                            Task task = (Task)handleMethod.Invoke(handler, [domainEvent, cancellationToken])!;
                            await task.ConfigureAwait(false);
                        }
                    }

                    _logger.LogInformation("Domain event {EventType} dispatched successfully", eventType.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to dispatch domain event {EventType}", eventType.Name);
                    // Do NOT throw - continue processing remaining events
                    // Failure in one handler should not break the entire transaction
                }
            }
        }

        public async Task PublishToOutboxAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default)
        {
            if (events == null || !events.Any())
            {
                return;
            }

            try
            {
                IOutboxRepository? outboxRepository = _serviceProvider.GetService<IOutboxRepository>();
                if (outboxRepository == null)
                {
                    _logger.LogWarning("IOutboxRepository not registered - domain events will not be published to outbox");
                    return;
                }

                foreach (IDomainEvent domainEvent in events)
                {
                    OutboxMessage outboxMessage = new()
                    {
                        Type = domainEvent.GetType().AssemblyQualifiedName ?? domainEvent.GetType().FullName ?? "Unknown",
                        Content = JsonSerializer.Serialize(domainEvent, _jsonSerializerOptions),
                        OccurredOnUtc = domainEvent.OccurredOn,
                        ProcessedOnUtc = null,
                        Error = null
                    };

                    await outboxRepository.InsertAsync(outboxMessage, cancellationToken: cancellationToken);

                    _logger.LogDebug("Domain event {EventType} added to outbox with ID {OutboxId}",
                        domainEvent.GetType().Name, outboxMessage.Id);
                }

                _logger.LogInformation("Published {Count} domain events to outbox", events.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish domain events to outbox");
                // Do NOT throw - this is a fire-and-forget operation
                // Events are already processed in-memory, outbox is for eventual consistency
            }
        }
    }
}
