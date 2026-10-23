using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus.Kafka;

public sealed class KafkaEventBus : IEventBus, IDisposable
{
    private readonly KafkaOptions _options;
    private readonly Lazy<IProducer<string, byte[]>> _producer;
    private readonly ConcurrentDictionary<string, bool> _knownTopics = new();

    public KafkaEventBus(IOptions<KafkaOptions> options)
    {
        _options = options.Value;
        _producer = new Lazy<IProducer<string, byte[]>>(() => new ProducerBuilder<string, byte[]>(new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
        }).Build());
    }

    public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent
    {
        string topic = KafkaTopology.TopicFor(_options, typeof(TEvent));
        if (!_knownTopics.ContainsKey(topic))
        {
            await KafkaTopology.EnsureTopicsAsync(_options, [topic]).ConfigureAwait(false);
            _knownTopics[topic] = true;
        }

        var message = new Message<string, byte[]>
        {
            Key = integrationEvent.EventId.ToString(),
            Value = EventSerializer.Serialize(integrationEvent),
        };
        await _producer.Value.ProduceAsync(topic, message, cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_producer.IsValueCreated)
        {
            _producer.Value.Flush(TimeSpan.FromSeconds(5));
            _producer.Value.Dispose();
        }
    }
}
