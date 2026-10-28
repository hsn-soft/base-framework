using System;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.EventBus.RabbitMQ.Connection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace HsnSoft.Base.EventBus.RabbitMQ;

/// <summary>
/// Publishes to RabbitMQ: a <c>direct</c> exchange with the event name as routing key (to every service that listens), or, for a message sent to one service,
/// straight to the queue that service reads the event from.
/// </summary>
public sealed partial class EventBusRabbitMq(
    IRabbitMqPersistentConnection connection,
    EventEnvelopeFactory envelopeFactory,
    IOptions<EventBusOptions> options,
    ILogger<EventBusRabbitMq> logger) : EventBusBase(envelopeFactory, options, logger), IAsyncDisposable
{
    private readonly IRabbitMqPersistentConnection _connection = connection;
    private readonly ILogger<EventBusRabbitMq> _logger = logger;
    private int _publishing;
    private bool _disposed;

    protected override bool IsTransient(Exception exception) => RabbitMqPublisher.IsTransient(exception);

    protected override async Task TransmitAsync(PublishRequest request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _publishing);
        try
        {
            if (!_connection.IsConnected)
            {
                await _connection.TryConnectAsync(cancellationToken).ConfigureAwait(false);
            }

            if (!_connection.IsConnected)
            {
                throw new RabbitMqNotConnectedException();
            }

            await RabbitMqPublisher.WithConfirmTimeoutAsync(
                async limit =>
                {
                    IChannel channel = await _connection.CreatePublisherChannelAsync(limit).ConfigureAwait(false);
                    await using (channel.ConfigureAwait(false))
                    {
                        BasicProperties properties = RabbitMqPublisher.Properties(request.Envelope.MessageId, request.Envelope.CorrelationId);
                        if (request.TargetService is null)
                        {
                            if (!await RabbitMqPublisher.PublishToExchangeAsync(channel, Settings.ExchangeName, request.EventName, properties, request.Body, limit).ConfigureAwait(false))
                            {
                                LogUnroutable(_logger, request.EventName, request.Envelope.MessageId);
                            }
                        }
                        else
                        {
                            // straight to the queue the service reads the event from: nobody else gets it
                            await RabbitMqPublisher.PublishToQueueAsync(channel, EventNameHelper.GetConsumerClientEventQueueName(Settings, request.TargetService, request.EventName), properties, request.Body, limit).ConfigureAwait(false);
                        }
                    }
                },
                Settings.PublishTimeout,
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Decrement(ref _publishing);
        }
    }

    /// <summary>Waits for the messages being published to finish (up to the shutdown timeout). The connection belongs to the container.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        DateTime deadline = DateTime.UtcNow + Settings.ShutdownTimeout;
        while (Volatile.Read(ref _publishing) > 0 && DateTime.UtcNow < deadline)
        {
            LogWaitingForPublishers(_logger);
            await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
        }
    }

    [LoggerMessage(EventId = 4402, EventName = "RABBITMQ_BUS_UNROUTABLE", Level = LogLevel.Warning, Message = "{EventName} ({MessageId}) was not delivered: no queue is bound to it (no service listens to the event yet)")]
    private static partial void LogUnroutable(ILogger logger, string eventName, Guid messageId);

    [LoggerMessage(EventId = 4401, EventName = "RABBITMQ_BUS_WAITING_FOR_PUBLISHERS", Level = LogLevel.Debug, Message = "Waiting for the publishers to finish...")]
    private static partial void LogWaitingForPublishers(ILogger logger);
}
