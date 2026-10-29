using System;

namespace HsnSoft.Base.EventBus.RabbitMQ.Configs;

/// <summary>RabbitMQ specific tuning, read from the same section as <see cref="EventBusOptions"/> (<c>RabbitMq:EventBus</c>).</summary>
public sealed class RabbitMqEventBusConfig
{
    /// <summary>How long a message that could not be handled for a transient reason (a timeout) waits before it goes back on its queue.</summary>
    public TimeSpan RequeueDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// How often a message whose handler timed out is put back on its queue before it counts as failed (reported to the error client and taken off the queue). The count is kept by the consumer in memory, so a message
    /// that comes back to another instance starts again; 0 puts it back for ever.
    /// </summary>
    public int MaxTimeoutRequeues { get; set; } = 10;

    /// <summary>How often a consumer checks that its channel is still open.</summary>
    public TimeSpan RecoveryProbeInterval { get; set; } = TimeSpan.FromSeconds(5);
}
