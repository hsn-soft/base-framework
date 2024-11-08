using System;
using System.IO;
using System.Net.Sockets;
using HsnSoft.Base.EventBus.Logging;
using HsnSoft.Base.EventBus.RabbitMQ.Configs;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace HsnSoft.Base.EventBus.RabbitMQ.Connection;

public sealed class RabbitMqPersistentConnection : IRabbitMqPersistentConnection
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly IEventBusLogger _logger;
    private const int RetryCount = 5;

    [CanBeNull]
    private IConnection _connection;

    private bool _disposed;

    private readonly object _syncRoot = new();

    public RabbitMqPersistentConnection(IOptions<RabbitMqConnectionSettings> conSettings, IEventBusLogger logger)
    {
        _logger = logger;
        _connectionFactory = new ConnectionFactory
        {
            HostName = conSettings.Value.HostName,
            Port = conSettings.Value.Port,
            UserName = conSettings.Value.UserName,
            Password = conSettings.Value.Password,
            VirtualHost = conSettings.Value.VirtualHost,
            RequestedHeartbeat = TimeSpan.FromSeconds(60),
            DispatchConsumersAsync = true,
        };
    }

    public bool IsConnected => _connection is { IsOpen: true } && !_disposed;

    public bool TryConnect()
    {
        _logger.LogDebug("{BrokerName} | Client is trying to connect", "RabbitMQ");

        lock (_syncRoot)
        {
            var policy = Policy.Handle<SocketException>()
                .Or<BrokerUnreachableException>()
                .WaitAndRetry(RetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                    {
                        _logger.LogWarning("{BrokerName} | Client could not connect after {TimeOut}s ({ExceptionMessage})", "RabbitMQ", $"{time.TotalSeconds:n1}", ex.Message);
                    }
                );

            policy.Execute(() =>
            {
                _connection = _connectionFactory.CreateConnection();
            });

            if (IsConnected)
            {
                _connection!.ConnectionShutdown += OnConnectionShutdown;
                _connection!.CallbackException += OnCallbackException;
                _connection!.ConnectionBlocked += OnConnectionBlocked;
                _connection!.ConnectionUnblocked += OnConnectionUnblocked;

                _logger.LogDebug("{BrokerName} | Client acquired a persistent connection to '{HostName}'", "RabbitMQ", _connection?.Endpoint?.HostName);

                return true;
            }

            _logger.LogError("{BrokerName} | Connections could not be created and opened", "RabbitMQ");

            return false;
        }
    }

    public IModel CreateModel()
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("RabbitMQ | No connections are available to perform this action");
        }

        return _connection?.CreateModel();
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        try
        {
            if (_connection != null)
            {
                _connection.ConnectionShutdown -= OnConnectionShutdown;
                _connection.CallbackException -= OnCallbackException;
                _connection.ConnectionBlocked -= OnConnectionBlocked;
                _connection.ConnectionUnblocked -= OnConnectionUnblocked;
                if (_connection.IsOpen)
                {
                    _connection.Close();
                    _logger.LogDebug("{BrokerName} | Client connection is closed", "RabbitMQ");
                }
            }

            _connection?.Dispose();
            _logger.LogDebug("{BrokerName} | Client is terminated", "RabbitMQ");
        }
        catch (IOException ex)
        {
            _logger.LogError("{BrokerName} | {ConnectionError}", "RabbitMQ", ex.Message);
        }
    }

    private void OnCallbackException(object sender, CallbackExceptionEventArgs args)
    {
        _logger.LogWarning("{BrokerName} | Connection throw exception. Trying to re-connect...", "RabbitMQ");
        TryConnectIfNotDisposed();
    }

    private void OnConnectionShutdown(object sender, ShutdownEventArgs args)
    {
        _logger.LogWarning("{BrokerName} | Connection is on shutdown. Trying to re-connect...", "RabbitMQ");
        TryConnectIfNotDisposed();
    }

    private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs args)
    {
        if (_disposed) return;

        _logger.LogWarning("{BrokerName} | Connection is blocked. Trying to re-connect...", "RabbitMQ");
        TryConnectIfNotDisposed();
    }

    private void OnConnectionUnblocked(object sender, EventArgs args)
    {
        _logger.LogWarning("{BrokerName} | Connection is unblocked. Trying to re-connect...", "RabbitMQ");
        TryConnectIfNotDisposed();
    }

    private void TryConnectIfNotDisposed()
    {
        if (_disposed)
        {
            _logger.LogDebug("{BrokerName} | Client is terminating. No action will be taken.", "RabbitMQ");
            return;
        }

        TryConnect();
    }
}