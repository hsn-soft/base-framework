using System;
using System.Threading;
using Confluent.Kafka;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Kafka;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HsnSoft.Base.EventBus.Kafka;

public sealed class KafkaConsumer
{
    private readonly ILogger _logger;
    private readonly ConsumerConfig _consumerConfig;
    private readonly EventBusConfig _eventBusConfig;
    private bool KeepConsuming { get; set; }

    public event EventHandler<object> OnMessageReceived;

    public KafkaConsumer(KafkaConnectionSettings connectionSettings, EventBusConfig eventBusConfig, ILogger logger)
    {
        _logger = logger;
        _eventBusConfig = eventBusConfig;
        _consumerConfig = new ConsumerConfig
        {
            BootstrapServers = $"{connectionSettings.HostName}:{connectionSettings.Port}",
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false,
            MaxPollIntervalMs = 300000,
            GroupId = eventBusConfig.ClientInfo,

            // Read messages from start if no commit exists.
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
        KeepConsuming = true;
    }

    public void StartReceivingMessages<TEvent>(string topicName, CancellationToken stoppingToken) where TEvent : IntegrationEvent
    {
        StartReceivingMessages(typeof(TEvent), topicName, stoppingToken);
    }

    public void StartReceivingMessages(Type eventType, string topicName, CancellationToken stoppingToken)
    {
        if (!eventType.IsAssignableTo(typeof(IntegrationEvent))) throw new TypeAccessException();

        using var consumer = new ConsumerBuilder<long, string>(_consumerConfig)
            .SetKeyDeserializer(Deserializers.Int64)
            .SetValueDeserializer(Deserializers.Utf8)
            .SetLogHandler((_, message) =>
            {
                switch (message.Level)
                {
                    case SyslogLevel.Emergency or SyslogLevel.Alert or SyslogLevel.Critical or SyslogLevel.Error:
                    {
                        _logger.LogError("Kafka | {ClientInfo} {Facility} => Message: {Message}", _eventBusConfig.ClientInfo, message.Facility, message.Message);

                        break;
                    }
                    case SyslogLevel.Warning or SyslogLevel.Notice or SyslogLevel.Debug:
                    {
                        _logger.LogDebug("Kafka | {ClientInfo} {Facility} => Message: {Message}", _eventBusConfig.ClientInfo, message.Facility, message.Message);
                        break;
                    }
                    default:
                    {
                        _logger.LogInformation("Kafka | {ClientInfo} {Facility} => Message: {Message}", _eventBusConfig.ClientInfo, message.Facility, message.Message);
                        break;
                    }
                }
            })
            .SetErrorHandler((_, e) =>
            {
                _logger.LogError("Kafka | {ClientInfo} CONSUMER => Error: {Reason}. Is Fatal: {IsFatal}", _eventBusConfig.ClientInfo, e.Reason, e.IsFatal);
                KeepConsuming = !e.IsFatal;
            })
            .Build();

        try
        {
            consumer.Subscribe(topicName);
            _logger.LogInformation("Kafka | {ClientInfo} CONSUMER [ {EventName} ] => Subscribed", _eventBusConfig.ClientInfo, topicName);

            while (KeepConsuming && !stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);
                    // var result = consumer.Consume(TimeSpan.FromMilliseconds(_consumerConfig.MaxPollIntervalMs - 1000 ?? 250000));
                    var message = result?.Message?.Value;
                    if (message == null)
                    {
                        _logger.LogDebug("Kafka | {ClientInfo} CONSUMER [ {EventName} ] => Loop [ {Time} ]", _eventBusConfig.ClientInfo, topicName, DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss zz"));
                        continue;
                    }

                    _logger.LogDebug("Kafka | {ClientInfo} CONSUMER [ {EventName} ] => Consume STARTED", _eventBusConfig.ClientInfo, topicName);

                    consumer.Commit(result);
                    consumer.StoreOffset(result);

                    var @event = JsonConvert.DeserializeObject(message, eventType);
                    _logger.LogDebug("Kafka | {ClientInfo} CONSUMER [ {EventName} ] => Received: {Key}:{Message} from partition: {Partition}", _eventBusConfig.ClientInfo, topicName, result.Message.Key, message, result.Partition.Value);
                    OnMessageReceived?.Invoke(this, @event);
                }
                catch (ConsumeException ce)
                {
                    if (ce.Error.IsFatal) throw;
                    _logger.LogWarning("Kafka | {ClientInfo} CONSUMER [ {EventName} ] => Consume ERROR : {ConsumeError} | {Time}", _eventBusConfig.ClientInfo, topicName, ce.Message, DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss zz"));
                }

                // loop wait period
                Thread.Sleep(TimeSpan.FromMilliseconds(200));
            }
        }
        catch (OperationCanceledException oe)
        {
            _logger.LogWarning("Kafka | {ClientInfo} CONSUMER [ {EventName} ] => Closing... : {CancelledMessage}", _eventBusConfig.ClientInfo, topicName, oe.Message);
        }
        catch (Exception e)
        {
            _logger.LogError("Kafka | {ClientInfo} CONSUMER [ {EventName} ] => FatalError : {FatalError}", _eventBusConfig.ClientInfo, topicName, e.Message);
        }
        finally
        {
            consumer.Close();
            _logger.LogInformation("Kafka | {ClientInfo} CONSUMER [ {EventName} ] => Closed", _eventBusConfig.ClientInfo, topicName);
        }
    }
}