using System.Net.Sockets;
using HsnSoft.Base.EventBus.RabbitMQ.Configs;
using HsnSoft.Base.EventBus.RabbitMQ.Connection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace HsnSoft.Base.EventBus.RabbitMQ.Tests;

public class RabbitMqConnectionTests
{
    private static (RabbitMqPersistentConnection Connection, ListLogger<RabbitMqPersistentConnection> Logger) Create(RabbitMqConnectionSettings? settings = null)
    {
        ListLogger<RabbitMqPersistentConnection> logger = new();
        return (new RabbitMqPersistentConnection(Options.Create(settings ?? new RabbitMqConnectionSettings()), logger), logger);
    }

    [Fact]
    public void Settings_HaveTheProductionDefaults()
    {
        RabbitMqConnectionSettings settings = new();

        Assert.Equal("localhost", settings.HostName);
        Assert.Equal(5672, settings.Port);
        Assert.Equal("guest", settings.UserName);
        Assert.Equal("guest", settings.Password);
        Assert.Equal("/", settings.VirtualHost);
        Assert.Equal(5, settings.ConnectionRetryCount);
        Assert.Equal(TimeSpan.FromSeconds(1), settings.ConnectionRetryBaseDelay);
        Assert.Equal(TimeSpan.FromSeconds(10), settings.ConnectionCloseTimeout);
        Assert.Equal(TimeSpan.FromSeconds(5), new RabbitMqEventBusConfig().RequeueDelay);
        Assert.Equal(TimeSpan.FromSeconds(5), new RabbitMqEventBusConfig().RecoveryProbeInterval);
    }

    [Fact]
    public async Task NewConnection_IsNotConnected_AndGivesNoChannel()
    {
        (RabbitMqPersistentConnection connection, _) = Create();

        Assert.False(connection.IsConnected);
        await Assert.ThrowsAsync<InvalidOperationException>(() => connection.CreateModelAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => connection.CreateModelAsync(4));
    }

