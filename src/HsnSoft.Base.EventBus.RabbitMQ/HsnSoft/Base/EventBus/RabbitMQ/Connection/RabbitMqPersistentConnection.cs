using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
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

public sealed class RabbitMqPersistentConnection(IOptions<RabbitMqConnectionSettings> conSettings, IEventBusLogger logger) : IRabbitMqPersistentConnection
{
    private readonly IConnectionFactory _connectionFactory = new ConnectionFactory
    {
        HostName = conSettings.Value.HostName,
        Port = conSettings.Value.Port,
        UserName = conSettings.Value.UserName,
        Password = conSettings.Value.Password,
        VirtualHost = conSettings.Value.VirtualHost,

        //DispatchConsumersAsync = true,
        AutomaticRecoveryEnabled = true,
        TopologyRecoveryEnabled = true,
        NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
        RequestedHeartbeat = TimeSpan.FromSeconds(60),
        HandshakeContinuationTimeout = TimeSpan.FromMinutes(2),
        ContinuationTimeout = TimeSpan.FromMinutes(2)
    };

    private readonly int _retryCount = conSettings.Value.ConnectionRetryCount;

    private IConnection _connection;
    private bool _disposed;

    private readonly Lock _syncRoot = new();

    public bool IsConnected => _connection is { IsOpen: true } && !_disposed;

