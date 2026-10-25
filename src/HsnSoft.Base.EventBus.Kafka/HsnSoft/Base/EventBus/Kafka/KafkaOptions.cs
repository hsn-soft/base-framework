namespace HsnSoft.Base.EventBus.Kafka;

/// <summary>Connection and topology settings; section <c>Kafka</c>.</summary>
public sealed class KafkaOptions
{
    public string BootstrapServers { get; set; } = "localhost:9092";

    /// <summary>Partitions of a topic the library has to create.</summary>
    public int Partitions { get; set; } = 1;

    /// <summary>Replication factor of a topic the library has to create.</summary>
    public short ReplicationFactor { get; set; } = 1;
}
