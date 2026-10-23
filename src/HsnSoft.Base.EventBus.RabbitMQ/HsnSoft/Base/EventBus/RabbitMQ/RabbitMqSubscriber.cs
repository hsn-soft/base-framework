using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace HsnSoft.Base.EventBus.RabbitMQ;

/// <summary>
/// Creates one durable queue per (service, event) pair, consumes it and gives each message to the
/// <see cref="IEventDispatcher"/>. A message the dispatcher gives up on goes to the dead-letter queue.
/// </summary>
public sealed partial class RabbitMqSubscriber(
    RabbitMqConnection connection,
    IEnumerable<EventSubscription> subscriptions,
    IEventDispatcher dispatcher,
    IOptions<RabbitMqOptions> options,
    IOptions<EventBusOptions> eventBusOptions,
    ILogger<RabbitMqSubscriber> logger) : BackgroundService
{
    private readonly RabbitMqConnection _connection = connection;
    private readonly List<EventSubscription> _subscriptions = [.. subscriptions];
    private readonly IEventDispatcher _dispatcher = dispatcher;
    private readonly RabbitMqOptions _options = options.Value;
    private readonly EventBusOptions _eventBusOptions = eventBusOptions.Value;
    private readonly ILogger<RabbitMqSubscriber> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_subscriptions.Count == 0)
        {
            return;
        }

        IConnection rabbit = await _connection.GetAsync(stoppingToken).ConfigureAwait(false);
        IChannel channel = await rabbit.CreateChannelAsync(cancellationToken: stoppingToken).ConfigureAwait(false);
        await using (channel.ConfigureAwait(false))
        {
            await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: stoppingToken).ConfigureAwait(false);

            await RabbitMqTopology.DeclareExchangeAsync(channel, _options, stoppingToken).ConfigureAwait(false);
            await RabbitMqTopology.DeclareDeadLetterAsync(channel, _options, stoppingToken).ConfigureAwait(false);

            foreach (EventSubscription subscription in _subscriptions)
            {
                string eventName = subscription.EventType.Name;
                string queue = $"{_eventBusOptions.ServiceName}.{eventName}";

                await channel.QueueDeclareAsync(
                    queue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: new Dictionary<string, object?> { ["x-dead-letter-exchange"] = RabbitMqTopology.DeadLetterExchange(_options) },
                    cancellationToken: stoppingToken).ConfigureAwait(false);
                await channel.QueueBindAsync(queue, _options.ExchangeName, eventName, cancellationToken: stoppingToken).ConfigureAwait(false);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += (_, delivery) => ConsumeAsync(channel, subscription, delivery, stoppingToken);
                await channel.BasicConsumeAsync(queue, autoAck: false, consumer, stoppingToken).ConfigureAwait(false);

                LogListening(_logger, _eventBusOptions.ServiceName, eventName, queue);
            }

            // hold the channel (and its consumers) open until the host shuts down
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }

    private async Task ConsumeAsync(IChannel channel, EventSubscription subscription, BasicDeliverEventArgs delivery, CancellationToken stoppingToken)
    {
        try
        {
            await _dispatcher.DispatchAsync(subscription, delivery.Body, stoppingToken).ConfigureAwait(false);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogDeadLettered(_logger, exception, subscription.EventType.Name);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, stoppingToken).ConfigureAwait(false);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{Service} listens to {Event} on queue {Queue}")]
    private static partial void LogListening(ILogger logger, string service, string @event, string queue);

    [LoggerMessage(Level = LogLevel.Error, Message = "{Event} could not be handled, moving it to the dead-letter queue")]
    private static partial void LogDeadLettered(ILogger logger, Exception exception, string @event);
}
