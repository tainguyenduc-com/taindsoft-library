using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using TaindSoft.Core.EventBus;

namespace TaindSoft.Core.EventBus.RabbitMQ;

public static class EventBusRabbitMqServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMqEventBus(this IServiceCollection services, Action<RabbitMqEventBusOptions> configure)
    {
        services.Configure(configure);
        services.AddSingleton<IEventBus, RabbitMqEventBus>();
        // ponytail: concrete impl only for RabbitMQ, add Kafka/Azure variants as adapters
        return services;
    }
}

public class RabbitMqEventBusOptions
{
    public required string HostName { get; set; }
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string? Exchange { get; set; }
}

public class RabbitMqEventBus : IEventBus, IAsyncDisposable
{
    private readonly RabbitMqEventBusOptions _options;
    private readonly ILogger<RabbitMqEventBus> _logger;
    private readonly ConnectionFactory _factory;
    private IConnection? _connection;
    private IChannel? _publishChannel;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly string _exchangeName;

    public RabbitMqEventBus(IOptions<RabbitMqEventBusOptions> options, ILogger<RabbitMqEventBus> logger)
    {
        _options = options.Value;
        _logger = logger;
        _exchangeName = _options.Exchange ?? "taindsoft.events";
        _factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            AutomaticRecoveryEnabled = true
        };
    }

    private async Task<IConnection> GetConnectionAsync(CancellationToken ct = default)
    {
        if (_connection != null && _connection.IsOpen) return _connection;

        await _connectionLock.WaitAsync(ct);
        try
        {
            if (_connection == null || !_connection.IsOpen)
            {
                _connection = await _factory.CreateConnectionAsync(ct);
                _logger.LogInformation("RabbitMQ Connection established to {Host}", _options.HostName);
            }
            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async Task<IChannel> GetPublishChannelAsync(CancellationToken ct = default)
    {
        if (_publishChannel != null && _publishChannel.IsOpen) return _publishChannel;

        var connection = await GetConnectionAsync(ct);
        _publishChannel = await connection.CreateChannelAsync(cancellationToken: ct);
        await _publishChannel.ExchangeDeclareAsync(_exchangeName, ExchangeType.Topic, durable: true, cancellationToken: ct);
        return _publishChannel;
    }

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var channel = await GetPublishChannelAsync(cancellationToken);
            var routingKey = typeof(T).FullName ?? typeof(T).Name;
            
            var envelope = new EventEnvelope<T> { Payload = @event, EventType = routingKey };
            var json = JsonSerializer.Serialize(envelope, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var body = Encoding.UTF8.GetBytes(json);
            
            var props = new BasicProperties { Persistent = true };
            
            await channel.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: props,
                body: body,
                cancellationToken: cancellationToken);

            _logger.LogDebug("Published event {EventName} with routing key {RoutingKey}", typeof(T).Name, routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing event {EventName}", typeof(T).Name);
            throw;
        }
    }

    public async Task SubscribeAsync<T>(Func<T, Task> handler) where T : class
    {
        try
        {
            var connection = await GetConnectionAsync();
            var channel = await connection.CreateChannelAsync();
            
            var routingKey = typeof(T).FullName ?? typeof(T).Name;
            var queueName = $"{_exchangeName}.{typeof(T).Name}";

            await channel.ExchangeDeclareAsync(_exchangeName, ExchangeType.Topic, durable: true);
            await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false);
            await channel.QueueBindAsync(queueName, _exchangeName, routingKey);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (sender, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    
                    var envelope = JsonSerializer.Deserialize<EventEnvelope<T>>(message, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                    if (envelope?.Payload != null)
                    {
                        await handler(envelope.Payload);
                    }
                    
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing event {EventName} from queue {QueueName}", typeof(T).Name, queueName);
                    // ponytail: basic retry/DLQ mechanism omitted; nack without requeue to avoid poison loops.
                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            await channel.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer);
            _logger.LogInformation("Subscribed to {EventName} on queue {QueueName}", typeof(T).Name, queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to event {EventName}", typeof(T).Name);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_publishChannel != null) await _publishChannel.DisposeAsync();
        if (_connection != null) await _connection.DisposeAsync();
        _connectionLock.Dispose();
    }
}
