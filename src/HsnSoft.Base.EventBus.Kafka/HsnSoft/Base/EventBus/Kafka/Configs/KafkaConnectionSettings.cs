namespace HsnSoft.Base.EventBus.Kafka.Configs;

public sealed class KafkaConnectionSettings
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 9092;
}