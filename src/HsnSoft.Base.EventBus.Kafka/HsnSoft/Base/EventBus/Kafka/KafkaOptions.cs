using System;

namespace HsnSoft.Base.EventBus.Kafka;

/// <summary>Connection and topology settings; section <c>Kafka</c>.</summary>
public sealed class KafkaOptions
{
    public string BootstrapServers { get; set; } = "localhost:9092";

    /// <summary>How long a consumer that failed on its own (the broker could not be read, an offset could not be committed) waits before it starts again.</summary>
    public TimeSpan RecoveryDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Partitions of a topic the library has to create.</summary>
    public int Partitions { get; set; } = 1;

    /// <summary>Replication factor of a topic the library has to create.</summary>
    public short ReplicationFactor { get; set; } = 1;
}
