using System;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus.Logging;
using HsnSoft.Base.EventBus.RabbitMQ.Configs;
using HsnSoft.Base.EventBus.RabbitMQ.Connection;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace HsnSoft.Base.EventBus.RabbitMQ;

/// <summary>
/// Publishes a FailedEto to this microservice's own error queue, notifying event-manager of a
/// pre-handler (framework-level) dispatch failure. Extracted from RabbitMqConsumer so the same
/// notification path is reusable by ReQueuedEtoHandler's redispatch failures too.
/// </summary>
public sealed class FailedEventNotifier(
    IRabbitMqPersistentConnection persistentConnection,
    IEventBusSubscriptionManager subscriptionsManager,
    IOptions<RabbitMqEventBusConfig> rabbitMqEventBusConfigOptions,
    IEventBusLogger logger
) : IFailedEventNotifier
{
    private readonly RabbitMqEventBusConfig _rabbitMqEventBusConfig = rabbitMqEventBusConfigOptions.Value;

    public async Task NotifyAsync(string errorMessage, string failedEventName, string failedMessageContent)
    {
        ParentMessageEnvelope failedEnvelopeInfo = null;
        Type failedEventEnvelopeMessageType = null;
        IIntegrationEventMessage failedMessageObject = null;
        try
        {
            dynamic failedEnvelope = JsonConvert.DeserializeObject<dynamic>(failedMessageContent);
            failedEnvelopeInfo = ((JObject)failedEnvelope)?.ToObject<ParentMessageEnvelope>();

            failedEventEnvelopeMessageType = subscriptionsManager.GetEventInfoByName(failedEventName)?.EventType;

            var genericClass = typeof(MessageEnvelope<>);
            var constructedClass = genericClass.MakeGenericType(failedEventEnvelopeMessageType!);
            object failedEventEnvelope = System.Text.Json.JsonSerializer.Deserialize(failedMessageContent, constructedClass);

            failedMessageObject = ((dynamic)failedEventEnvelope)?.Message;
        }
        catch (Exception e)
        {
            errorMessage += ". FailedMessageContent convert operation error: " + e.Message;
        }

        await PublishFailedEtoAsync(errorMessage, failedEnvelopeInfo, failedEventEnvelopeMessageType?.Name, failedMessageObject);
    }

    public async Task NotifyDirectAsync(string errorMessage, [CanBeNull] string failedMessageTypeName, [CanBeNull] object failedMessageObject, [CanBeNull] ParentMessageEnvelope parentContext)
    {
        await PublishFailedEtoAsync(errorMessage, parentContext, failedMessageTypeName, failedMessageObject);
    }

    private async Task PublishFailedEtoAsync(string errorMessage, [CanBeNull] ParentMessageEnvelope failedEnvelopeInfo, [CanBeNull] string failedMessageTypeName, [CanBeNull] object failedMessageObject)
    {
        if (!persistentConnection.IsConnected) await persistentConnection.TryConnectAsync();
        if (!persistentConnection.IsConnected) throw new ConnectFailureException("", new Exception("Connection fail"));

        var produceTime = DateTime.UtcNow;
        var @event = new MessageEnvelope<FailedEto>
        {
            ParentMessageId = failedEnvelopeInfo?.MessageId,
            MessageId = Guid.CreateVersion7(),
            MessageTime = produceTime,
            Message = new FailedEto(
                FailedReason: errorMessage,
                FailedMessageEnvelopeTime: failedEnvelopeInfo?.MessageTime.ToUniversalTime(),
                FailedMessageObject: failedMessageObject,
                FailedMessageTypeName: failedMessageTypeName
            ),
            Producer = _rabbitMqEventBusConfig.ConsumerClientInfo,
            CorrelationId = failedEnvelopeInfo?.CorrelationId,
            UserId = failedEnvelopeInfo?.UserId,
            UserRoles = failedEnvelopeInfo?.UserRoles,
            ClientLat = failedEnvelopeInfo?.ClientLat,
            ClientLong = failedEnvelopeInfo?.ClientLong,
            ClientChannel = failedEnvelopeInfo?.ClientChannel,
            ClientVersion = failedEnvelopeInfo?.ClientVersion,
            HopLevel = failedEnvelopeInfo != null ? (ushort)(failedEnvelopeInfo.HopLevel + 1) : (ushort)1,
            ReQueuedCount = failedEnvelopeInfo?.ReQueuedCount ?? 0
        };

        string eventName = EventNameHelper.TrimEventName(_rabbitMqEventBusConfig, @event.Message.GetType().Name);
        string consumerErrorQueueName = $"{_rabbitMqEventBusConfig.ErrorClientInfo}_{eventName}";

        logger.LogWarning("{BrokerName} | PRODUCER {ClientInfo} EVENT [ {EventName} ] => MessageId [ {MessageId} ] STARTED",
            "RabbitMQ", _rabbitMqEventBusConfig.ConsumerClientInfo, eventName, @event.MessageId.ToString());

        var policy = Policy.Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
            {
                logger.LogError("{BrokerName} | Could not publish failed event message : {Event} after {Timeout}s ({ExceptionMessage})",
                    "RabbitMQ", failedMessageObject, $"{time.TotalSeconds:n1}", ex.Message);

                // Persistent Log
                logger.EventBusErrorLog(new ProduceMessageLogModel(
                    LogId: Guid.CreateVersion7().ToString(),
                    CorrelationId: @event.CorrelationId,
                    Facility: nameof(EventBusLogFacility.PRODUCE_EVENT_ERROR),
                    ProduceDateTimeUtc: produceTime,
                    MessageLog: new MessageLogDetail(
                        EventType: eventName,
                        HopLevel: @event.HopLevel,
                        ParentMessageId: @event.ParentMessageId,
                        MessageId: @event.MessageId,
                        MessageTime: @event.MessageTime,
                        Message: @event.Message,
                        UserId: @event.UserId,
                        UserRoles: @event.UserRoles,
                        ClientLat: @event.ClientLat,
                        ClientLong: @event.ClientLong,
                        ClientChannel: @event.ClientChannel,
                        ClientVersion: @event.ClientVersion
                    ),
                    ProduceDetails: $"Message publish error: {ex.Message}"));
            });

        byte[] body = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), new JsonSerializerOptions { WriteIndented = true });

        await policy.ExecuteAsync(async () =>
        {
            await using var publisherChannel = await persistentConnection.CreateModelAsync()!;

            await publisherChannel.QueueDeclareAsync(
                queue: consumerErrorQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            await publisherChannel.BasicPublishAsync(
                exchange: "",
                routingKey: consumerErrorQueueName,
                mandatory: true,
                basicProperties: new BasicProperties { DeliveryMode = DeliveryModes.Persistent },
                body: body);
        });

        logger.LogWarning("{BrokerName} | PRODUCER {ClientInfo} EVENT [ {EventName} ] => MessageId [ {MessageId} ] COMPLETED",
            "RabbitMQ", _rabbitMqEventBusConfig.ConsumerClientInfo, eventName, @event.MessageId.ToString());
    }
}
