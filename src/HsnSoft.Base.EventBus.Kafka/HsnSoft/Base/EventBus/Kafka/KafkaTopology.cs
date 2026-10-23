using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace HsnSoft.Base.EventBus.Kafka;

internal static class KafkaTopology
{
    public static string TopicFor(KafkaOptions options, string eventName) => options.TopicPrefix + eventName;

    public static string DeadLetterTopic(KafkaOptions options) => options.TopicPrefix + "dead-letter";

    /// <summary>Creates the topics that do not exist yet; a topic that already exists is fine.</summary>
    public static async Task EnsureTopicsAsync(KafkaOptions options, IEnumerable<string> topics)
    {
        using IAdminClient admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = options.BootstrapServers }).Build();
        IEnumerable<TopicSpecification> specifications = topics.Distinct().Select(topic => new TopicSpecification
        {
            Name = topic,
            NumPartitions = options.Partitions,
            ReplicationFactor = options.ReplicationFactor,
        });

        try
        {
            await admin.CreateTopicsAsync(specifications).ConfigureAwait(false);
        }
        catch (CreateTopicsException exception) when (exception.Results.All(result => result.Error.Code is ErrorCode.NoError or ErrorCode.TopicAlreadyExists))
        {
            // all of them exist now
        }
    }
}
