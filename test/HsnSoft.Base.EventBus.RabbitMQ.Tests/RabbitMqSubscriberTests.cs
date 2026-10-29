using HsnSoft.Base.EventBus.RabbitMQ.Configs;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus.RabbitMQ.Tests;

public class RabbitMqSubscriberTests
{
    private static (RabbitMqSubscriber Subscriber, FakeConnection Connection) Create(bool connected, params EventSubscription[] subscriptions)
    {
        EventBusOptions options = new() { ExchangeName = "HHS", ConsumerClientName = "OrderService", ShutdownTimeout = TimeSpan.FromSeconds(1) };
        InMemoryEventBusSubscriptionManager manager = new(subscriptions, Options.Create(options), NullLogger<InMemoryEventBusSubscriptionManager>.Instance);
        FakeConnection connection = new() { IsConnected = connected };
        RabbitMqSubscriber subscriber = new(
            connection,
            manager,
            new FakeDispatcher(),
            new FakeNotifier(),
            Options.Create(options),
            Options.Create(new RabbitMqEventBusConfig { RecoveryProbeInterval = TimeSpan.FromMilliseconds(20) }),
            NullLoggerFactory.Instance);
        return (subscriber, connection);
    }

    [Fact]
    public async Task WithoutSubscriptions_NothingIsOpened()
    {
        (RabbitMqSubscriber subscriber, FakeConnection connection) = Create(connected: false);

        await subscriber.StartAsync(CancellationToken.None);
        await subscriber.StopAsync(CancellationToken.None);

        Assert.Equal(0, connection.ConnectCalls);
        Assert.Empty(connection.Channels);
    }

    [Fact]
    public async Task EveryEvent_GetsItsOwnConsumerAndChannel()
    {
        (RabbitMqSubscriber subscriber, FakeConnection connection) = Create(
            connected: true,
            new EventSubscription(typeof(SampleEto), typeof(SampleHandler), 2),
            new EventSubscription(typeof(OtherEto), typeof(SampleHandler), 1));

        await subscriber.StartAsync(CancellationToken.None);

        Assert.Equal(2, connection.Channels.Count);
        Assert.Equal(0, connection.ConnectCalls);
        Assert.Equal([(ushort?)2, (ushort?)1], connection.DispatchConcurrencies);
        Assert.Equal(
            ["HHS_OrderService_Sample", "HHS_OrderService_Other"],
            connection.Channels.Select(channel => (string)channel.Named("QueueDeclareAsync").Single().Args[0]!));
        Assert.All(connection.Channels, channel => Assert.Single(channel.Named("BasicConsumeAsync")));

        await subscriber.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task NotConnected_ConnectsBeforeConsuming()
    {
        (RabbitMqSubscriber subscriber, FakeConnection connection) = Create(false, new EventSubscription(typeof(SampleEto), typeof(SampleHandler), 1));

        await subscriber.StartAsync(CancellationToken.None);

        Assert.Equal(1, connection.ConnectCalls);
        Assert.Single(connection.Channels);

        await subscriber.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Stop_ClosesEveryChannel_AndCanBeRepeated()
    {
        (RabbitMqSubscriber subscriber, FakeConnection connection) = Create(
            connected: true,
            new EventSubscription(typeof(SampleEto), typeof(SampleHandler), 1),
            new EventSubscription(typeof(OtherEto), typeof(SampleHandler), 1));
        await subscriber.StartAsync(CancellationToken.None);

        await subscriber.StopAsync(CancellationToken.None);
        await subscriber.StopAsync(CancellationToken.None);

        Assert.All(connection.Channels, channel =>
        {
            Assert.Single(channel.Named("CloseAsync"));
            Assert.True(channel.IsClosed);
        });
    }

    [Fact]
    public async Task BrokerThatCannotBeReached_FailsTheStart()
    {
        (RabbitMqSubscriber subscriber, FakeConnection connection) = Create(false, new EventSubscription(typeof(SampleEto), typeof(SampleHandler), 1));
        connection.ConnectFailure = new InvalidOperationException("no broker");

        await Assert.ThrowsAsync<InvalidOperationException>(() => subscriber.StartAsync(CancellationToken.None));
    }
}
