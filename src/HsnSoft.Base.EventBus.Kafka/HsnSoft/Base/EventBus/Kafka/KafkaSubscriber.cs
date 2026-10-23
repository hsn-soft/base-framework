using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus.Kafka;

/// <summary>
/// Joins the consumer group named after the service, reads the topic of every subscribed event one message at a time and gives
/// each to the <see cref="IEventDispatcher"/>. A message the dispatcher gives up on is copied to the dead-letter topic, then
/// the offset moves on. Offsets are committed only after a message is dealt with, so a crash re-delivers it.
/// </summary>
public sealed partial class KafkaSubscriber(
    IEventBusSubscriptionManager subscriptions,
    IEventDispatcher dispatcher,
    IOptions<KafkaOptions> options,
    IOptions<EventBusOptions> eventBusOptions,
    ILogger<KafkaSubscriber> logger) : BackgroundService
{
    private readonly IEventBusSubscriptionManager _subscriptions = subscriptions;
    private readonly IEventDispatcher _dispatcher = dispatcher;
    private readonly KafkaOptions _options = options.Value;
    private readonly EventBusOptions _eventBusOptions = eventBusOptions.Value;
    private readonly ILogger<KafkaSubscriber> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_subscriptions.IsEmpty)
        {
            return;
        }

        Dictionary<string, IntegrationEventInfo> byTopic = _subscriptions.Events.ToDictionary(info => KafkaTopology.TopicFor(_options, info.EventName));
        string deadLetterTopic = KafkaTopology.DeadLetterTopic(_options);

        await KafkaTopology.EnsureTopicsAsync(_options, byTopic.Keys.Append(deadLetterTopic)).ConfigureAwait(false);

        // consuming blocks a thread, so it must not run on the one that started the host
        await Task.Run(() => ConsumeLoopAsync(byTopic, deadLetterTopic, stoppingToken), CancellationToken.None).ConfigureAwait(false);
    }

    private async Task ConsumeLoopAsync(Dictionary<string, IntegrationEventInfo> byTopic, string deadLetterTopic, CancellationToken stoppingToken)
    {
        using IConsumer<string, byte[]> consumer = new ConsumerBuilder<string, byte[]>(new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _eventBusOptions.ConsumerClientInfo,
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
        }).Build();
        using IProducer<string, byte[]> deadLetters = new ProducerBuilder<string, byte[]>(new ProducerConfig { BootstrapServers = _options.BootstrapServers }).Build();

        consumer.Subscribe(byTopic.Keys);
        string topics = string.Join(", ", byTopic.Keys);
        LogListening(_logger, _eventBusOptions.ConsumerClientInfo, topics);

        try
        {
            while (true)
            {
                ConsumeResult<string, byte[]> result = consumer.Consume(stoppingToken);
                try
                {
                    IntegrationEventInfo info = byTopic[result.Topic];
                    object envelope = EventSerializer.DeserializeEnvelope(result.Message.Value, info.EventType);
                    await _dispatcher.DispatchAsync(info.EventName, info.EventType, envelope, stoppingToken).ConfigureAwait(false);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    LogDeadLettered(_logger, exception, result.Topic);
                    await deadLetters.ProduceAsync(deadLetterTopic, new Message<string, byte[]>
                    {
                        Key = result.Message.Key,
                        Value = result.Message.Value,
                        Headers = new Headers { { "x-original-topic", Encoding.UTF8.GetBytes(result.Topic) } },
                    }, stoppingToken).ConfigureAwait(false);
                }

                consumer.Commit(result);
            }
        }
        catch (OperationCanceledException)
        {
            // the host is shutting down
        }
        finally
        {
            consumer.Close();
        }
    }

    [LoggerMessage(EventId = 4501, EventName = "KAFKA_SUBSCRIBER_LISTENING", Level = LogLevel.Information, Message = "{Service} listens to topics {Topics}")]
    private static partial void LogListening(ILogger logger, string service, string topics);

    [LoggerMessage(EventId = 4502, EventName = "KAFKA_SUBSCRIBER_DEAD_LETTERED", Level = LogLevel.Error, Message = "A message from {Topic} could not be handled, moving it to the dead-letter topic")]
    private static partial void LogDeadLettered(ILogger logger, Exception exception, string topic);
}
