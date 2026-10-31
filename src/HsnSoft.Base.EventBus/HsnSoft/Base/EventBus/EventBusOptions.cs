using System;

namespace HsnSoft.Base.EventBus;

/// <summary>
/// Settings every broker project shares. The defaults are the ones the production services have always used; a product sets its own
/// values in configuration (section <c>RabbitMq:EventBus</c> for the RabbitMQ project).
/// </summary>
public sealed class EventBusOptions
{
    /// <summary>The namespace of the events on the broker: the exchange name. Services that talk to each other use the same one.</summary>
    public string ExchangeName { get; set; } = "HsnSoft";

    /// <summary>The name of this service as a consumer, e.g. "OrderService". It names the queues the service reads.</summary>
    public string ConsumerClientName { get; set; } = "Client";

    /// <summary>The service that manages failed events (e.g. "EventManagerService"); the failed messages of this service go to its queue. Defaults to the consumer's own name.</summary>
    public string? ErrorClientName { get; set; }

    /// <summary>Removed from the start of an event type's name to get the event name: <c>Prefix</c> turns <c>PrefixOrderPlaced</c> into <c>OrderPlaced</c>.</summary>
    public string EventNamePrefix { get; set; } = string.Empty;

    /// <summary>Removed from the end of an event type's name to get the event name: the default turns <c>OrderPlacedEto</c> into <c>OrderPlaced</c>.</summary>
    public string EventNameSuffix { get; set; } = "Eto";

    /// <summary><c>{ExchangeName}_{ConsumerClientName}</c>: who a message comes from, and the start of the names of the queues the service reads.</summary>
    public string ConsumerClientInfo =>
        (string.IsNullOrWhiteSpace(ExchangeName) ? string.Empty : $"{ExchangeName}_")
        + (string.IsNullOrWhiteSpace(ConsumerClientName) ? string.Empty : ConsumerClientName);

    /// <summary><c>{ExchangeName}_{ErrorClientName}</c>: the start of the names of the queues failed messages go to.</summary>
    public string ErrorClientInfo =>
        (string.IsNullOrWhiteSpace(ExchangeName) ? string.Empty : $"{ExchangeName}_")
        + (string.IsNullOrWhiteSpace(ErrorClientName)
            ? string.IsNullOrWhiteSpace(ConsumerClientName) ? string.Empty : ConsumerClientName
            : ErrorClientName);

    /// <summary>How often a handler is tried before the message counts as failed. 1 leaves retrying to the handler.</summary>
    public int MaxHandlerAttempts { get; set; } = 1;

    /// <summary>The pause before handler attempt <c>n + 1</c> is <c>n * RetryDelay</c>.</summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(200);

    /// <summary>How long a consumer that reads many messages itself (Kafka) waits before it asks again for a message another handling holds (<see cref="HsnSoft.Base.EventBus.Inbox.MessageInProgressException"/>). RabbitMQ puts the message back after its own <c>RequeueDelay</c>.</summary>
    public TimeSpan InProgressRetryDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>How often publishing is repeated when the broker is unreachable, after the first try.</summary>
    public int PublishRetryCount { get; set; } = 5;

    /// <summary>The pause before publish retry <c>n</c> is <c>2^n * PublishRetryBaseDelay</c>.</summary>
    public TimeSpan PublishRetryBaseDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>How long a publisher waits for the broker to confirm one message (RabbitMQ publisher confirms); running out of it is a failed try, which is repeated like an unreachable broker.</summary>
    public TimeSpan PublishTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How many messages the service handles at the same moment, whatever their events (every event has its own <c>FetchCount</c>, and a service with many events adds them up). A message that arrives when the limit is reached waits
    /// for a place without holding a thread; it is not lost and the broker still counts it as taken. It keeps what the handlers share (the connections of a database, the threads, a CPU) from being asked for more than they have.
    /// 0 or less: no limit, the sum of the <c>FetchCount</c> of the events.
    /// </summary>
    public int MaxConcurrentHandlers { get; set; }

    /// <summary>How long a stopping service waits for the messages being handled and published to finish.</summary>
    public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Writes the whole message body into the logs (the Debug log of a received message, the log of a dropped failed event).
    /// Off by default: a message may carry personal data, and the console is a log too. Turn it on to debug a service.
    /// </summary>
    public bool LogMessageBody { get; set; }

    /// <summary>
    /// Puts the message that is handled (the envelope with its headers and the event) in the <c>ConsumeStart</c> log as <c>MessageBody</c>, so the
    /// state a handling started in is known. The sensitive data of the message is hidden first (<c>[SensitiveData]</c> and the usual keys); when
    /// no <see cref="HsnSoft.Base.Logging.Masking.ILogMasker"/> is registered the message is not logged. On by default; unlike <see cref="LogMessageBody"/> it is masked.
    /// </summary>
    public bool LogConsumedMessage { get; set; } = true;

    /// <summary>The longest <c>MessageBody</c> of a <c>ConsumeStart</c> log, in characters; a longer message is cut (and says by how much).</summary>
    public int MaxLoggedMessageLength { get; set; } = 8192;
}
