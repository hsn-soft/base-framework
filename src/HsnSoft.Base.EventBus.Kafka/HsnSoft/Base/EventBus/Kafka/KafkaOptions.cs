namespace HsnSoft.Base.EventBus.Kafka;

/// <summary>Connection and topology settings; section <c>Kafka</c>.</summary>
public sealed class KafkaOptions
{
    public string BootstrapServers { get; set; } = "localhost:9092";

    /// <summary>
    /// Put before every topic name: an event goes to <c>{TopicPrefix}{EventName}</c>. Products that share a cluster use their
    /// own prefix (e.g. "myproduct.").
    /// </summary>
    public string TopicPrefix { get; set; } = "hsnsoft.";

    /// <summary>Partitions of a topic the library has to create.</summary>
    public int Partitions { get; set; } = 1;

    /// <summary>Replication factor of a topic the library has to create.</summary>
    public short ReplicationFactor { get; set; } = 1;
}
