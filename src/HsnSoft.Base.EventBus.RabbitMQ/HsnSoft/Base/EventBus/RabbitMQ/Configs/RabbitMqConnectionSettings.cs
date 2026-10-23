using System;

namespace HsnSoft.Base.EventBus.RabbitMQ.Configs;

/// <summary>How to reach the broker; section <c>RabbitMq:Connection</c>.</summary>
public sealed class RabbitMqConnectionSettings
{
    public string HostName { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string UserName { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    public string VirtualHost { get; set; } = "/";

    /// <summary>How often connecting is repeated while the broker is not reachable, after the first try.</summary>
    public int ConnectionRetryCount { get; set; } = 5;

    /// <summary>The pause before connection retry <c>n</c> is <c>2^n * ConnectionRetryBaseDelay</c>.</summary>
    public TimeSpan ConnectionRetryBaseDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>How long closing the connection may take (it waits for the callbacks still running) before it is given up.</summary>
    public TimeSpan ConnectionCloseTimeout { get; set; } = TimeSpan.FromSeconds(10);
}