    public async Task<bool> TryConnectAsync()
    {
        int conCount = await GetRabbitMqConnectionCountAsync();
        logger.LogInformation("RabbitMQ Connection Count [{Count}]", conCount);
        logger.LogInformation("RabbitMQ Client is trying to connect");

        lock (_syncRoot)
        {
            try
            {
                if (_connection != null)
                {
                    _connection!.ConnectionShutdownAsync -= OnConnectionShutdownAsync;
                    _connection!.CallbackExceptionAsync -= OnCallbackExceptionAsync;
                    _connection!.ConnectionBlockedAsync -= OnConnectionBlockedAsync;
                    _connection!.ConnectionUnblockedAsync -= OnConnectionUnblockedAsync;
                    _connection!.RecoverySucceededAsync -= OnRecoverySucceededAsync;
                    _connection!.ConnectionRecoveryErrorAsync -= OnConnectionRecoveryErrorAsync;

                    _connection.CloseAsync().GetAwaiter().GetResult();
                }
            }
            catch (IOException ex)
            {
                logger.LogError(ex.Message);
            }
            finally
            {
                _connection?.Dispose();
            }

            var policy = Policy.Handle<SocketException>()
                .Or<BrokerUnreachableException>()
                .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (ex, time) => { logger.LogWarning("RabbitMQ Client could not connect after {TimeOut}s ({ExceptionMessage})", $"{time.TotalSeconds:n1}", ex.Message); }
                );

            policy.Execute(async () => { _connection = await _connectionFactory.CreateConnectionAsync(); }).GetAwaiter().GetResult();

            if (IsConnected)
            {
                // AutomaticRecoveryEnabled=true (set above) means RabbitMQ.Client's own AutorecoveringConnection
                // retries forever (every NetworkRecoveryInterval) and transparently recovers connection,
                // topology, channels, and consumers with zero app involvement. These 4 handlers are
                // deliberately log-only — they must NOT trigger TryConnectAsync(), which would tear down and
                // replace the whole connection object mid-recovery, orphaning channels/consumers that the
                // library was already in the process of recovering. RecoverySucceededAsync/
                // ConnectionRecoveryErrorAsync give visibility into that recovery without interfering with it.
                _connection!.ConnectionShutdownAsync += OnConnectionShutdownAsync;
                _connection!.CallbackExceptionAsync += OnCallbackExceptionAsync;
                _connection!.ConnectionBlockedAsync += OnConnectionBlockedAsync;
                _connection!.ConnectionUnblockedAsync += OnConnectionUnblockedAsync;
                _connection!.RecoverySucceededAsync += OnRecoverySucceededAsync;
                _connection!.ConnectionRecoveryErrorAsync += OnConnectionRecoveryErrorAsync;

                logger.LogInformation("RabbitMQ Client acquired a persistent connection to '{HostName}'", _connection?.Endpoint.HostName);

                return IsConnected;
            }

            logger.LogError("FATAL ERROR: RabbitMQ connections could not be created and opened");

            return IsConnected;
        }
    }


    public Task<IChannel> CreateModelAsync(ushort? consumerDispatchConcurrency = null) => !IsConnected
        ? throw new InvalidOperationException("No RabbitMQ connections are available to perform this action")
        : _connection?.CreateChannelAsync(consumerDispatchConcurrency.HasValue
            ? new CreateChannelOptions(publisherConfirmationsEnabled: false, publisherConfirmationTrackingEnabled: false, consumerDispatchConcurrency: consumerDispatchConcurrency.Value)
            : null);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            if (_connection != null)
            {
                _connection!.ConnectionShutdownAsync -= OnConnectionShutdownAsync;
                _connection!.CallbackExceptionAsync -= OnCallbackExceptionAsync;
                _connection!.ConnectionBlockedAsync -= OnConnectionBlockedAsync;
                _connection!.ConnectionUnblockedAsync -= OnConnectionUnblockedAsync;
                _connection!.RecoverySucceededAsync -= OnRecoverySucceededAsync;
                _connection!.ConnectionRecoveryErrorAsync -= OnConnectionRecoveryErrorAsync;
                if (_connection.IsOpen)
                {
                    _connection.CloseAsync().GetAwaiter().GetResult();
                    logger.LogDebug("RabbitMQ | Client connection is closed");
                }
            }

            _connection?.Dispose();
        }
        catch (IOException ex)
        {
            logger.LogError(ex.Message);
        }
    }

    public async Task<int> GetRabbitMqConnectionCountAsync()
    {
        var connections = new List<object>();
        try
        {
            using var httpClient = new HttpClient();

            var request = new HttpRequestMessage(HttpMethod.Get, $"http://{conSettings.Value.HostName}:{conSettings.Value.Port}/api/connections");
            byte[] byteArray = System.Text.Encoding.ASCII.GetBytes($"{conSettings.Value.UserName}:{conSettings.Value.Password}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            using var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();

            connections = System.Text.Json.JsonSerializer.Deserialize<List<object>>(json);
        }
        catch (Exception)
        {
            // Ignore
        }
        finally
        {
            connections ??= [];
        }

        return connections.Count;
    }

    // Log-only: AutomaticRecoveryEnabled's own infinite-retry loop (every NetworkRecoveryInterval) owns
    // reconnection now — these handlers must not call TryConnectAsync(), which would tear down and
    // replace the whole connection object mid-recovery. See the comment above where these are wired up.
    private Task OnCallbackExceptionAsync(object sender, CallbackExceptionEventArgs @event)
    {
        logger.LogWarning("A RabbitMQ connection callback threw an exception: {Error}", @event?.Exception.Message ?? "UNKNOWN");
        return Task.CompletedTask;
    }

    private Task OnConnectionShutdownAsync(object sender, ShutdownEventArgs @event)
    {
        logger.LogWarning("A RabbitMQ connection is shutting down. Automatic recovery will handle reconnection.");
        return Task.CompletedTask;
    }

    private Task OnConnectionBlockedAsync(object sender, ConnectionBlockedEventArgs @event)
    {
        // Broker-side flow control (e.g. a resource alarm), not a connection failure — no reconnect needed.
        logger.LogWarning("RabbitMQ connection blocked by broker: {Reason}", @event?.Reason ?? "UNKNOWN");
        return Task.CompletedTask;
    }

    private Task OnConnectionUnblockedAsync(object sender, AsyncEventArgs @event)
    {
        logger.LogInformation("RabbitMQ connection unblocked by broker");
        return Task.CompletedTask;
    }

    private Task OnRecoverySucceededAsync(object sender, AsyncEventArgs @event)
    {
        logger.LogInformation("RabbitMQ connection automatic recovery succeeded");
        return Task.CompletedTask;
    }

    private Task OnConnectionRecoveryErrorAsync(object sender, ConnectionRecoveryErrorEventArgs @event)
    {
        logger.LogWarning("RabbitMQ connection automatic recovery attempt failed, will retry: {Error}", @event?.Exception.Message ?? "UNKNOWN");
        return Task.CompletedTask;
    }
}