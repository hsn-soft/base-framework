namespace Xyz.Shared.Messaging;

public sealed class RabbitMqOptions
{
    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string UserName { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    public string VirtualHost { get; set; } = "/";

    /// <summary>Topic exchange all events are published to; the routing key is the event type name.</summary>
    public string ExchangeName { get; set; } = "xyz.events";

    /// <summary>How often a handler is tried before the message is moved to the dead-letter queue.</summary>
    public int MaxHandlerAttempts { get; set; } = 3;
}
