namespace HsnSoft.Base.EventBus.RabbitMQ.Configs;

public sealed class RabbitMqConnectionSettings
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    public string VirtualHost { get; set; } = "/";

    public int ConnectionRetryCount { get; set; } = 5;
}