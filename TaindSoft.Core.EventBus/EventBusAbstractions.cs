namespace TaindSoft.Core.EventBus;

public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class;
    Task SubscribeAsync<T>(Func<T, Task> handler) where T : class;
}

public interface ISubscriptionManager
{
    void AddSubscription<T, TH>() where T : class where TH : class;
    void RemoveSubscription<T, TH>() where T : class where TH : class;
    bool HasSubscriptionsForEvent<T>() where T : class;
}

public class EventEnvelope<T>
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public string? EventType { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string? TraceId { get; set; }
    public string? CorrelationId { get; set; }
    public T Payload { get; set; } = default!;
}
