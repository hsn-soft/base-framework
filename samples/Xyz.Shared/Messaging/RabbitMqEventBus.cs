using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Xyz.Shared.Events;

namespace Xyz.Shared.Messaging;

public sealed class RabbitMqEventBus(RabbitMqConnection connection, IOptions<RabbitMqOptions> options) : IEventBus
{
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent
    {
        var settings = options.Value;
        string eventName = typeof(TEvent).Name;

        var rabbit = await connection.GetAsync(cancellationToken);
        await using var channel = await rabbit.CreateChannelAsync(cancellationToken: cancellationToken);
        await RabbitMqTopology.DeclareExchangeAsync(channel, settings, cancellationToken);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = @event.EventId.ToString(),
            Type = eventName,
        };

        await channel.BasicPublishAsync(
            settings.ExchangeName,
            routingKey: eventName,
            mandatory: false,
            basicProperties: properties,
            body: JsonSerializer.SerializeToUtf8Bytes(@event, JsonSerializerOptions.Web),
            cancellationToken: cancellationToken);
    }
}
