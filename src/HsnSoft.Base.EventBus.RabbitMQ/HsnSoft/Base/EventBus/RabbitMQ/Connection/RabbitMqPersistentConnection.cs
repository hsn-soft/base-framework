using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.EventBus.RabbitMQ.Configs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace HsnSoft.Base.EventBus.RabbitMQ.Connection;

public sealed partial class RabbitMqPersistentConnection : IRabbitMqPersistentConnection, IAsyncDisposable
{
    private readonly RabbitMqConnectionSettings _settings;
    private readonly ILogger<RabbitMqPersistentConnection> _logger;
    private readonly IConnectionFactory _connectionFactory;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;
    private bool _disposed;

    public RabbitMqPersistentConnection(IOptions<RabbitMqConnectionSettings> settings, ILogger<RabbitMqPersistentConnection> logger)
        : this(settings, logger, CreateFactory(settings.Value))
    {
    }

    internal RabbitMqPersistentConnection(IOptions<RabbitMqConnectionSettings> settings, ILogger<RabbitMqPersistentConnection> logger, IConnectionFactory connectionFactory)
    {
        _settings = settings.Value;
        _logger = logger;
        _connectionFactory = connectionFactory;
    }

    public bool IsConnected => _connection is { IsOpen: true } && !_disposed;

    private bool IsRecovering => !_disposed && _connection is { IsOpen: false, CloseReason.Initiator: not ShutdownInitiator.Application };

    public async Task<bool> TryConnectAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // somebody else connected while this call waited; replacing a healthy connection would orphan its channels
            if (IsConnected)
            {
                return true;
            }

            // the client's automatic recovery is reconnecting this connection (it was lost, not closed by us): replacing it from here would orphan the channels and consumers it brings back at that very moment
            if (IsRecovering)
            {
                LogRecovering(_logger);
                return false;
            }

            await CloseConnectionAsync().ConfigureAwait(false);
            LogConnecting(_logger, _settings.HostName, _settings.Port);
            if (_settings is { UserName: "guest", Password: "guest" } && !IsLocal(_settings.HostName))
            {
                LogDefaultCredentials(_logger, _settings.HostName);
            }

