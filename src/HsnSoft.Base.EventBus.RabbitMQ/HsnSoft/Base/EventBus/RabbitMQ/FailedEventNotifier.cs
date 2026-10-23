using System;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus.RabbitMQ.Connection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace HsnSoft.Base.EventBus.RabbitMQ;

/// <summary>
/// Publishes a <see cref="FailedEto"/> to the queue of the error client (<c>{ExchangeName}_{ErrorClientName}_{EventName}</c>). It is
/// separate from the event bus so the consumer can report a failure even when it has no handler for the message.
/// </summary>
public sealed partial class FailedEventNotifier(
    IRabbitMqPersistentConnection connection,
    IEventBusSubscriptionManager subscriptions,
    EventEnvelopeFactory envelopeFactory,
    IOptions<EventBusOptions> options,
    ILogger<FailedEventNotifier> logger) : IFailedEventNotifier
{
    private readonly IRabbitMqPersistentConnection _connection = connection;
    private readonly IEventBusSubscriptionManager _subscriptions = subscriptions;
    private readonly EventEnvelopeFactory _envelopeFactory = envelopeFactory;
    private readonly EventBusOptions _options = options.Value;
    private readonly ILogger<FailedEventNotifier> _logger = logger;

    public async Task NotifyAsync(string errorMessage, string failedEventName, string failedMessageContent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(errorMessage);
        ArgumentNullException.ThrowIfNull(failedEventName);
        ArgumentNullException.ThrowIfNull(failedMessageContent);

        ParentMessageEnvelope? failedEnvelope = null;
        Type? eventType = null;
        object? failedMessage = null;
        try
        {
            byte[] body = Encoding.UTF8.GetBytes(failedMessageContent);
            failedEnvelope = EventSerializer.DeserializeParent(body);
            eventType = _subscriptions.GetEventInfoByName(failedEventName)?.EventType
                        ?? throw new InvalidOperationException($"No event type is registered for '{failedEventName}'.");
            object envelope = EventSerializer.DeserializeEnvelope(body, eventType);
            failedMessage = envelope.GetType().GetProperty(nameof(MessageEnvelope<>.Message), BindingFlags.Public | BindingFlags.Instance)!.GetValue(envelope);
        }
        catch (Exception exception)
        {
            errorMessage += ". FailedMessageContent convert operation error: " + exception.Message;
        }

        await PublishFailedAsync(errorMessage, failedEnvelope, eventType?.Name, failedMessage, cancellationToken).ConfigureAwait(false);
    }

    public Task NotifyDirectAsync(string errorMessage, string? failedMessageTypeName, object? failedMessageObject, ParentMessageEnvelope? parentContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(errorMessage);

        return PublishFailedAsync(errorMessage, parentContext, failedMessageTypeName, failedMessageObject, cancellationToken);
    }

    private async Task PublishFailedAsync(string errorMessage, ParentMessageEnvelope? failedEnvelope, string? failedTypeName, object? failedMessage, CancellationToken cancellationToken)
    {
        if (!_connection.IsConnected)
        {
            await _connection.TryConnectAsync(cancellationToken).ConfigureAwait(false);
        }

        if (!_connection.IsConnected)
        {
            throw new InvalidOperationException("RabbitMQ connection could not be established.");
        }

        FailedEto failed = new(errorMessage, failedEnvelope?.MessageTime.ToUniversalTime(), failedMessage, failedTypeName);
        MessageEnvelope<FailedEto> envelope = _envelopeFactory.Create(failed, failedEnvelope, null, isReQueuePublish: false);
        byte[] body = EventSerializer.Serialize(envelope);
        string eventName = EventNames.For(_options, typeof(FailedEto));
        string queueName = EventNameHelper.GetErrorEventQueueName(_options, eventName);

        LogNotifying(_logger, queueName, envelope.MessageId);
        await PublishRetry.ExecuteAsync(
            async () =>
            {
                IChannel channel = await _connection.CreateModelAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                await using (channel.ConfigureAwait(false))
                {
                    await RabbitMqPublisher.PublishToQueueAsync(channel, queueName, RabbitMqPublisher.Properties(envelope.MessageId, envelope.CorrelationId), body, cancellationToken).ConfigureAwait(false);
                }
            },
            _options.PublishRetryCount,
            _options.PublishRetryBaseDelay,
            exception => exception is BrokerUnreachableException or SocketException,
            (exception, delay, retry) => LogNotifyRetry(_logger, exception, queueName, retry, delay.TotalSeconds),
            cancellationToken).ConfigureAwait(false);
        LogNotified(_logger, queueName, envelope.MessageId);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Reporting a failed message to {Queue}, message {MessageId}")]
    private static partial void LogNotifying(ILogger logger, string queue, Guid messageId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Reported a failed message to {Queue}, message {MessageId}")]
    private static partial void LogNotified(ILogger logger, string queue, Guid messageId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Could not report the failed message to {Queue}: retry {Retry} after {DelaySeconds} s")]
    private static partial void LogNotifyRetry(ILogger logger, Exception exception, string queue, int retry, double delaySeconds);
}
