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

public class RabbitMqPersistentConnection(IOptions<RabbitMqConnectionSettings> conSettings, IEventBusLogger logger) : IRabbitMqPersistentConnection
{
    private readonly IConnectionFactory _connectionFactory = new ConnectionFactory
    {
        HostName = conSettings.Value.HostName,
        Port = conSettings.Value.Port,
        UserName = conSettings.Value.UserName,
        Password = conSettings.Value.Password,
        VirtualHost = conSettings.Value.VirtualHost,
        RequestedHeartbeat = TimeSpan.FromSeconds(30),
        //DispatchConsumersAsync = true,
        AutomaticRecoveryEnabled = true,
        NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
    };

    private readonly int _retryCount = conSettings.Value.ConnectionRetryCount;

    private IConnection _connection;
    private bool _disposed;

    private readonly Lock _syncRoot = new();

    //DispatchConsumersAsync = true,

    public bool IsConnected => _connection is { IsOpen: true } && !_disposed;

    public async Task<bool> TryConnectAsync()
    {
        var conCount = await GetRabbitMqConnectionCountAsync();
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
                _connection!.ConnectionShutdownAsync += OnConnectionShutdownAsync;
                _connection!.CallbackExceptionAsync += OnCallbackExceptionAsync;
                _connection!.ConnectionBlockedAsync += OnConnectionBlockedAsync;
                _connection!.ConnectionUnblockedAsync += OnConnectionUnblockedAsync;

                logger.LogInformation("RabbitMQ Client acquired a persistent connection to '{HostName}'", _connection?.Endpoint.HostName);

                return IsConnected;
            }

            logger.LogError("FATAL ERROR: RabbitMQ connections could not be created and opened");

            return IsConnected;
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

    public async Task<int> GetRabbitMqConnectionCountAsync()
    {
        var connections = new List<object>();
        try
        {
            using var httpClient = new HttpClient();

            var request = new HttpRequestMessage(HttpMethod.Get, $"http://{conSettings.Value.HostName}:{conSettings.Value.Port}/api/connections");
            var byteArray = System.Text.Encoding.ASCII.GetBytes($"{conSettings.Value.UserName}:{conSettings.Value.Password}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            using var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

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