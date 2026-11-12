using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.EventBus.RabbitMQ.Configs;
using HsnSoft.Base.EventBus.RabbitMQ.Connection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus.RabbitMQ;

/// <summary>
/// Starts one <see cref="RabbitMqConsumer"/> per event the service listens to when the host starts, and stops them (waiting for the
/// messages in flight) when it stops. Fails the start when the broker cannot be reached.
/// </summary>
public sealed class RabbitMqSubscriber(
    IRabbitMqPersistentConnection connection,
    IEventBusSubscriptionManager subscriptions,
    IEventDispatcher dispatcher,
    IFailedEventNotifier failedEventNotifier,
    IOptions<EventBusOptions> options,
    IOptions<RabbitMqEventBusConfig> config,
    ILoggerFactory loggerFactory) : IHostedService, IDisposable
{
    private readonly IRabbitMqPersistentConnection _connection = connection;
    private readonly IEventBusSubscriptionManager _subscriptions = subscriptions;
    private readonly IEventDispatcher _dispatcher = dispatcher;
    private readonly IFailedEventNotifier _failedEventNotifier = failedEventNotifier;
    private readonly EventBusOptions _options = options.Value;
    private readonly RabbitMqEventBusConfig _config = config.Value;
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private readonly List<RabbitMqConsumer> _consumers = [];

    // the start adds the consumers one after the other and the stop takes them all: one at a time, so a stop that is called by more than one caller at once (the owner of the host and what disposes it) never enumerates the list while another changes it
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_subscriptions.IsEmpty)
            {
                return;
            }

            if (!_connection.IsConnected)
            {
                await _connection.TryConnectAsync(cancellationToken).ConfigureAwait(false);
            }

            ILogger logger = _loggerFactory.CreateLogger<RabbitMqConsumer>();
            foreach (IntegrationEventInfo info in _subscriptions.Events)
            {
                RabbitMqConsumer consumer = new(_connection, _subscriptions, _dispatcher, _failedEventNotifier, _options, _config, logger, info);
                _consumers.Add(consumer);
                await consumer.StartAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // a stop that waits for a start in progress stops the consumers that start made; a second stop finds none
        await _gate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            RabbitMqConsumer[] consumers = [.. _consumers];
            _consumers.Clear();

            // in parallel: one after the other the waits for the messages in flight would add up
            await Task.WhenAll(consumers.Select(consumer => consumer.DisposeAsync().AsTask())).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose() => _gate.Dispose();
}
