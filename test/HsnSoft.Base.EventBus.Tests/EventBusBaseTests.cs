using HsnSoft.Base.Domain.Entities.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus.Tests;

public class EventBusBaseTests
{
    private sealed class FakeBus(EventBusOptions options, params IEventHeaderProvider[] providers)
        : EventBusBase(new EventEnvelopeFactory(Options.Create(options), providers), Options.Create(options), NullLogger.Instance)
    {
        public List<PublishRequest> Sent { get; } = [];

        public int FailFirst { get; set; }

        public bool Transient { get; set; } = true;

        public CancellationToken LastToken { get; private set; }

        protected override Task SendAsync(PublishRequest request, CancellationToken cancellationToken)
        {
            LastToken = cancellationToken;
            if (Sent.Count < FailFirst)
            {
                Sent.Add(request);
                throw new InvalidOperationException("broker down");
            }

            Sent.Add(request);
            return Task.CompletedTask;
        }

        protected override bool IsTransient(Exception exception) => Transient;
    }

    /// <summary>Does not say which failures are transient, so nothing is retried.</summary>
    private sealed class StrictBus(EventBusOptions options)
        : EventBusBase(new EventEnvelopeFactory(Options.Create(options), []), Options.Create(options), NullLogger.Instance)
    {
        public int Calls { get; private set; }

        protected override Task SendAsync(PublishRequest request, CancellationToken cancellationToken)
        {
            Calls++;
            throw new InvalidOperationException("down");
        }
    }

    private static EventBusOptions FastOptions() => new() { ConsumerClientName = "OrderService", ExchangeName = "HHS", PublishRetryCount = 2, PublishRetryBaseDelay = TimeSpan.FromMilliseconds(1) };

    [Fact]
    public async Task PublishedMessage_IsSentWithItsEventNameEnvelopeAndBody()
    {
        FakeBus bus = new(FastOptions());

        await bus.PublishAsync(new SampleEto("a", 1), correlationId: "corr", isExchangeEvent: false, isReQueuePublish: true);

        PublishRequest request = Assert.Single(bus.Sent);
        Assert.Equal("Sample", request.EventName);
        Assert.Equal(new SampleEto("a", 1), request.Message);
        Assert.Equal("corr", request.Envelope.CorrelationId);
        Assert.False(request.IsExchangeEvent);
        Assert.True(request.IsReQueuePublish);
        Assert.Equal((ushort)1, request.Envelope.ReQueuedCount);
        MessageEnvelope<SampleEto> onTheWire = (MessageEnvelope<SampleEto>)EventSerializer.DeserializeEnvelope(request.Body, typeof(SampleEto));
        Assert.Equal(request.Envelope.MessageId, onTheWire.MessageId);
        Assert.Equal(new SampleEto("a", 1), onTheWire.Message);
    }

    [Fact]
    public async Task Publish_FollowsTheMessageBeingHandled()
    {
        FakeBus bus = new(FastOptions(), new FixedHeaderProvider("provided", ("UserId", "u1")));
        ParentMessageEnvelope handling = new() { MessageId = Guid.CreateVersion7(), HopLevel = 1, CorrelationId = "chain" };

        using (EventContext.Use(handling))
        {
            await bus.PublishAsync(new SampleEto("a", 1));
        }

        PublishRequest request = Assert.Single(bus.Sent);
        Assert.Equal("chain", request.Envelope.CorrelationId);
        Assert.Equal((ushort)2, request.Envelope.HopLevel);
        Assert.Equal("u1", request.Envelope.GetHeader("UserId"));
    }

    [Fact]
    public async Task TransientFailure_IsRetried()
    {
        FakeBus bus = new(FastOptions()) { FailFirst = 2 };

        await bus.PublishAsync(new SampleEto("a", 1));

        Assert.Equal(3, bus.Sent.Count);
        Assert.Single(bus.Sent.Select(request => request.Envelope.MessageId).Distinct());
    }

    [Fact]
    public async Task TransientFailure_ThatNeverGoesAway_IsThrown()
    {
        FakeBus bus = new(FastOptions()) { FailFirst = 100 };

        await Assert.ThrowsAsync<InvalidOperationException>(() => bus.PublishAsync(new SampleEto("a", 1)));

        Assert.Equal(3, bus.Sent.Count);
    }

    [Fact]
    public async Task FailureThatIsNotTransient_IsThrownAtOnce()
    {
        FakeBus bus = new(FastOptions()) { FailFirst = 100, Transient = false };

        await Assert.ThrowsAsync<InvalidOperationException>(() => bus.PublishAsync(new SampleEto("a", 1)));

        Assert.Single(bus.Sent);
    }

    [Fact]
    public async Task CancellationToken_ReachesTheBroker()
    {
        FakeBus bus = new(FastOptions());
        using CancellationTokenSource cancellation = new();

        await bus.PublishAsync(new SampleEto("a", 1), cancellationToken: cancellation.Token);

        Assert.Equal(cancellation.Token, bus.LastToken);
    }

    [Fact]
    public async Task NullMessage_IsRejected()
    {
        FakeBus bus = new(FastOptions());

        await Assert.ThrowsAsync<ArgumentNullException>(() => bus.PublishAsync<SampleEto>(null!));
    }

    [Fact]
    public async Task ByDefault_NoFailureIsRetried()
    {
        StrictBus bus = new(FastOptions());

        await Assert.ThrowsAsync<InvalidOperationException>(() => bus.PublishAsync(new SampleEto("a", 1)));

        Assert.Equal(1, bus.Calls);
    }
}
