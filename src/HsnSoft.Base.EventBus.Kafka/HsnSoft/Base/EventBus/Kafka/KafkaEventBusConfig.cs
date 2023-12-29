namespace HsnSoft.Base.EventBus.Kafka;

public class KafkaEventBusConfig : EventBusConfig
{
    public KafkaProducerConfig KafkaProducerConfig { get; set; } = new();
    public KafkaConsumerConfig KafkaConsumerConfig { get; set; } = new();
}

public class KafkaProducerConfig
{
    public int ReceiveMessageMaxBytes { get; set; } = 50000000;
    public int MessageMaxBytes { get; set; } = 50000000;
}

public class KafkaConsumerConfig
{
    public int SessionTimeoutMs { get; set; } = 90000;
    public int HeartbeatIntervalMs { get; set; } = 30000;
    public int FetchMaxBytes { get; set; } = 50000000;
    public int MaxPartitionFetchBytes { get; set; } = 50000000;
}