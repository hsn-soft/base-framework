using System.Net.Sockets;
using HsnSoft.Base.Domain.Entities.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace HsnSoft.Base.EventBus.RabbitMQ.Tests;

public class EventBusRabbitMqTests
{
    private static EventBusOptions NewOptions() => new()
    {
        ExchangeName = "HHS",
        ConsumerClientName = "OrderService",
        PublishRetryCount = 2,
        PublishRetryBaseDelay = TimeSpan.FromMilliseconds(1),
        ShutdownTimeout = TimeSpan.FromSeconds(5),
    };

    private static EventBusRabbitMq NewBus(FakeConnection connection, EventBusOptions? options = null)
    {
        EventBusOptions bus = options ?? NewOptions();
        return new EventBusRabbitMq(connection, new EventEnvelopeFactory(Options.Create(bus), []), Options.Create(bus), NullLogger<EventBusRabbitMq>.Instance);
    }

    [Fact]
    public async Task ExchangeEvent_GoesToTheDirectExchange_WithTheEventNameAsRoutingKey()
    {
        FakeConnection connection = new();
        EventBusRabbitMq bus = NewBus(connection);

        await bus.PublishAsync(new SampleEto("a", 1), correlationId: "corr");

        ChannelState channel = Assert.Single(connection.Channels);
        ChannelCall declared = Assert.Single(channel.Named("ExchangeDeclareAsync"));
        Assert.Equal("HHS", declared.Args[0]);
        Assert.Equal("direct", declared.Args[1]);
        Assert.Equal(false, declared.Args[2]);
        ChannelCall published = Assert.Single(channel.Named("BasicPublishAsync"));
        Assert.Equal("HHS", published.Args[0]);
        Assert.Equal("Sample", published.Args[1]);
        Assert.Equal(true, published.Args[2]);
        BasicProperties properties = Assert.IsType<BasicProperties>(published.Args[3]);
        Assert.Equal(DeliveryModes.Persistent, properties.DeliveryMode);
        Assert.Equal("corr", properties.CorrelationId);
        ReadOnlyMemory<byte> body = Assert.IsType<ReadOnlyMemory<byte>>(published.Args[4]);
        MessageEnvelope<SampleEto> envelope = (MessageEnvelope<SampleEto>)EventSerializer.DeserializeEnvelope(body.Span, typeof(SampleEto));
        Assert.Equal(new SampleEto("a", 1), envelope.Message);
        Assert.Equal(envelope.MessageId.ToString(), properties.MessageId);
        Assert.Empty(channel.Named("QueueDeclareAsync"));
        Assert.True(channel.IsClosed);
    }

    [Fact]
    public async Task EventNotForTheExchange_GoesStraightToTheServicesOwnQueue()
    {
        FakeConnection connection = new();

        await NewBus(connection).PublishAsync(new SampleEto("a", 1), isExchangeEvent: false);

        ChannelState channel = Assert.Single(connection.Channels);
        ChannelCall queue = Assert.Single(channel.Named("QueueDeclareAsync"));
        Assert.Equal("HHS_OrderService_Sample", queue.Args[0]);
        Assert.Equal(true, queue.Args[1]);
        ChannelCall published = Assert.Single(channel.Named("BasicPublishAsync"));
        Assert.Equal(string.Empty, published.Args[0]);
        Assert.Equal("HHS_OrderService_Sample", published.Args[1]);
        Assert.Empty(channel.Named("ExchangeDeclareAsync"));
    }

    [Fact]
    public async Task ReQueue_GoesToTheQueueOfTheConsumerThatFailed_AndCountsTheRequeue()
    {
        FakeConnection connection = new();
        ReQueuedEto reQueued = new("HHS_PaymentService", new SampleEto("a", 1), "SampleEto");

        await NewBus(connection).PublishAsync(reQueued, isExchangeEvent: false, isReQueuePublish: true);

        ChannelCall published = Assert.Single(connection.Channels[0].Named("BasicPublishAsync"));
        Assert.Equal(string.Empty, published.Args[0]);
        Assert.Equal("HHS_PaymentService_ReQueued", published.Args[1]);
        ReadOnlyMemory<byte> body = Assert.IsType<ReadOnlyMemory<byte>>(published.Args[4]);
        MessageEnvelope<ReQueuedEto> envelope = (MessageEnvelope<ReQueuedEto>)EventSerializer.DeserializeEnvelope(body.Span, typeof(ReQueuedEto));
        Assert.Equal((ushort)1, envelope.ReQueuedCount);
        Assert.Equal("HHS_PaymentService", envelope.Message.ReQueuedMessageEnvelopeConsumer);
    }

