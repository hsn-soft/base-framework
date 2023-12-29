using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Kafka;
using HsnSoft.Base.Kafka.Converters;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HsnSoft.Base.EventBus.Kafka;

public sealed class KafkaProducer
{
    private readonly ILogger _logger;
    private readonly ProducerConfig _producerConfig;
    private readonly JsonSerializerSettings _options = DefaultJsonOptions.Get();
    private readonly KafkaEventBusConfig _kafkaEventBusConfig;

    public KafkaProducer(KafkaConnectionSettings connectionSettings, KafkaEventBusConfig kafkaEventBusConfig, ILogger logger)
    {
        _logger = logger;
        _kafkaEventBusConfig = kafkaEventBusConfig;
        _producerConfig = new ProducerConfig()
        {
            // mandatory settings
            BootstrapServers = $"{connectionSettings.HostName}:{connectionSettings.Port}",
            EnableDeliveryReports = true,
            ClientId = Dns.GetHostName(),
            Debug = "msg",
            // retry settings:
            // Receive acknowledgement from all sync replicas
            Acks = Acks.All,
            // Number of times to retry before giving up
            MessageSendMaxRetries = 3,
            // Duration to retry before next attempt
            RetryBackoffMs = 1000,
            // Set to true if you don't want to reorder messages on retry
            EnableIdempotence = true,

            // other optional settings
            ReceiveMessageMaxBytes = _kafkaEventBusConfig.KafkaProducerConfig.ReceiveMessageMaxBytes,
            MessageMaxBytes = _kafkaEventBusConfig.KafkaProducerConfig.MessageMaxBytes
        };
    }

    public async Task StartSendingMessages<TEventMessage>(string topicName, MessageEnvelope<TEventMessage> @event) where TEventMessage : IIntegrationEventMessage
    {
        using var producer = new ProducerBuilder<long, string>(_producerConfig)
            .SetKeySerializer(Serializers.Int64)
            .SetValueSerializer(Serializers.Utf8)
            .SetLogHandler((_, message) =>
            {
                switch (message.Level)
                {
                    case SyslogLevel.Emergency or SyslogLevel.Alert or SyslogLevel.Critical or SyslogLevel.Error:
                    {
                        _logger.LogError("Kafka | {ClientInfo} {Facility} => Message: {Message}", _kafkaEventBusConfig.ClientInfo, message.Facility, message.Message);

                        break;
                    }
                    case SyslogLevel.Warning or SyslogLevel.Notice or SyslogLevel.Debug:
                    {
                        _logger.LogDebug("Kafka | {ClientInfo} {Facility} => Message: {Message}", _kafkaEventBusConfig.ClientInfo, message.Facility, message.Message);
                        break;
                    }
                    default:
                    {
                        _logger.LogInformation("Kafka | {ClientInfo} {Facility} => Message: {Message}", _kafkaEventBusConfig.ClientInfo, message.Facility, message.Message);
                        break;
                    }
                }
            })
            .SetErrorHandler((_, e) => _logger.LogError("Kafka | {ClientInfo} PRODUCER => Error: {Reason}. Is Fatal: {IsFatal}", _kafkaEventBusConfig.ClientInfo, e.Reason, e.IsFatal))
            .Build();

        try
        {
            _logger.LogDebug("Kafka | {ClientInfo} PRODUCER [ {EventName} ] => MessageId [ {MessageId} ] STARTED", _kafkaEventBusConfig.ClientInfo, topicName, @event.MessageId.ToString());

            var message = JsonConvert.SerializeObject(@event, _options);

            var deliveryReport = await producer.ProduceAsync(topicName,
                new Message<long, string>
                {
                    Key = DateTime.UtcNow.Ticks,
                    Value = message
                });

            producer.Flush(new TimeSpan(0, 0, 10));
            if (deliveryReport.Status != PersistenceStatus.Persisted)
            {
                // delivery might have failed after retries. This message requires manual processing.
                _logger.LogError("Kafka | Message not ack\'d by all brokers (value: \'{Message}\'). Delivery status: {DeliveryReportStatus}", message, deliveryReport.Status);
            }
            else
            {
                _logger.LogDebug("Kafka | Message sent (value: \'{Message}\'). Delivery status: {DeliveryReportStatus}", message, deliveryReport.Status);
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(50));
            _logger.LogDebug("Kafka | {ClientInfo} PRODUCER [ {EventName} ] => MessageId [ {MessageId} ] COMPLETED", _kafkaEventBusConfig.ClientInfo, topicName, @event.MessageId.ToString());
        }
        catch (ProduceException<long, string> e)
        {
            // Log this message for manual processing.
            _logger.LogError("Kafka | {ClientInfo} PRODUCER [ {EventName} ] => MessageId [ {MessageId} ] ERROR: {ProduceError} for message (value: \'{DeliveryResultValue}\')",
                _kafkaEventBusConfig.ClientInfo,
                topicName,
                @event.MessageId.ToString(),
                e.Message,
                e.DeliveryResult.Value);
        }
    }
}