using System;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus;

/// <summary>What a broker project is asked to send.</summary>
/// <param name="EventName">The event name without prefix and suffix; the routing key of the message.</param>
/// <param name="Message">The message (a <see cref="ReQueuedEto"/> tells the broker project which consumer it goes to).</param>
/// <param name="Envelope">The envelope the body was made from.</param>
/// <param name="Body">The message on the wire.</param>
/// <param name="IsExchangeEvent">To everyone who listens, or straight to this service's own queue.</param>
/// <param name="IsReQueuePublish">A failed message put back to one consumer.</param>
public sealed record PublishRequest(
    string EventName,
    IIntegrationEventMessage Message,
    ParentMessageEnvelope Envelope,
    byte[] Body,
    bool IsExchangeEvent,
    bool IsReQueuePublish);

/// <summary>
/// The broker-independent half of publishing: envelope, serialization, retry when the broker is unreachable, logging. A broker project
/// derives from it and only sends the bytes.
/// </summary>
public abstract partial class EventBusBase(EventEnvelopeFactory envelopeFactory, IOptions<EventBusOptions> options, ILogger logger) : IEventBus
{
    private readonly EventEnvelopeFactory _envelopeFactory = envelopeFactory;
    private readonly ILogger _logger = logger;

    protected EventBusOptions Settings { get; } = options.Value;

    public async Task PublishAsync<TEventMessage>(
        TEventMessage eventMessage,
        ParentMessageEnvelope? parentMessage = null,
        string? correlationId = null,
        bool isExchangeEvent = true,
        bool isReQueuePublish = false,
        CancellationToken cancellationToken = default)
        where TEventMessage : IIntegrationEventMessage
    {
        ArgumentNullException.ThrowIfNull(eventMessage);

        MessageEnvelope<TEventMessage> envelope = _envelopeFactory.Create(eventMessage, parentMessage, correlationId, isReQueuePublish);
        PublishRequest request = new(
            EventNames.For(Settings, eventMessage.GetType()),
            eventMessage,
            envelope,
            EventSerializer.Serialize(envelope),
            isExchangeEvent,
            isReQueuePublish);

        LogPublishing(_logger, request.EventName, envelope.MessageId);
        await PublishRetry.ExecuteAsync(
            () => SendAsync(request, cancellationToken),
            Settings.PublishRetryCount,
            Settings.PublishRetryBaseDelay,
            IsTransient,
            (exception, delay, retry) => LogPublishRetry(_logger, exception, request.EventName, retry, delay.TotalSeconds),
            cancellationToken).ConfigureAwait(false);
        LogPublished(_logger, request.EventName, envelope.MessageId);
    }

    /// <summary>Sends the message to the broker.</summary>
    protected abstract Task SendAsync(PublishRequest request, CancellationToken cancellationToken);

    /// <summary>Tells whether a failure of <see cref="SendAsync"/> is worth another try (the broker was not reachable). Nothing is, by default.</summary>
    protected virtual bool IsTransient(Exception exception) => false;

    [LoggerMessage(EventId = 1000, EventName = "PRODUCE_EVENT_STARTED", Level = LogLevel.Debug, Message = "Publishing {EventName}, message {MessageId}")]
    private static partial void LogPublishing(ILogger logger, string eventName, Guid messageId);

    [LoggerMessage(EventId = 1001, EventName = "PRODUCE_EVENT_COMPLETED", Level = LogLevel.Debug, Message = "Published {EventName}, message {MessageId}")]
    private static partial void LogPublished(ILogger logger, string eventName, Guid messageId);

    [LoggerMessage(EventId = 1002, EventName = "PRODUCE_EVENT_ERROR", Level = LogLevel.Warning, Message = "Publishing {EventName} failed, retry {Retry} after {DelaySeconds} s")]
    private static partial void LogPublishRetry(ILogger logger, Exception exception, string eventName, int retry, double delaySeconds);
}