    [Fact]
    public async Task ReQueueForThisService_IsNotAnExchangeEither()
    {
        FakeConnection connection = new();

        await NewBus(connection).PublishAsync(new SampleEto("a", 1), isExchangeEvent: true, isReQueuePublish: true);

        ChannelCall published = Assert.Single(connection.Channels[0].Named("BasicPublishAsync"));
        Assert.Equal(string.Empty, published.Args[0]);
        Assert.Equal("HHS_OrderService_Sample", published.Args[1]);
    }

    [Fact]
    public async Task NotConnected_ConnectsFirst()
    {
        FakeConnection connection = new() { IsConnected = false };

        await NewBus(connection).PublishAsync(new SampleEto("a", 1));

        Assert.Equal(1, connection.ConnectCalls);
        Assert.Single(connection.Channels);
    }

    [Fact]
    public async Task BrokerThatCannotBeConnected_IsAnError()
    {
        FakeConnection connection = new() { IsConnected = false, ConnectSucceeds = false };

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() => NewBus(connection).PublishAsync(new SampleEto("a", 1)));

        Assert.Contains("could not be established", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BrokerThatIsNotReachable_IsRetried()
    {
        FakeConnection connection = new() { ChannelsToFail = 2, CreateChannelFailure = new BrokerUnreachableException(new IOException("down")) };

        await NewBus(connection).PublishAsync(new SampleEto("a", 1));

        Assert.Single(connection.Channels);
        Assert.Equal(3, connection.DispatchConcurrencies.Count);
    }

    [Fact]
    public async Task SocketFailure_IsRetriedAndThenThrown()
    {
        FakeConnection connection = new() { ChannelsToFail = 10, CreateChannelFailure = new SocketException() };

        await Assert.ThrowsAsync<SocketException>(() => NewBus(connection).PublishAsync(new SampleEto("a", 1)));

        Assert.Equal(3, connection.DispatchConcurrencies.Count);
    }

    [Fact]
    public async Task OtherFailure_IsNotRetried()
    {
        FakeConnection connection = new() { ChannelsToFail = 10, CreateChannelFailure = new InvalidOperationException("no") };

        await Assert.ThrowsAsync<InvalidOperationException>(() => NewBus(connection).PublishAsync(new SampleEto("a", 1)));

        Assert.Single(connection.DispatchConcurrencies);
    }

    [Fact]
    public async Task Dispose_WaitsForThePublishingInFlight()
    {
        FakeConnection connection = new();
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ChannelState channel = new() { Before = call => call.Name == "BasicPublishAsync" ? release.Task : null };
        connection.QueueChannelState(channel);
        EventBusRabbitMq bus = NewBus(connection);

        Task publishing = bus.PublishAsync(new SampleEto("a", 1));
        await Until(() => channel.Named("BasicPublishAsync").Count == 1);
        Task disposing = bus.DisposeAsync().AsTask();
        await Task.Delay(250);
        Assert.False(disposing.IsCompleted);

        release.SetResult();
        await publishing;
        await disposing;
    }

    [Fact]
    public async Task Dispose_GivesUpAfterTheShutdownTimeout_AndIsRepeatable()
    {
        FakeConnection connection = new();
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ChannelState channel = new() { Before = call => call.Name == "BasicPublishAsync" ? release.Task : null };
        connection.QueueChannelState(channel);
        EventBusOptions options = NewOptions();
        options.ShutdownTimeout = TimeSpan.FromMilliseconds(150);
        EventBusRabbitMq bus = NewBus(connection, options);

        Task publishing = bus.PublishAsync(new SampleEto("a", 1));
        await Until(() => channel.Named("BasicPublishAsync").Count == 1);

        await bus.DisposeAsync();
        await bus.DisposeAsync();

        release.SetResult();
        await publishing;
    }

    internal static async Task Until(Func<bool> condition, int timeoutMs = 5000)
    {
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "Timed out waiting for a condition.");
            await Task.Delay(10);
        }
    }
}
