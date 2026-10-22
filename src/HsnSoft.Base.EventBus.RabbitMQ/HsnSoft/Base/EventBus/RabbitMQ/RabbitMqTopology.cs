using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace HsnSoft.Base.EventBus.RabbitMQ;

/// <summary>The exchange layout shared by the publisher and the subscriber.</summary>
internal static class RabbitMqTopology
{
    public static string DeadLetterExchange(RabbitMqOptions options) => options.ExchangeName + ".dlx";

    public static string DeadLetterQueue(RabbitMqOptions options) => options.ExchangeName + ".dead-letter";

    public static Task DeclareExchangeAsync(IChannel channel, RabbitMqOptions options, CancellationToken cancellationToken)
        => channel.ExchangeDeclareAsync(options.ExchangeName, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);

    public static async Task DeclareDeadLetterAsync(IChannel channel, RabbitMqOptions options, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(DeadLetterExchange(options), ExchangeType.Fanout, durable: true, cancellationToken: cancellationToken).ConfigureAwait(false);
        await channel.QueueDeclareAsync(DeadLetterQueue(options), durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken).ConfigureAwait(false);
        await channel.QueueBindAsync(DeadLetterQueue(options), DeadLetterExchange(options), routingKey: string.Empty, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
