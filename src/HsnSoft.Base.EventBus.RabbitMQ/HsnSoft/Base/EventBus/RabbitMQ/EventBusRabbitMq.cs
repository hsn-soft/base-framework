using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.EventBus.RabbitMQ.Connection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

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

    protected override bool IsTransient(Exception exception) => exception is BrokerUnreachableException or SocketException;

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
                throw new InvalidOperationException("RabbitMQ connection could not be established.");
            }

            IChannel channel = await _connection.CreateModelAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            await using (channel.ConfigureAwait(false))
            {
                BasicProperties properties = RabbitMqPublisher.Properties(request.Envelope.MessageId, request.Envelope.CorrelationId);
                if (request.TargetService is null)
                {
                    await RabbitMqPublisher.PublishToExchangeAsync(channel, Settings.ExchangeName, request.EventName, properties, request.Body, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // straight to the queue the service reads the event from: nobody else gets it
                    await RabbitMqPublisher.PublishToQueueAsync(channel, EventNameHelper.GetConsumerClientEventQueueName(Settings, request.TargetService, request.EventName), properties, request.Body, cancellationToken).ConfigureAwait(false);
                }
            }
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

    [LoggerMessage(EventId = 4401, EventName = "RABBITMQ_BUS_WAITING_FOR_PUBLISHERS", Level = LogLevel.Debug, Message = "Waiting for the publishers to finish...")]
    private static partial void LogWaitingForPublishers(ILogger logger);
}
