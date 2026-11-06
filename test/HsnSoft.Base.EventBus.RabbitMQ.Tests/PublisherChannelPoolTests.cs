using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus.RabbitMQ.Tests;

public class PublisherChannelPoolTests
{
    private static EventBusRabbitMq NewBus(FakeConnection connection, int poolSize = 4)
    {
        EventBusOptions options = new() { ExchangeName = "HHS", ConsumerClientName = "OrderService", PublishRetryCount = 2, PublishRetryBaseDelay = TimeSpan.FromMilliseconds(1), ShutdownTimeout = TimeSpan.FromSeconds(5), PublisherChannelPoolSize = poolSize };
        return new EventBusRabbitMq(connection, new EventEnvelopeFactory(Options.Create(options), []), Options.Create(options), NullLogger<EventBusRabbitMq>.Instance);
    }

    [Fact]
    public async Task ManyMessages_UseOneChannel_AndDeclareTheExchangeOnce()
    {
        FakeConnection connection = new();
        await using EventBusRabbitMq bus = NewBus(connection);

        for (int message = 0; message < 5; message++)
        {
            await bus.PublishAsync(new SampleEto("a", message));
        }

        ChannelState channel = Assert.Single(connection.Channels);
        Assert.Equal(1, connection.PublisherChannels);
        Assert.Single(channel.Named("ExchangeDeclareAsync"));
        Assert.Equal(5, channel.Named("BasicPublishAsync").Count);
        Assert.False(channel.IsClosed);
    }

    [Fact]
    public async Task ASendToAService_DeclaresItsQueueOnce_OnTheChannelItKept()
    {
        FakeConnection connection = new();
        await using EventBusRabbitMq bus = NewBus(connection);

        await bus.SendAsync(new SampleEto("a", 1), "ShipmentService");
        await bus.SendAsync(new SampleEto("b", 2), "ShipmentService");
        await bus.SendAsync(new SampleEto("c", 3), "ProductService");

        ChannelState channel = Assert.Single(connection.Channels);
        Assert.Equal(2, channel.Named("QueueDeclareAsync").Count);
        Assert.Equal(3, channel.Named("BasicPublishAsync").Count);
    }

    [Fact]
    public async Task AChannelOnWhichAPublishFailed_IsClosedAndNotUsedAgain_TheNextMessageDeclaresOnANewOne()
    {
        FakeConnection connection = new();
        ChannelState failing = new() { Failure = call => call.Name == "BasicPublishAsync" ? new InvalidOperationException("nack") : null };
        connection.QueueChannelState(failing);
        await using EventBusRabbitMq bus = NewBus(connection);

        await Assert.ThrowsAsync<InvalidOperationException>(() => bus.PublishAsync(new SampleEto("a", 1)));
        await bus.PublishAsync(new SampleEto("b", 2));

        Assert.True(failing.IsClosed);
        Assert.Equal(2, connection.Channels.Count);
        Assert.Single(connection.Channels[1].Named("ExchangeDeclareAsync"));
        Assert.False(connection.Channels[1].IsClosed);
    }

    [Fact]
    public async Task AChannelThatIsNotOpenAnyMore_IsLeftOutOfThePool()
    {
        FakeConnection connection = new();
        await using EventBusRabbitMq bus = NewBus(connection);
        await bus.PublishAsync(new SampleEto("a", 1));
        connection.Channels[0].IsClosed = true;   // the connection broke

        await bus.PublishAsync(new SampleEto("b", 2));

        Assert.Equal(2, connection.Channels.Count);
        Assert.Single(connection.Channels[1].Named("BasicPublishAsync"));
    }

    [Fact]
    public async Task WithNoPool_AChannelIsMadeAndClosedForEachMessage()
    {
        FakeConnection connection = new();
        await using EventBusRabbitMq bus = NewBus(connection, poolSize: 0);

        await bus.PublishAsync(new SampleEto("a", 1));
        await bus.PublishAsync(new SampleEto("b", 2));

        Assert.Equal(2, connection.Channels.Count);
        Assert.All(connection.Channels, channel => Assert.True(channel.IsClosed));
        Assert.All(connection.Channels, channel => Assert.Single(channel.Named("ExchangeDeclareAsync")));
    }

    [Fact]
    public async Task MessagesAtTheSameMoment_NeverShareAChannel_AndThePoolKeepsOnlyItsSize()
    {
        FakeConnection connection = new();
        TaskCompletionSource hold = new(TaskCreationOptions.RunContinuationsAsynchronously);
        for (int channel = 0; channel < 5; channel++)
        {
            connection.QueueChannelState(new ChannelState { Before = call => call.Name == "BasicPublishAsync" ? hold.Task : null });
        }

        await using EventBusRabbitMq bus = NewBus(connection, poolSize: 2);
        Task[] publishing = [.. Enumerable.Range(0, 5).Select(number => bus.PublishAsync(new SampleEto("a", number)))];
        for (int wait = 0; wait < 200 && connection.Channels.Count < 5; wait++)
        {
            await Task.Delay(10);
        }

        hold.SetResult();
        await Task.WhenAll(publishing);

        Assert.Equal(5, connection.Channels.Count);
        Assert.All(connection.Channels, channel => Assert.Single(channel.Named("BasicPublishAsync")));   // one publish on each: none was shared
        Assert.Equal(2, connection.Channels.Count(channel => !channel.IsClosed));   // the pool kept its size, the rest were closed
    }

    [Fact]
    public async Task DisposingTheBus_ClosesTheChannelsItKept()
    {
        FakeConnection connection = new();
        EventBusRabbitMq bus = NewBus(connection);
        await bus.PublishAsync(new SampleEto("a", 1));

        await bus.DisposeAsync();

        Assert.All(connection.Channels, channel => Assert.True(channel.IsClosed));
    }

    [Fact]
    public async Task AChannelGivenBackAfterThePoolWasDisposed_IsClosed()
    {
        FakeConnection connection = new();
        PublisherChannelPool pool = new(connection.CreatePublisherChannelAsync, 2);
        PublisherChannelLease lease = await pool.RentAsync(CancellationToken.None);
        lease.Succeed();

        await pool.DisposeAsync();
        await lease.DisposeAsync();

        Assert.True(Assert.Single(connection.Channels).IsClosed);
        Assert.Equal(0, pool.Idle);
    }

    [Fact]
    public async Task ACloseThatFails_DoesNotMatter()
    {
        FakeConnection connection = new();
        connection.QueueChannelState(new ChannelState { Failure = call => call.Name == "CloseAsync" ? new TimeoutException("close") : null });
        PublisherChannelPool pool = new(connection.CreatePublisherChannelAsync, 1);

        PublisherChannelLease lease = await pool.RentAsync(CancellationToken.None);
        await lease.DisposeAsync();   // not succeeded: closed, and the failure of the close is not thrown
        await pool.DisposeAsync();

        Assert.Equal(0, pool.Idle);
    }
}
