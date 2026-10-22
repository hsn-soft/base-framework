using System;

namespace HsnSoft.Base.EventBus;

/// <summary>Settings that do not depend on the broker.</summary>
public sealed class EventBusOptions
{
    /// <summary>
    /// The logical name of the running service, e.g. "order-service". Brokers use it to name the service's own queue or
    /// consumer group, so two services that listen to the same event each get a copy of it.
    /// </summary>
    public string ServiceName { get; set; } = "service";

    /// <summary>How often a handler is tried before the message is given up on (and moved to a dead-letter destination).</summary>
    public int MaxHandlerAttempts { get; set; } = 3;

    /// <summary>The pause before attempt <c>n + 1</c> is <c>n * RetryDelay</c>.</summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(200);
}
