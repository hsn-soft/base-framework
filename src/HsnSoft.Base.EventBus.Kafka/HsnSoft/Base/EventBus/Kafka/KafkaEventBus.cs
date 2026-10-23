using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus.Kafka;

public sealed class KafkaEventBus : EventBusBase, IDisposable
{
    private readonly KafkaOptions _kafkaOptions;
    private readonly Lazy<IProducer<string, byte[]>> _producer;
    private readonly ConcurrentDictionary<string, bool> _knownTopics = new();

    public KafkaEventBus(
        IOptions<KafkaOptions> kafkaOptions,
        EventEnvelopeFactory envelopeFactory,
        IOptions<EventBusOptions> options,
        ILogger<KafkaEventBus> logger)
        : base(envelopeFactory, options, logger)
    {
        _kafkaOptions = kafkaOptions.Value;
        _producer = new Lazy<IProducer<string, byte[]>>(() => new ProducerBuilder<string, byte[]>(new ProducerConfig
        {
            BootstrapServers = _kafkaOptions.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
        }).Build());
    }

    protected override async Task SendAsync(PublishRequest request, CancellationToken cancellationToken)
    {
        string topic = _kafkaOptions.TopicPrefix + request.EventName;
        if (!_knownTopics.ContainsKey(topic))
        {
            await KafkaTopology.EnsureTopicsAsync(_kafkaOptions, [topic]).ConfigureAwait(false);
            _knownTopics[topic] = true;
        }

        Message<string, byte[]> message = new()
        {
            Key = request.Envelope.CorrelationId ?? request.Envelope.MessageId.ToString(),
            Value = request.Body,
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
