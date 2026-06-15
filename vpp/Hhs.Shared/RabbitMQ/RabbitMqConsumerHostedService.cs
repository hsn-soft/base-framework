using System.Text;
using System.Text.Json;
using Hhs.Shared.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public sealed class RabbitMqConsumerHostedService<TEvent, THandler> : BackgroundService
    where TEvent : class
    where THandler : class, IIntegrationEventHandler<TEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqConsumerHostedService<TEvent, THandler>> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqConsumerHostedService(
        IServiceProvider serviceProvider,
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqConsumerHostedService<TEvent, THandler>> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password
        };

        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            cancellationToken: stoppingToken);

        // var eventName = typeof(TEvent).Name;
        string eventName = ResolveEventName(typeof(TEvent));
        string serviceName = ResolveServiceName(typeof(THandler));
        string queueName = $"{serviceName}.{eventName}";

        await _channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(
            queue: queueName,
            exchange: _options.ExchangeName,
            routingKey: eventName,
            cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(0, 1, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                string json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var @event = JsonSerializer.Deserialize<TEvent>(json);

                if (@event is null)
                {
                    await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                    return;
                }

                using var scope = _serviceProvider.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<THandler>();

                await handler.HandleAsync(@event, stoppingToken);

                await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "RabbitMQ consume error. Queue: {QueueName}, Event: {EventName}",
                    queueName,
                    eventName);

                await _channel.BasicNackAsync(
                    ea.DeliveryTag,
                    multiple: false,
                    requeue: true,
                    cancellationToken: stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);
    }

    private static string ResolveEventName(Type eventType)
    {
        string name = eventType.Name;

        return name.EndsWith("Event")
            ? name[..^"Event".Length]
            : name;
    }

    private static string ResolveServiceName(Type handlerType)
    {
        string ns = handlerType.Namespace ?? "";

        if (ns.Contains("ContentService"))
            return "content";

        if (ns.Contains("TextNormalizerService"))
            return "text-normalizer";

        if (ns.Contains("VideoGeneratorService"))
            return "video-generator";

        return "unknown-service";
    }
}