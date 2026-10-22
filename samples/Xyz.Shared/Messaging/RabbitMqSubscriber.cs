using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xyz.Shared.Events;
using Xyz.Shared.Hosting;

namespace Xyz.Shared.Messaging;

/// <summary>
/// Creates one durable queue per (service, event) pair, consumes it and calls the registered handler.
/// A handler is retried a few times; a message that still fails goes to the dead-letter queue.
/// </summary>
public sealed class RabbitMqSubscriber(
    RabbitMqConnection connection,
    IEnumerable<EventSubscription> subscriptions,
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> options,
    IOptions<ServiceOptions> service,
    ILogger<RabbitMqSubscriber> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var all = subscriptions.ToList();
        if (all.Count == 0)
        {
            return;
        }

        var settings = options.Value;
        var rabbit = await connection.GetAsync(stoppingToken);
        await using var channel = await rabbit.CreateChannelAsync(cancellationToken: stoppingToken);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: stoppingToken);

        await RabbitMqTopology.DeclareExchangeAsync(channel, settings, stoppingToken);
        await RabbitMqTopology.DeclareDeadLetterAsync(channel, settings, stoppingToken);

        foreach (var subscription in all)
        {
            string eventName = subscription.EventType.Name;
            string queue = $"{service.Value.Name}.{eventName}";

            await channel.QueueDeclareAsync(
                queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object?> { ["x-dead-letter-exchange"] = RabbitMqTopology.DeadLetterExchange(settings) },
                cancellationToken: stoppingToken);
            await channel.QueueBindAsync(queue, settings.ExchangeName, eventName, cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += (_, delivery) => ConsumeAsync(channel, subscription, delivery, stoppingToken);
            await channel.BasicConsumeAsync(queue, autoAck: false, consumer, stoppingToken);

            logger.LogInformation("{Service} listens to {Event} on queue {Queue}", service.Value.Name, eventName, queue);
        }

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // the host is shutting down
        }
    }

    private async Task ConsumeAsync(IChannel channel, EventSubscription subscription, BasicDeliverEventArgs delivery, CancellationToken stoppingToken)
    {
        var maxAttempts = Math.Max(1, options.Value.MaxHandlerAttempts);
        try
        {
            var @event = (IntegrationEvent?)JsonSerializer.Deserialize(delivery.Body.Span, subscription.EventType, JsonSerializerOptions.Web)
                         ?? throw new JsonException("The message body is empty.");

            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    await subscription.Handle(scope.ServiceProvider, @event, stoppingToken);
                    break;
                }
                catch (Exception exception) when (attempt < maxAttempts && exception is not OperationCanceledException)
                {
                    logger.LogWarning(exception, "{Event} {EventId}: attempt {Attempt}/{Max} failed", subscription.EventType.Name, @event.EventId, attempt, maxAttempts);
                    await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), stoppingToken);
                }
            }

            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "{Event} could not be handled, moving it to the dead-letter queue", subscription.EventType.Name);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, stoppingToken);
        }
    }
}
