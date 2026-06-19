using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Hhs.Shared.RabbitMQ;

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
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password
            };

            // Retry connection 3 times with delays: 10s, 20s, 30s
            var delays = new[] { 10, 20, 30 };
            Exception lastException = null;

            for (int attempt = 0; attempt < delays.Length; attempt++)
            {
                try
                {
                    _connection = await factory.CreateConnectionAsync(stoppingToken);
                    _logger.LogInformation("RabbitMQ connection established on attempt {Attempt}", attempt + 1);
                    break;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    if (attempt < delays.Length - 1)
                    {
                        _logger.LogWarning("RabbitMQ connection attempt {Attempt} failed, retrying in {DelaySeconds}s: {Error}",
                            attempt + 1, delays[attempt], ex.Message);
                        await Task.Delay(TimeSpan.FromSeconds(delays[attempt]), stoppingToken);
                    }
                    else
                    {
                        _logger.LogError(ex, "RabbitMQ connection failed after {Attempts} attempts", delays.Length);
                        throw;
                    }
                }
            }

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ consumer service failed");
            throw;
        }
    }

    private static string ResolveEventName(Type eventType)
    {
        string name = eventType.Name;

        return name.EndsWith("Eto")
            ? name[..^"Eto".Length]
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