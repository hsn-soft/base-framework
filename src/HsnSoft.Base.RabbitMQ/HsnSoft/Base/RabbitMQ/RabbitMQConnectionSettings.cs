namespace HsnSoft.Base.RabbitMQ;

public sealed class RabbitMQConnectionSettings
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    public int ConnectionRetryCount { get; set; } = 5;
}