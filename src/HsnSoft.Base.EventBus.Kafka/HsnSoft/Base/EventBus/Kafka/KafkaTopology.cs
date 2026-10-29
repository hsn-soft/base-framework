using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace HsnSoft.Base.EventBus.Kafka;

internal static class KafkaTopology
{
    /// <summary>The topic of an event: <c>{ExchangeName}_{EventName}</c>, so the products that share a cluster keep to their own exchange name (just the event name when there is none).</summary>
    public static string TopicFor(EventBusOptions options, string eventName) => Named(options, EventNames.Trim(options, eventName));

    /// <summary>The consumer group of a service for an event, named like the RabbitMQ queue of the same service and event: every service reads an event at its own pace, with its own offset.</summary>
    public static string GroupFor(EventBusOptions options, string eventName) => $"{options.ConsumerClientInfo}_{EventNames.Trim(options, eventName)}";

    public static string DeadLetterTopic(EventBusOptions options) => Named(options, "dead-letter");

    private static string Named(EventBusOptions options, string name) => string.IsNullOrWhiteSpace(options.ExchangeName) ? name : $"{options.ExchangeName}_{name}";

    /// <summary>
    /// Creates the topics that do not exist yet; a topic that already exists is fine. A broker that cannot be reached yet (it is starting, or the first address of its name does not answer) is tried again, so a host that starts
    /// beside its broker does not stop on the first refusal.
    /// </summary>
    public static Task EnsureTopicsAsync(KafkaOptions options, IEnumerable<string> topics, CancellationToken cancellationToken = default)
        => RetryAsync(() => CreateTopicsAsync(options, topics), attempts: 10, TimeSpan.FromSeconds(2), cancellationToken);

    // a stopping service (or a caller that gave up) does not wait for the next try
    internal static async Task RetryAsync(Func<Task> action, int attempts, TimeSpan delay, CancellationToken cancellationToken = default)
    {
        for (int attempt = 1; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await action().ConfigureAwait(false);
                return;
            }
            catch (KafkaException) when (attempt < attempts)
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static async Task CreateTopicsAsync(KafkaOptions options, IEnumerable<string> topics)
    {
        using IAdminClient admin = new AdminClientBuilder(options.Apply(new AdminClientConfig())).Build();
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