    [Fact]
    public async Task BrokerThatIsNotThere_IsRetried_ThenReported()
    {
        (RabbitMqPersistentConnection connection, ListLogger<RabbitMqPersistentConnection> logger) = Create(new RabbitMqConnectionSettings
        {
            HostName = "127.0.0.1",
            Port = 1,
            ConnectionRetryCount = 1,
            ConnectionRetryBaseDelay = TimeSpan.FromMilliseconds(1),
        });

        await Assert.ThrowsAsync<BrokerUnreachableException>(() => connection.TryConnectAsync());

        Assert.False(connection.IsConnected);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Warning && entry.Message.Contains("not reachable, retry 1", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Dispose_IsRepeatable_AndDisconnects()
    {
        (RabbitMqPersistentConnection connection, _) = Create();

        await connection.DisposeAsync();
        connection.Dispose();
        await connection.DisposeAsync();

        Assert.False(connection.IsConnected);
    }

    [Fact]
    public async Task ConnectionEvents_AreLogged()
    {
        (RabbitMqPersistentConnection connection, ListLogger<RabbitMqPersistentConnection> logger) = Create();
        using CancellationTokenSource source = new();
        CancellationToken token = source.Token;

        await connection.OnConnectionShutdownAsync(this, new ShutdownEventArgs(ShutdownInitiator.Peer, 320, "CONNECTION_FORCED"));
        await connection.OnCallbackExceptionAsync(this, new CallbackExceptionEventArgs(new Dictionary<string, object>(), new InvalidOperationException("callback broke"), token));
        await connection.OnConnectionBlockedAsync(this, new ConnectionBlockedEventArgs("low on memory", token));
        await connection.OnConnectionUnblockedAsync(this, new AsyncEventArgs(token));
        await connection.OnRecoverySucceededAsync(this, new AsyncEventArgs(token));
        await connection.OnConnectionRecoveryErrorAsync(this, new ConnectionRecoveryErrorEventArgs(new InvalidOperationException("still down"), token));

        string[] messages = [.. logger.Entries.Select(entry => entry.Message)];
        Assert.Contains(messages, message => message.Contains("CONNECTION_FORCED", StringComparison.Ordinal));
        Assert.Contains(messages, message => message.Contains("callback broke", StringComparison.Ordinal));
        Assert.Contains(messages, message => message.Contains("low on memory", StringComparison.Ordinal));
        Assert.Contains(messages, message => message.Contains("unblocked", StringComparison.Ordinal));
        Assert.Contains(messages, message => message.Contains("recovered", StringComparison.Ordinal));
        Assert.Contains(messages, message => message.Contains("still down", StringComparison.Ordinal));
    }

    private static RabbitMqConnectionSettings FastSettings() => new()
    {
        ConnectionRetryCount = 2,
        ConnectionRetryBaseDelay = TimeSpan.FromMilliseconds(1),
        ConnectionCloseTimeout = TimeSpan.FromSeconds(3),
    };

    private static (RabbitMqPersistentConnection Connection, ConnectionFactoryProxy Factory, ListLogger<RabbitMqPersistentConnection> Logger) CreateWithFactory()
    {
        ConnectionFactoryProxy factory = ConnectionFactoryProxy.Create(out IConnectionFactory connectionFactory);
        ListLogger<RabbitMqPersistentConnection> logger = new();
        return (new RabbitMqPersistentConnection(Options.Create(FastSettings()), logger, connectionFactory), factory, logger);
    }

    private static ConnectionState Offer(ConnectionFactoryProxy factory)
    {
        ConnectionState state = new();
        factory.Connections.Enqueue(ConnectionProxy.Create(state));
        return state;
    }

    [Fact]
    public async Task Connect_OpensTheConnection_AndWatchesItsEvents()
    {
        (RabbitMqPersistentConnection connection, ConnectionFactoryProxy factory, ListLogger<RabbitMqPersistentConnection> logger) = CreateWithFactory();
        ConnectionState state = Offer(factory);

        bool connected = await connection.TryConnectAsync();

        Assert.True(connected);
        Assert.True(connection.IsConnected);
        Assert.Equal(
            ["add_ConnectionShutdownAsync", "add_CallbackExceptionAsync", "add_ConnectionBlockedAsync", "add_ConnectionUnblockedAsync", "add_RecoverySucceededAsync", "add_ConnectionRecoveryErrorAsync"],
            state.Calls.Select(call => call.Name).Where(name => name.StartsWith("add_", StringComparison.Ordinal)));
        Assert.Contains(logger.Entries, entry => entry.Message.Contains("persistent connection to fake-host", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Connect_WhileConnected_KeepsTheConnection()
    {
        (RabbitMqPersistentConnection connection, ConnectionFactoryProxy factory, _) = CreateWithFactory();
        Offer(factory);
        await connection.TryConnectAsync();

        bool again = await connection.TryConnectAsync();

        Assert.True(again);
        Assert.Single(factory.Created);
    }

    [Fact]
    public async Task Channel_IsOpenedWithOrWithoutDispatchConcurrency()
    {
        (RabbitMqPersistentConnection connection, ConnectionFactoryProxy factory, _) = CreateWithFactory();
        ConnectionState state = Offer(factory);
        await connection.TryConnectAsync();

        await connection.CreateModelAsync();
        await connection.CreateModelAsync(4);

        IReadOnlyList<ChannelCall> opened = state.Named("CreateChannelAsync");
        Assert.Null(opened[0].Args[0]);
        CreateChannelOptions options = Assert.IsType<CreateChannelOptions>(opened[1].Args[0]);
        Assert.Equal((ushort)4, options.ConsumerDispatchConcurrency);
        Assert.False(options.PublisherConfirmationsEnabled);
    }

    [Fact]
    public async Task ConnectionThatIsGone_IsReplaced_AndTheOldOneIsReleased()
    {
        (RabbitMqPersistentConnection connection, ConnectionFactoryProxy factory, _) = CreateWithFactory();
        ConnectionState first = Offer(factory);
        await connection.TryConnectAsync();
        first.IsOpen = false;
        Assert.False(connection.IsConnected);
        ConnectionState second = Offer(factory);

        bool connected = await connection.TryConnectAsync();

        Assert.True(connected);
        Assert.Equal(2, factory.Created.Count);
        Assert.Equal(6, first.Calls.Count(call => call.Name.StartsWith("remove_", StringComparison.Ordinal)));
        Assert.Empty(first.Named("CloseAsync"));
        Assert.Single(first.Named("Dispose"));
        Assert.Empty(second.Named("CloseAsync"));
    }

    [Fact]
    public async Task BrokerThatIsStillStarting_IsRetried()
    {
        (RabbitMqPersistentConnection connection, ConnectionFactoryProxy factory, ListLogger<RabbitMqPersistentConnection> logger) = CreateWithFactory();
        factory.Failures.Enqueue(new SocketException());
        Offer(factory);

        bool connected = await connection.TryConnectAsync();

        Assert.True(connected);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Warning && entry.Message.Contains("retry 1", StringComparison.Ordinal));
    }

    [Fact]
    public async Task BrokerThatNeverComesUp_IsReportedAfterTheRetries()
    {
        (RabbitMqPersistentConnection connection, ConnectionFactoryProxy factory, _) = CreateWithFactory();
        for (int attempt = 0; attempt < 3; attempt++)
        {
            factory.Failures.Enqueue(new SocketException());
        }

        await Assert.ThrowsAsync<SocketException>(() => connection.TryConnectAsync());

        Assert.False(connection.IsConnected);
        Assert.Empty(factory.Created);
    }

    [Fact]
    public async Task Dispose_ClosesTheConnectionWithinTheConfiguredTimeout()
    {
        (RabbitMqPersistentConnection connection, ConnectionFactoryProxy factory, _) = CreateWithFactory();
        ConnectionState state = Offer(factory);
        await connection.TryConnectAsync();

        await connection.DisposeAsync();

        ChannelCall close = Assert.Single(state.Named("CloseAsync"));
        Assert.Equal((ushort)Constants.ReplySuccess, close.Args[0]);
        Assert.Equal(TimeSpan.FromSeconds(3), close.Args[2]);
        Assert.Equal(false, close.Args[3]);
        Assert.Equal(6, state.Calls.Count(call => call.Name.StartsWith("remove_", StringComparison.Ordinal)));
        Assert.Single(state.Named("Dispose"));
        Assert.False(connection.IsConnected);
    }

    [Theory]
    [InlineData(typeof(IOException))]
    [InlineData(typeof(TimeoutException))]
    [InlineData(typeof(OperationCanceledException))]
    public async Task Dispose_SurvivesAConnectionThatCannotBeClosed(Type failure)
    {
        (RabbitMqPersistentConnection connection, ConnectionFactoryProxy factory, ListLogger<RabbitMqPersistentConnection> logger) = CreateWithFactory();
        ConnectionState state = Offer(factory);
        state.CloseFailure = (Exception)Activator.CreateInstance(failure, "close failed")!;
        await connection.TryConnectAsync();

        await connection.DisposeAsync();

        Assert.Single(state.Named("Dispose"));
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("could not be closed cleanly", StringComparison.Ordinal));
    }
}
