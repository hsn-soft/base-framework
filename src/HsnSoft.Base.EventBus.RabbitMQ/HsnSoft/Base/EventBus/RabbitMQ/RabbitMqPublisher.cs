using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.EventBus.RabbitMQ.Connection;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace HsnSoft.Base.EventBus.RabbitMQ;

/// <summary>The two ways a message is put on the broker, shared by the event bus and the failed event notifier.</summary>
internal static class RabbitMqPublisher
{
    /// <summary>What is worth repeating: the broker or the connection is not there for the moment, or did not confirm the message. A message that is nacked, returned or not confirmed may have reached the queue, so it can be delivered twice.</summary>
    internal static bool IsTransient(Exception exception)
        => exception is BrokerUnreachableException or SocketException or IOException or AlreadyClosedException or PublishException or RabbitMqNotConnectedException or TimeoutException;

    /// <summary>
    /// Runs one publish with confirms: it is given <paramref name="timeout"/> to be confirmed (the broker that never answers would hold the caller for ever), and running out of it is a <see cref="TimeoutException"/>, which is transient.
    /// </summary>
    internal static async Task WithConfirmTimeoutAsync(Func<CancellationToken, Task> publish, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using CancellationTokenSource limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limit.CancelAfter(timeout);
        try
        {
            await publish(limit.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"The broker did not confirm the message within {timeout.TotalSeconds:0.#} s.");
        }
    }

    /// <summary>
    /// To the exchange, with the event name as routing key: every service that bound a queue to the event gets a copy. <c>false</c> when no queue took it (nobody listens to the event yet): an event nobody
    /// subscribed to is not a failure of the publisher, but it is not delivered, and the caller says so.
    /// </summary>
    internal static async Task<bool> PublishToExchangeAsync(IChannel channel, string exchangeName, string eventName, BasicProperties properties, byte[] body, CancellationToken cancellationToken)
    {
        // the exchange is declared with every publish so the first publisher can start before anybody else
        await channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Direct, cancellationToken: cancellationToken).ConfigureAwait(false);
        try
        {
            await channel.BasicPublishAsync(exchangeName, eventName, mandatory: true, properties, body, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (PublishReturnException)
        {
            return false;
        }
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
