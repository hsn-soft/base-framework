using RabbitMQ.Client;

namespace Xyz.Shared.Messaging;

/// <summary>The exchange layout shared by publishers and subscribers.</summary>
internal static class RabbitMqTopology
{
    public const string DeadLetterQueue = "xyz.dead-letter";

    public static string DeadLetterExchange(RabbitMqOptions options) => options.ExchangeName + ".dlx";

    public static async Task DeclareExchangeAsync(IChannel channel, RabbitMqOptions options, CancellationToken cancellationToken)
        => await channel.ExchangeDeclareAsync(options.ExchangeName, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);

    public static async Task DeclareDeadLetterAsync(IChannel channel, RabbitMqOptions options, CancellationToken cancellationToken)
    {
        string exchange = DeadLetterExchange(options);
        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Fanout, durable: true, cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(DeadLetterQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(DeadLetterQueue, exchange, routingKey: string.Empty, cancellationToken: cancellationToken);
    }
}