            IConnection? connection = null;
            await PublishRetry.ExecuteAsync(
                async () => connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false),
                _settings.ConnectionRetryCount,
                _settings.ConnectionRetryBaseDelay,
                exception => exception is SocketException or BrokerUnreachableException,
                (exception, delay, retry) => LogNotReachable(_logger, exception, retry, delay.TotalSeconds),
                cancellationToken).ConfigureAwait(false);

            _connection = connection;

            // The handlers only log: the client's own recovery owns reconnecting. Replacing the connection from here would orphan
            // channels and consumers the client is recovering at that very moment.
            _connection!.ConnectionShutdownAsync += OnConnectionShutdownAsync;
            _connection.CallbackExceptionAsync += OnCallbackExceptionAsync;
            _connection.ConnectionBlockedAsync += OnConnectionBlockedAsync;
            _connection.ConnectionUnblockedAsync += OnConnectionUnblockedAsync;
            _connection.RecoverySucceededAsync += OnRecoverySucceededAsync;
            _connection.ConnectionRecoveryErrorAsync += OnConnectionRecoveryErrorAsync;
            LogConnected(_logger, _connection.Endpoint.HostName);
            return IsConnected;
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task<IChannel> CreatePublisherChannelAsync(CancellationToken cancellationToken = default)
    {
        IConnection connection = _connection is { IsOpen: true } open && !_disposed ? open : throw new RabbitMqNotConnectedException("No RabbitMQ connections are available to perform this action");
        return connection.CreateChannelAsync(new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true), cancellationToken);
    }

    public Task<IChannel> CreateModelAsync(ushort? consumerDispatchConcurrency = null, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            throw new RabbitMqNotConnectedException("No RabbitMQ connections are available to perform this action");
        }

        CreateChannelOptions? options = consumerDispatchConcurrency.HasValue
            ? new CreateChannelOptions(
                publisherConfirmationsEnabled: false,
                publisherConfirmationTrackingEnabled: false,
                consumerDispatchConcurrency: consumerDispatchConcurrency.Value)
            : null;
        return _connection!.CreateChannelAsync(options, cancellationToken);
    }

    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await CloseConnectionAsync().ConfigureAwait(false);
        _gate.Dispose();
    }

    private async Task CloseConnectionAsync()
    {
        IConnection? connection = _connection;
        _connection = null;
        if (connection is null)
        {
            return;
        }

        connection.ConnectionShutdownAsync -= OnConnectionShutdownAsync;
        connection.CallbackExceptionAsync -= OnCallbackExceptionAsync;
        connection.ConnectionBlockedAsync -= OnConnectionBlockedAsync;
        connection.ConnectionUnblockedAsync -= OnConnectionUnblockedAsync;
        connection.RecoverySucceededAsync -= OnRecoverySucceededAsync;
        connection.ConnectionRecoveryErrorAsync -= OnConnectionRecoveryErrorAsync;
        try
        {
            if (connection.IsOpen)
            {
                await connection.CloseAsync(Constants.ReplySuccess, "Goodbye", _settings.ConnectionCloseTimeout, abort: false).ConfigureAwait(false);
                LogClosed(_logger);
            }
        }
        catch (Exception exception) when (exception is IOException or AlreadyClosedException or TimeoutException or OperationCanceledException)
        {
            LogCloseFailed(_logger, exception.Message);
        }
        finally
        {
            connection.Dispose();
        }
    }

    private static ConnectionFactory CreateFactory(RabbitMqConnectionSettings settings) => RabbitMqConnectionFactory.Create(settings, automaticRecovery: true);

    internal Task OnCallbackExceptionAsync(object sender, CallbackExceptionEventArgs args)
    {
        LogCallbackException(_logger, args.Exception.Message);
        return Task.CompletedTask;
    }

    internal Task OnConnectionShutdownAsync(object sender, ShutdownEventArgs args)
    {
        LogShutdown(_logger, args.ReplyText ?? "no reason given");
        return Task.CompletedTask;
    }

    internal Task OnConnectionBlockedAsync(object sender, ConnectionBlockedEventArgs args)
    {
        // flow control of the broker (a resource alarm), not a failed connection: nothing to reconnect
        LogBlocked(_logger, args.Reason);
        return Task.CompletedTask;
    }

    internal Task OnConnectionUnblockedAsync(object sender, AsyncEventArgs args)
    {
        LogUnblocked(_logger);
        return Task.CompletedTask;
    }

    internal Task OnRecoverySucceededAsync(object sender, AsyncEventArgs args)
    {
        LogRecovered(_logger);
        return Task.CompletedTask;
    }

    internal Task OnConnectionRecoveryErrorAsync(object sender, ConnectionRecoveryErrorEventArgs args)
    {
        LogRecoveryFailed(_logger, args.Exception.Message);
        return Task.CompletedTask;
    }

    [LoggerMessage(EventId = 4112, EventName = "RABBITMQ_CONNECTION_RECOVERING", Level = LogLevel.Debug, Message = "The RabbitMQ connection is being recovered by the client, it is not replaced")]
    private static partial void LogRecovering(ILogger logger);

    // the broker refuses guest from another machine by default; one that accepts it is open to anybody who knows the default
    private static bool IsLocal(string host) => host is "localhost" or "127.0.0.1" or "::1";

    [LoggerMessage(EventId = 4113, EventName = "RABBITMQ_CONNECTION_DEFAULT_CREDENTIALS", Level = LogLevel.Warning, Message = "RabbitMQ at {HostName} is reached with the default guest credentials: set RabbitMq:Connection:UserName and Password")]
    private static partial void LogDefaultCredentials(ILogger logger, string hostName);

    [LoggerMessage(EventId = 4101, EventName = "RABBITMQ_CONNECTION_CONNECTING", Level = LogLevel.Information, Message = "RabbitMQ client is connecting to {HostName}:{Port}")]
    private static partial void LogConnecting(ILogger logger, string hostName, int port);

    [LoggerMessage(EventId = 4102, EventName = "RABBITMQ_CONNECTION_NOT_REACHABLE", Level = LogLevel.Warning, Message = "RabbitMQ is not reachable, retry {Retry} after {DelaySeconds} s")]
    private static partial void LogNotReachable(ILogger logger, Exception exception, int retry, double delaySeconds);

    [LoggerMessage(EventId = 4103, EventName = "RABBITMQ_CONNECTION_CONNECTED", Level = LogLevel.Information, Message = "RabbitMQ client holds a persistent connection to {HostName}")]
    private static partial void LogConnected(ILogger logger, string hostName);

    [LoggerMessage(EventId = 4104, EventName = "RABBITMQ_CONNECTION_CLOSED", Level = LogLevel.Debug, Message = "RabbitMQ connection is closed")]
    private static partial void LogClosed(ILogger logger);

    [LoggerMessage(EventId = 4105, EventName = "RABBITMQ_CONNECTION_CLOSE_FAILED", Level = LogLevel.Error, Message = "RabbitMQ connection could not be closed cleanly: {Error}")]
    private static partial void LogCloseFailed(ILogger logger, string error);

    [LoggerMessage(EventId = 4106, EventName = "RABBITMQ_CONNECTION_CALLBACK_EXCEPTION", Level = LogLevel.Warning, Message = "A RabbitMQ connection callback threw an exception: {Error}")]
    private static partial void LogCallbackException(ILogger logger, string error);

    [LoggerMessage(EventId = 4107, EventName = "RABBITMQ_CONNECTION_SHUTDOWN", Level = LogLevel.Warning, Message = "The RabbitMQ connection is shutting down ({Reason}); automatic recovery reconnects")]
    private static partial void LogShutdown(ILogger logger, string reason);

    [LoggerMessage(EventId = 4108, EventName = "RABBITMQ_CONNECTION_BLOCKED", Level = LogLevel.Warning, Message = "RabbitMQ blocked the connection: {Reason}")]
    private static partial void LogBlocked(ILogger logger, string reason);

    [LoggerMessage(EventId = 4109, EventName = "RABBITMQ_CONNECTION_UNBLOCKED", Level = LogLevel.Information, Message = "RabbitMQ unblocked the connection")]
    private static partial void LogUnblocked(ILogger logger);

    [LoggerMessage(EventId = 4110, EventName = "RABBITMQ_CONNECTION_RECOVERED", Level = LogLevel.Information, Message = "RabbitMQ connection recovered")]
    private static partial void LogRecovered(ILogger logger);

    [LoggerMessage(EventId = 4111, EventName = "RABBITMQ_CONNECTION_RECOVERY_FAILED", Level = LogLevel.Warning, Message = "A RabbitMQ connection recovery attempt failed, it will be repeated: {Error}")]
    private static partial void LogRecoveryFailed(ILogger logger, string error);
}
