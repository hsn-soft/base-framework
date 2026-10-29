using System.Net.Sockets;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus.RabbitMQ.Connection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client.Exceptions;

namespace HsnSoft.Base.EventBus.RabbitMQ.Tests;

public class FailedEventNotifierTests
{
    private static EventBusOptions NewOptions() => new()
    {
        ExchangeName = "HHS",
        ConsumerClientName = "OrderService",
        ErrorClientName = "EventManagerService",
        PublishRetryCount = 2,
        PublishRetryBaseDelay = TimeSpan.FromMilliseconds(1),
    };

    private static FailedEventNotifier NewNotifier(FakeConnection connection, bool subscribed = true)
    {
        EventBusOptions options = NewOptions();
        IEnumerable<EventSubscription> subscriptions = subscribed ? [new EventSubscription(typeof(SampleEto), typeof(SampleHandler), 1)] : [];
        InMemoryEventBusSubscriptionManager manager = new(subscriptions, Options.Create(options), NullLogger<InMemoryEventBusSubscriptionManager>.Instance);
        return new FailedEventNotifier(connection, manager, new EventEnvelopeFactory(Options.Create(options), []), Options.Create(options), NullLogger<FailedEventNotifier>.Instance);
    }

    private static string FailedMessageJson() => """
        {"HopLevel":2,"ReQueuedCount":1,"MessageId":"0190a2f4-8c1e-7b3a-9d5e-2f6a4c8e1b07","MessageTime":"2026-05-01T08:30:00Z",
         "CorrelationId":"corr-1","TraceUser":"user-7","Producer":"HHS_Other","Message":{"Name":"keyboard","Count":3}}
        """;

    private static MessageEnvelope<FailedEto> PublishedFailure(FakeConnection connection, out ChannelCall published)
    {
        published = Assert.Single(connection.Channels[0].Named("BasicPublishAsync"));
        ReadOnlyMemory<byte> body = Assert.IsType<ReadOnlyMemory<byte>>(published.Args[4]);
        return (MessageEnvelope<FailedEto>)EventSerializer.DeserializeEnvelope(body.Span, typeof(FailedEto));
    }

    [Fact]
    public async Task FailedMessage_IsReportedToTheErrorClientsQueue_WithItsTypeAndContent()
    {
        FakeConnection connection = new();

        await NewNotifier(connection).NotifyAsync("boom", "Sample", FailedMessageJson());

        MessageEnvelope<FailedEto> envelope = PublishedFailure(connection, out ChannelCall published);
        Assert.Equal(string.Empty, published.Args[0]);
        Assert.Equal("HHS_EventManagerService_Failed", published.Args[1]);
        Assert.Equal("HHS_EventManagerService_Failed", Assert.Single(connection.Channels[0].Named("QueueDeclareAsync")).Args[0]);
        Assert.Equal("boom", envelope.Message.FailedReason);
        Assert.Equal("SampleEto", envelope.Message.FailedMessageTypeName);
        Assert.Equal(new DateTime(2026, 5, 1, 8, 30, 0, DateTimeKind.Utc), envelope.Message.FailedMessageEnvelopeTime?.ToUniversalTime());
        Assert.NotNull(envelope.Message.FailedMessageObject);
    }

    [Fact]
    public async Task Report_ContinuesTheChainOfTheFailedMessage()
    {
        FakeConnection connection = new();

        await NewNotifier(connection).NotifyAsync("boom", "Sample", FailedMessageJson());

        MessageEnvelope<FailedEto> envelope = PublishedFailure(connection, out _);
        Assert.Equal(Guid.Parse("0190a2f4-8c1e-7b3a-9d5e-2f6a4c8e1b07"), envelope.ParentMessageId);
        Assert.Equal((ushort)3, envelope.HopLevel);
        Assert.Equal((ushort)1, envelope.ReQueuedCount);
        Assert.Equal("corr-1", envelope.CorrelationId);
        Assert.Equal("user-7", envelope.GetHeader("TraceUser"));
        Assert.Equal("OrderService", envelope.Producer);
    }

