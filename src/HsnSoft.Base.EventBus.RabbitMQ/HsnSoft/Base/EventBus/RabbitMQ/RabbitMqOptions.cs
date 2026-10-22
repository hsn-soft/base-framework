namespace HsnSoft.Base.EventBus.RabbitMQ;

/// <summary>Connection and topology settings; section <c>RabbitMq</c>.</summary>
public sealed class RabbitMqOptions
{
    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string UserName { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// The topic exchange all events are published to; the routing key is the event type name. Products that share a broker
    /// use their own exchange name (e.g. "myproduct.events").
    /// </summary>
    public string ExchangeName { get; set; } = "hsnsoft.events";
}
