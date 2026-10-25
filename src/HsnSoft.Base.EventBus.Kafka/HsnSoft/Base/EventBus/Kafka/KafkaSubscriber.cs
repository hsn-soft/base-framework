using System;
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
/// Reads the topic of every subscribed event with a consumer of its own, in a consumer group of its own (<c>{ExchangeName}_{ConsumerClientName}_{EventName}</c>, the name of the queue the RabbitMQ bus makes for the same service and
/// event): the offsets of the events, and of the services that handle the same event, are independent, and a slow handler of one event does not hold the others back. A consumer reads one message at a time and gives it to the
/// <see cref="IEventDispatcher"/>. A message the dispatcher gives up on is copied to the dead-letter topic, then the offset moves on. Offsets are committed only after a message is dealt with, so a crash re-delivers it.
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

        string deadLetterTopic = KafkaTopology.DeadLetterTopic(_eventBusOptions);
        IntegrationEventInfo[] events = [.. _subscriptions.Events];

        await KafkaTopology.EnsureTopicsAsync(_options, events.Select(info => KafkaTopology.TopicFor(_eventBusOptions, info.EventName)).Append(deadLetterTopic)).ConfigureAwait(false);

        // consuming blocks a thread, so each consumer runs on one of its own and not on the one that started the host
        using IProducer<string, byte[]> deadLetters = new ProducerBuilder<string, byte[]>(new ProducerConfig { BootstrapServers = _options.BootstrapServers }).Build();
        await Task.WhenAll(events.Select(info => Task.Run(() => ConsumeLoopAsync(info, deadLetters, deadLetterTopic, stoppingToken), CancellationToken.None))).ConfigureAwait(false);
    }

    private async Task ConsumeLoopAsync(IntegrationEventInfo info, IProducer<string, byte[]> deadLetters, string deadLetterTopic, CancellationToken stoppingToken)
    {
        string topic = KafkaTopology.TopicFor(_eventBusOptions, info.EventName);
        string group = KafkaTopology.GroupFor(_eventBusOptions, info.EventName);
        using IConsumer<string, byte[]> consumer = new ConsumerBuilder<string, byte[]>(new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = group,
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
        }).Build();

        consumer.Subscribe(topic);
        LogListening(_logger, group, topic);

        // a stopping service reads no new message, but the one it is handling is let finish: its handler is cancelled only when the shutdown timeout has passed (and the message is then handled again later, as it was not committed)
        using CancellationTokenSource handlerCts = new();
        await using CancellationTokenRegistration stopping = stoppingToken.Register(() => handlerCts.CancelAfter(_eventBusOptions.ShutdownTimeout));

        try
        {
            while (true)
            {
                ConsumeResult<string, byte[]> result = consumer.Consume(stoppingToken);
                try
                {
                    object envelope = EventSerializer.DeserializeEnvelope(result.Message.Value, info.EventType);
                    await _dispatcher.DispatchAsync(info.EventName, info.EventType, envelope, handlerCts.Token).ConfigureAwait(false);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    LogDeadLettered(_logger, exception, result.Topic);
                    await deadLetters.ProduceAsync(deadLetterTopic, new Message<string, byte[]>
                    {
                        Key = result.Message.Key,
                        Value = result.Message.Value,
                        Headers = new Headers { { "x-original-topic", Encoding.UTF8.GetBytes(result.Topic) } },
                    }, handlerCts.Token).ConfigureAwait(false);
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

    [LoggerMessage(EventId = 4501, EventName = "KAFKA_SUBSCRIBER_LISTENING", Level = LogLevel.Information, Message = "{Group} listens to topic {Topic}")]
    private static partial void LogListening(ILogger logger, string group, string topic);

    [LoggerMessage(EventId = 4502, EventName = "KAFKA_SUBSCRIBER_DEAD_LETTERED", Level = LogLevel.Error, Message = "A message from {Topic} could not be handled, moving it to the dead-letter topic")]
    private static partial void LogDeadLettered(ILogger logger, Exception exception, string topic);
}
