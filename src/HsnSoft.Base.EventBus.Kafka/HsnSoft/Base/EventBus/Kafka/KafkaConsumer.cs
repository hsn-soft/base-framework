using System;
using System.Collections.Generic;
using System.Threading;
using Confluent.Kafka;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus.Logging;
using HsnSoft.Base.Kafka;

namespace HsnSoft.Base.EventBus.Kafka;

public sealed class KafkaConsumer
{
    private readonly IEventBusLogger _logger;
    private readonly ConsumerConfig _consumerConfig;
    private readonly KafkaEventBusConfig _kafkaEventBusConfig;
    private bool KeepConsuming { get; set; }

    public event EventHandler<KeyValuePair<Type, string>> OnMessageReceived;

    public KafkaConsumer(KafkaConnectionSettings connectionSettings, KafkaEventBusConfig kafkaEventBusConfig, IEventBusLogger eventBusLogger)
    {
        _logger = eventBusLogger;
        _kafkaEventBusConfig = kafkaEventBusConfig;
        _consumerConfig = new ConsumerConfig
        {
            // mandatory settings
            BootstrapServers = $"{connectionSettings.HostName}:{connectionSettings.Port}",
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false,
            MaxPollIntervalMs = 300000,
            GroupId = _kafkaEventBusConfig.ClientInfo,
            // Read messages from start if no commit exists.
            AutoOffsetReset = AutoOffsetReset.Earliest,

            // other optional settings
            SessionTimeoutMs = _kafkaEventBusConfig.KafkaConsumerConfig.SessionTimeoutMs,
            HeartbeatIntervalMs = _kafkaEventBusConfig.KafkaConsumerConfig.HeartbeatIntervalMs,
            FetchMaxBytes = _kafkaEventBusConfig.KafkaConsumerConfig.FetchMaxBytes,
            MaxPartitionFetchBytes = _kafkaEventBusConfig.KafkaConsumerConfig.MaxPartitionFetchBytes
        };
        KeepConsuming = true;
    }

    public void StartReceivingMessages<TEvent>(string topicName, CancellationToken stoppingToken) where TEvent : IIntegrationEventMessage
    {
        StartReceivingMessages(typeof(TEvent), topicName, stoppingToken);
    }

    public void StartReceivingMessages(Type eventType, string topicName, CancellationToken stoppingToken)
    {
        if (!eventType.IsAssignableTo(typeof(IIntegrationEventMessage))) throw new TypeAccessException();

        using var consumer = new ConsumerBuilder<long, string>(_consumerConfig)
            .SetKeyDeserializer(Deserializers.Int64)
            .SetValueDeserializer(Deserializers.Utf8)
            .SetLogHandler((_, message) =>
            {
                switch (message.Level)
                {
                    case SyslogLevel.Emergency or SyslogLevel.Alert or SyslogLevel.Critical or SyslogLevel.Error:
                    {
                        _logger.LogError("Kafka | {ClientInfo} {Facility} => Message: {Message}", _kafkaEventBusConfig.ClientInfo, message.Facility, message.Message);
                        break;
                    }
                    default:
                    {
                        _logger.LogDebug("Kafka | {ClientInfo} {Facility} => Message: {Message}", _kafkaEventBusConfig.ClientInfo, message.Facility, message.Message);
                        break;
                    }
                }
            })
            .SetErrorHandler((_, e) =>
            {
                _logger.LogError("Kafka | {ClientInfo} CONSUMER => Error: {Reason}. Is Fatal: {IsFatal}", _kafkaEventBusConfig.ClientInfo, e.Reason, e.IsFatal);
                KeepConsuming = !e.IsFatal;
            })
            .Build();

        try
        {
            consumer.Subscribe(topicName);
            _logger.LogDebug("Kafka | {ClientInfo} CONSUMER [ {EventName} ] => Subscribed", _kafkaEventBusConfig.ClientInfo, topicName);

            while (KeepConsuming && !stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);
                    // var result = consumer.Consume(TimeSpan.FromMilliseconds(_consumerConfig.MaxPollIntervalMs - 1000 ?? 250000));
                    var message = result?.Message?.Value;
                    if (message == null)
                    {
                        _logger.LogDebug("Kafka | {ClientInfo} CONSUMER [ {EventName} ] => Loop [ {Time} ]", _kafkaEventBusConfig.ClientInfo, topicName, DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss zz"));
                        continue;
                    }

                    _logger.LogDebug("Kafka | {ClientInfo} CONSUMER [ {EventName} ] => Consume STARTED", _kafkaEventBusConfig.ClientInfo, topicName);

                    consumer.Commit(result);
                    consumer.StoreOffset(result);

                    _logger.LogDebug("Kafka | {ClientInfo} CONSUMER [ {EventName} ] => Received: {Key}:{Message} from partition: {Partition}", _kafkaEventBusConfig.ClientInfo, topicName, result.Message.Key, message, result.Partition.Value);
                    OnMessageReceived?.Invoke(this, new KeyValuePair<Type, string>(eventType, message));
                }
                catch (ConsumeException ce)
                {
                    if (ce.Error.IsFatal) throw;
                    _logger.LogDebug("Kafka | {ClientInfo} CONSUMER [ {EventName} ] => Consume ERROR : {ConsumeError} | {Time}", _kafkaEventBusConfig.ClientInfo, topicName, ce.Message, DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss zz"));
                }

                // loop wait period
                Thread.Sleep(TimeSpan.FromMilliseconds(200));
            }
        }
        catch (OperationCanceledException oe)
        {
            _logger.LogDebug("Kafka | {ClientInfo} CONSUMER [ {EventName} ] => Closing... : {CancelledMessage}", _kafkaEventBusConfig.ClientInfo, topicName, oe.Message);
        }
        catch (Exception e)
        {
            _logger.LogError("Kafka | {ClientInfo} CONSUMER [ {EventName} ] => FatalError : {FatalError}", _kafkaEventBusConfig.ClientInfo, topicName, e.Message);
        }
        finally
        {
            consumer.Close();
            _logger.LogDebug("Kafka | {ClientInfo} CONSUMER [ {EventName} ] => Closed", _kafkaEventBusConfig.ClientInfo, topicName);
        }
    }
}