namespace Hhs.Shared.RabbitMQ;

/// <summary>
/// RabbitMQ connection options.
/// Development defaults: localhost:5672 with guest credentials.
/// These should be overridden via appsettings.json or environment variables in production.
/// </summary>
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    /// <summary>
    /// RabbitMQ server hostname. Default: localhost (development only)
    /// </summary>
    public string HostName { get; set; } = "localhost";

    /// <summary>
    /// RabbitMQ server port. Default: 5672 (standard AMQP port)
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Username for RabbitMQ authentication. Default: guest (development only)
    /// </summary>
    public string UserName { get; set; } = "guest";

    /// <summary>
    /// Password for RabbitMQ authentication. Default: guest (development only)
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// RabbitMQ exchange name for publishing integration events.
    /// Default: hhs.saga.exchange
    /// </summary>
    public string ExchangeName { get; set; } = "hhs.saga.exchange";
}