    [Fact]
    public async Task EventTheServiceDoesNotKnow_IsStillReported_WithTheReason()
    {
        FakeConnection connection = new();

        await NewNotifier(connection, subscribed: false).NotifyAsync("boom", "Sample", FailedMessageJson());

        MessageEnvelope<FailedEto> envelope = PublishedFailure(connection, out _);
        Assert.StartsWith("boom. FailedMessageContent convert operation error:", envelope.Message.FailedReason, StringComparison.Ordinal);
        Assert.Contains("No event type is registered for 'Sample'", envelope.Message.FailedReason, StringComparison.Ordinal);
        Assert.Null(envelope.Message.FailedMessageTypeName);
        Assert.Null(envelope.Message.FailedMessageObject);
        // the envelope itself could be read
        Assert.Equal(Guid.Parse("0190a2f4-8c1e-7b3a-9d5e-2f6a4c8e1b07"), envelope.ParentMessageId);
    }

    [Fact]
    public async Task MessageThatIsNotJson_IsReportedWithoutAChain()
    {
        FakeConnection connection = new();

        await NewNotifier(connection).NotifyAsync("boom", "Sample", "this is not json");

        MessageEnvelope<FailedEto> envelope = PublishedFailure(connection, out _);
        Assert.Contains("convert operation error", envelope.Message.FailedReason, StringComparison.Ordinal);
        Assert.Null(envelope.ParentMessageId);
        Assert.Equal((ushort)1, envelope.HopLevel);
    }

    [Fact]
    public async Task DirectReport_UsesTheGivenTypeObjectAndParent()
    {
        FakeConnection connection = new();
        ParentMessageEnvelope parent = new() { MessageId = Guid.CreateVersion7(), HopLevel = 4, CorrelationId = "c", MessageTime = DateTime.UtcNow };

        await NewNotifier(connection).NotifyDirectAsync("no handler", "SampleEto", new SampleEto("a", 1), parent);

        MessageEnvelope<FailedEto> envelope = PublishedFailure(connection, out _);
        Assert.Equal("no handler", envelope.Message.FailedReason);
        Assert.Equal("SampleEto", envelope.Message.FailedMessageTypeName);
        Assert.Equal(parent.MessageId, envelope.ParentMessageId);
        Assert.Equal((ushort)5, envelope.HopLevel);
    }

    [Fact]
    public async Task DirectReport_WithoutAParent_StartsAChain()
    {
        FakeConnection connection = new();

        await NewNotifier(connection).NotifyDirectAsync("no handler", null, null, null);

        MessageEnvelope<FailedEto> envelope = PublishedFailure(connection, out _);
        Assert.Null(envelope.Message.FailedMessageEnvelopeTime);
        Assert.Equal((ushort)1, envelope.HopLevel);
    }

    [Fact]
    public async Task NotConnected_ConnectsFirst_AndWithoutABrokerItIsAnError()
    {
        FakeConnection connected = new() { IsConnected = false };
        await NewNotifier(connected).NotifyDirectAsync("x", null, null, null);
        Assert.Equal(1, connected.ConnectCalls);

        FakeConnection unreachable = new() { IsConnected = false, ConnectSucceeds = false };
        await Assert.ThrowsAsync<RabbitMqNotConnectedException>(() => NewNotifier(unreachable).NotifyDirectAsync("x", null, null, null));
    }

    [Fact]
    public async Task BrokerThatIsNotReachable_IsRetried()
    {
        FakeConnection connection = new() { ChannelsToFail = 2, CreateChannelFailure = new SocketException() };

        await NewNotifier(connection).NotifyDirectAsync("x", null, null, null);

        Assert.Equal(3, connection.DispatchConcurrencies.Count);
        Assert.Single(connection.Channels);
    }

    [Fact]
    public async Task BrokerThatStaysUnreachable_IsAnError()
    {
        FakeConnection connection = new() { ChannelsToFail = 10, CreateChannelFailure = new BrokerUnreachableException(new IOException("down")) };

        await Assert.ThrowsAsync<BrokerUnreachableException>(() => NewNotifier(connection).NotifyDirectAsync("x", null, null, null));
    }

    [Fact]
    public async Task NullArguments_AreRejected()
    {
        FailedEventNotifier notifier = NewNotifier(new FakeConnection());

        await Assert.ThrowsAsync<ArgumentNullException>(() => notifier.NotifyAsync(null!, "Sample", "{}"));
        await Assert.ThrowsAsync<ArgumentNullException>(() => notifier.NotifyAsync("e", null!, "{}"));
        await Assert.ThrowsAsync<ArgumentNullException>(() => notifier.NotifyAsync("e", "Sample", null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => notifier.NotifyDirectAsync(null!, null, null, null));
    }
}
