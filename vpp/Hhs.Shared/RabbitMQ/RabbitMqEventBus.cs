using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Hhs.Shared.RabbitMQ;

public sealed class RabbitMqEventBus : IEventBus, IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    public RabbitMqEventBus(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password
        };

        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        _channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true).GetAwaiter().GetResult();
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        var eventName = ResolveEventName(@event.GetType());
        var json = JsonSerializer.Serialize(@event, @event.GetType());
        var body = Encoding.UTF8.GetBytes(json);

        var props = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            Type = eventName
        };

        await _channel.BasicPublishAsync(
            exchange: _options.ExchangeName,
            routingKey: eventName,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private static string ResolveEventName(Type eventType)
    {
        var name = eventType.Name;

        return name.EndsWith("Event")
            ? name[..^"Event".Length]
            : name;
    }
}