using System;
using System.Linq;
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

    // a consumer that fails on its own (the broker could not be read, an offset could not be committed) is built again after a pause, so one failure does not stop the host and the other events with it
    private Task ConsumeLoopAsync(IntegrationEventInfo info, IProducer<string, byte[]> deadLetters, string deadLetterTopic, CancellationToken stoppingToken)
        => KafkaRecovery.RunAsync(
            cancellationToken => ConsumeAsync(info, deadLetters, deadLetterTopic, cancellationToken),
            _options.RecoveryDelay,
            exception => LogConsumerFailed(_logger, exception, KafkaTopology.GroupFor(_eventBusOptions, info.EventName), _options.RecoveryDelay.TotalSeconds),
            stoppingToken);

    private Task ConsumeAsync(IntegrationEventInfo info, IProducer<string, byte[]> deadLetters, string deadLetterTopic, CancellationToken stoppingToken)
        => new KafkaEventConsumer(info, _options, _eventBusOptions, _dispatcher, deadLetters, deadLetterTopic, _logger).RunAsync(stoppingToken);

    [LoggerMessage(EventId = 4503, EventName = "KAFKA_SUBSCRIBER_CONSUMER_FAILED", Level = LogLevel.Error, Message = "{Group}: the consumer failed, it starts again in {Seconds} s")]
    private static partial void LogConsumerFailed(ILogger logger, Exception exception, string group, double seconds);
}
