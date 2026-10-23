using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace HsnSoft.Base.EventBus.RabbitMQ;

public sealed class RabbitMqEventBus(RabbitMqConnection connection, IOptions<RabbitMqOptions> options) : IEventBus
{
    private readonly RabbitMqConnection _connection = connection;
    private readonly RabbitMqOptions _options = options.Value;

    public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent
    {
        string eventName = typeof(TEvent).Name;

        IConnection rabbit = await _connection.GetAsync(cancellationToken).ConfigureAwait(false);
        IChannel channel = await rabbit.CreateChannelAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        await using (channel.ConfigureAwait(false))
        {
            await RabbitMqTopology.DeclareExchangeAsync(channel, _options, cancellationToken).ConfigureAwait(false);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                MessageId = integrationEvent.EventId.ToString(),
                Type = eventName,
            };

            await channel.BasicPublishAsync(
                _options.ExchangeName,
                routingKey: eventName,
                mandatory: false,
                basicProperties: properties,
                body: EventSerializer.Serialize(integrationEvent),
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}
