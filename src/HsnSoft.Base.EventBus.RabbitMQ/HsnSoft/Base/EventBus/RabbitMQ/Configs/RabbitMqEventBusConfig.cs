using System;

namespace HsnSoft.Base.EventBus.RabbitMQ.Configs;

/// <summary>RabbitMQ specific tuning, read from the same section as <see cref="EventBusOptions"/> (<c>RabbitMq:EventBus</c>).</summary>
public sealed class RabbitMqEventBusConfig
{
    /// <summary>How long a message that could not be handled for a transient reason (a timeout) waits before it goes back on its queue.</summary>
    public TimeSpan RequeueDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>How often a consumer checks that its channel is still open.</summary>
    public TimeSpan RecoveryProbeInterval { get; set; } = TimeSpan.FromSeconds(5);
}
