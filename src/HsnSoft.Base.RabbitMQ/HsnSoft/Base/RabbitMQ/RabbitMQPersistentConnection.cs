using System;
using System.IO;
using System.Net.Sockets;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace HsnSoft.Base.RabbitMQ;

public class RabbitMQPersistentConnection : IRabbitMQPersistentConnection
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMQPersistentConnection> _logger;
    private readonly int _retryCount;

    [CanBeNull]
    private IConnection _connection;

    private bool _disposed;

    private readonly object _syncRoot = new();

    public RabbitMQPersistentConnection(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));

        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger<RabbitMQPersistentConnection>() ?? throw new ArgumentNullException(nameof(loggerFactory));

        var conSettings = serviceProvider.GetRequiredService<IOptions<RabbitMQConnectionSettings>>();
        _connectionFactory = new ConnectionFactory()
        {
            HostName = conSettings.Value.HostName,
            Port = conSettings.Value.Port,
            UserName = conSettings.Value.UserName,
            Password = conSettings.Value.Password,
        };
        _retryCount = conSettings.Value.ConnectionRetryCount;
    }

    public bool IsConnected => _connection is { IsOpen: true } && !_disposed;

    public IModel CreateModel()
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
        }

        return _connection?.CreateModel();
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        try
        {
            if (!IsConnected) return;
            _connection!.ConnectionShutdown -= OnConnectionShutdown;
            _connection.CallbackException -= OnCallbackException;
            _connection.ConnectionBlocked -= OnConnectionBlocked;
            _connection.Dispose();
        }
        catch (IOException ex)
        {
            _logger.LogCritical(ex.Message);
        }
    }

    public bool TryConnect()
    {
        _logger.LogInformation("RabbitMQ Client is trying to connect");

        lock (_syncRoot)
        {
            var policy = Policy.Handle<SocketException>()
                .Or<BrokerUnreachableException>()
                .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                    {
                        _logger.LogWarning(ex, "RabbitMQ Client could not connect after {TimeOut}s ({ExceptionMessage})", $"{time.TotalSeconds:n1}", ex.Message);
                    }
                );

            policy.Execute(() =>
            {
                _connection = _connectionFactory
                    .CreateConnection();
            });

            if (IsConnected)
            {
                _connection!.ConnectionShutdown += OnConnectionShutdown;
                _connection.CallbackException += OnCallbackException;
                _connection.ConnectionBlocked += OnConnectionBlocked;

                _logger.LogInformation("RabbitMQ Client acquired a persistent connection to '{HostName}'", _connection.Endpoint.HostName);

                return true;
            }

            _logger.LogCritical("FATAL ERROR: RabbitMQ connections could not be created and opened");

            return false;
        }
    }

    private void OnConnectionBlocked([CanBeNull] object sender, ConnectionBlockedEventArgs e)
    {
        if (_disposed) return;

        _logger.LogWarning("A RabbitMQ connection is shutdown. Trying to re-connect...");

        TryConnect();
    }

    void OnCallbackException([CanBeNull] object sender, CallbackExceptionEventArgs e)
    {
        if (_disposed) return;

        _logger.LogWarning("A RabbitMQ connection throw exception. Trying to re-connect...");

        TryConnect();
    }

    void OnConnectionShutdown([CanBeNull] object sender, ShutdownEventArgs reason)
    {
        if (_disposed) return;

        _logger.LogWarning("A RabbitMQ connection is on shutdown. Trying to re-connect...");

        TryConnect();
    }
}