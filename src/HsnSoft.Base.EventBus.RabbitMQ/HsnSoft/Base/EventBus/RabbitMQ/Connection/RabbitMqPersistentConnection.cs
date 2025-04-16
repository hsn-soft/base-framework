using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.EventBus.Logging;
using HsnSoft.Base.EventBus.RabbitMQ.Configs;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace HsnSoft.Base.EventBus.RabbitMQ.Connection;

public class RabbitMqPersistentConnection(IOptions<RabbitMqConnectionSettings> conSettings, IEventBusLogger logger) : IRabbitMqPersistentConnection
{
    private readonly IConnectionFactory _connectionFactory = new ConnectionFactory
    {
        HostName = conSettings.Value.HostName,
        Port = conSettings.Value.Port,
        UserName = conSettings.Value.UserName,
        Password = conSettings.Value.Password,
        VirtualHost = conSettings.Value.VirtualHost,
        RequestedHeartbeat = TimeSpan.FromSeconds(60),
        //DispatchConsumersAsync = true,
    };

    private readonly int _retryCount = conSettings.Value.ConnectionRetryCount;

    private IConnection _connection;
    private bool _disposed;

    private readonly Lock _syncRoot = new();

    //DispatchConsumersAsync = true,

    public bool IsConnected => _connection is { IsOpen: true } && !_disposed;

    public Task<bool> TryConnectAsync()
    {
        logger.LogInformation("RabbitMQ Client is trying to connect");

        lock (_syncRoot)
        {
            var policy = Policy.Handle<SocketException>()
                .Or<BrokerUnreachableException>()
                .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (ex, time) => { logger.LogWarning("RabbitMQ Client could not connect after {TimeOut}s ({ExceptionMessage})", $"{time.TotalSeconds:n1}", ex.Message); }
                );

            policy.Execute(async () => { _connection = await _connectionFactory.CreateConnectionAsync(); }).GetAwaiter().GetResult();

            if (IsConnected)
            {
                _connection!.ConnectionShutdownAsync += OnConnectionShutdownAsync;
                _connection!.CallbackExceptionAsync += OnCallbackExceptionAsync;
                _connection!.ConnectionBlockedAsync += OnConnectionBlockedAsync;
                _connection!.ConnectionUnblockedAsync += OnConnectionUnblockedAsync;

                logger.LogInformation("RabbitMQ Client acquired a persistent connection to '{HostName}'", _connection?.Endpoint.HostName);

                return Task.FromResult(IsConnected);
            }

            logger.LogError("FATAL ERROR: RabbitMQ connections could not be created and opened");

            return Task.FromResult(IsConnected);
        }
    }


    public Task<IChannel> CreateModelAsync()
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
        }

        return _connection?.CreateChannelAsync();
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        try
        {
            if (IsConnected)
            {
                if (_connection != null)
                {
                    _connection!.ConnectionShutdownAsync -= OnConnectionShutdownAsync;
                    _connection!.CallbackExceptionAsync -= OnCallbackExceptionAsync;
                    _connection!.ConnectionBlockedAsync -= OnConnectionBlockedAsync;
                    _connection!.ConnectionUnblockedAsync -= OnConnectionUnblockedAsync;

                    _connection.CloseAsync().GetAwaiter().GetResult();
                }
            }

            _connection?.Dispose();
        }
        catch (IOException ex)
        {
            logger.LogError(ex.Message);
        }
    }

    private Task OnCallbackExceptionAsync(object sender, CallbackExceptionEventArgs @event)
    {
        logger.LogWarning("A RabbitMQ connection throw exception. Trying to re-connect...");
        return TryConnectIfNotDisposed();
    }

    private Task OnConnectionShutdownAsync(object sender, ShutdownEventArgs @event)
    {
        logger.LogWarning("A RabbitMQ connection is on shutdown. Trying to re-connect...");
        return TryConnectIfNotDisposed();
    }

    private Task OnConnectionBlockedAsync(object sender, ConnectionBlockedEventArgs @event)
    {
        logger.LogWarning("A RabbitMQ connection is unblocked. Trying to re-connect...");
        return TryConnectIfNotDisposed();
    }

    private Task OnConnectionUnblockedAsync(object sender, AsyncEventArgs @event)
    {
        logger.LogWarning("A RabbitMQ connection is blocked. Trying to re-connect...");
        return TryConnectIfNotDisposed();
    }

    private Task TryConnectIfNotDisposed()
    {
        if (!_disposed) return TryConnectAsync();

        logger.LogInformation("RabbitMQ client is disposed. No action will be taken.");
        return Task.CompletedTask;
    }
}