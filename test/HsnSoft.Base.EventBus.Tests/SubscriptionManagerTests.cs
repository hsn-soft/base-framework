using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus.Tests;

public class SubscriptionManagerTests
{
    private static InMemoryEventBusSubscriptionManager Create(ListLogger<InMemoryEventBusSubscriptionManager>? logger, params EventSubscription[] subscriptions)
        => new(subscriptions, Options.Create(new EventBusOptions()), logger ?? new ListLogger<InMemoryEventBusSubscriptionManager>());

    [Fact]
    public void WithoutSubscriptions_ItIsEmpty()
    {
        InMemoryEventBusSubscriptionManager manager = Create(null);

        Assert.True(manager.IsEmpty);
        Assert.Empty(manager.Events);
        Assert.False(manager.HasSubscriptionsForEvent("Sample"));
        Assert.Empty(manager.GetHandlersForEvent("Sample"));
        Assert.Null(manager.GetEventInfoByName("Sample"));
    }

    [Fact]
    public void Subscription_IsFoundByEventNameAndByType()
    {
        InMemoryEventBusSubscriptionManager manager = Create(null, new EventSubscription(typeof(SampleEto), typeof(RecordingHandler), 3));

        Assert.False(manager.IsEmpty);
        Assert.True(manager.HasSubscriptionsForEvent("Sample"));
        Assert.True(manager.HasSubscriptionsForEvent<SampleEto>());
        Assert.Equal(typeof(RecordingHandler), Assert.Single(manager.GetHandlersForEvent("Sample")).HandlerType);
        Assert.Equal(typeof(RecordingHandler), Assert.Single(manager.GetHandlersForEvent<SampleEto>()).HandlerType);
        IntegrationEventInfo info = Assert.IsType<IntegrationEventInfo>(manager.GetEventInfoByName("Sample"));
        Assert.Equal(new IntegrationEventInfo("Sample", typeof(SampleEto), 3), info);
        Assert.Equal("Sample", manager.GetEventKey<SampleEto>());
        Type otherType = typeof(OtherEto);
        Assert.Equal("Other", manager.GetEventKey(otherType));
        Assert.Equal("Sample", manager.EventNameGetter("SampleEto"));
    }

    [Fact]
    public void FetchCountBelowOne_BecomesOne()
    {
        InMemoryEventBusSubscriptionManager manager = Create(null, new EventSubscription(typeof(SampleEto), typeof(RecordingHandler), 0));

        Assert.Equal((ushort)1, manager.GetEventInfoByName("Sample")!.FetchCount);
    }

    [Fact]
    public void EventWithTwoHandlers_KeepsTheFirst_AndWarnsAboutTheOther()
    {
        ListLogger<InMemoryEventBusSubscriptionManager> logger = new();
        InMemoryEventBusSubscriptionManager manager = Create(
            logger,
            new EventSubscription(typeof(SampleEto), typeof(RecordingHandler), 1),
            new EventSubscription(typeof(SampleEto), typeof(SecondHandler), 1));

        Assert.Equal(typeof(RecordingHandler), Assert.Single(manager.GetHandlersForEvent("Sample")).HandlerType);
        Assert.Single(manager.Events);
        (_, string message) = Assert.Single(logger.Entries);
        Assert.Contains("SecondHandler", message, StringComparison.Ordinal);
        Assert.Contains("RecordingHandler", message, StringComparison.Ordinal);
    }

    [Fact]
    public void SameHandlerRegisteredTwice_IsNotAWarning()
    {
        ListLogger<InMemoryEventBusSubscriptionManager> logger = new();
        InMemoryEventBusSubscriptionManager manager = Create(
            logger,
            new EventSubscription(typeof(SampleEto), typeof(RecordingHandler), 1),
            new EventSubscription(typeof(SampleEto), typeof(RecordingHandler), 1));

        Assert.Single(manager.Events);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public void Events_AreListedInRegistrationOrder()
    {
        InMemoryEventBusSubscriptionManager manager = Create(
            null,
            new EventSubscription(typeof(OtherEto), typeof(SecondHandler), 1),
            new EventSubscription(typeof(SampleEto), typeof(RecordingHandler), 1));

        Assert.Equal(["Other", "Sample"], manager.Events.Select(info => info.EventName));
    }
}
