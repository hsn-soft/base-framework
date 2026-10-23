using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace HsnSoft.Base.EventBus.RabbitMQ;

/// <summary>The two ways a message is put on the broker, shared by the event bus and the failed event notifier.</summary>
internal static class RabbitMqPublisher
{
    /// <summary>To the exchange, with the event name as routing key: every service that bound a queue to the event gets a copy.</summary>
    internal static async Task PublishToExchangeAsync(IChannel channel, string exchangeName, string eventName, BasicProperties properties, byte[] body, CancellationToken cancellationToken)
    {
        // the exchange is declared with every publish so the first publisher can start before anybody else
        await channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Direct, cancellationToken: cancellationToken).ConfigureAwait(false);
        await channel.BasicPublishAsync(exchangeName, eventName, mandatory: true, properties, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Straight to one queue through the default exchange; the queue is created if it does not exist.</summary>
    internal static async Task PublishToQueueAsync(IChannel channel, string queueName, BasicProperties properties, byte[] body, CancellationToken cancellationToken)
    {
        await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: cancellationToken).ConfigureAwait(false);
        await channel.BasicPublishAsync(string.Empty, queueName, mandatory: true, properties, body, cancellationToken).ConfigureAwait(false);
    }

    internal static BasicProperties Properties(System.Guid messageId, string? correlationId)
        => new()
        {
            DeliveryMode = DeliveryModes.Persistent,
            ContentType = "application/json",
            MessageId = messageId.ToString(),
            CorrelationId = correlationId,
        };
}
