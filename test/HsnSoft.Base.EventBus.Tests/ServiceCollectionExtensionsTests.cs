using HsnSoft.Base.Domain.Entities.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus.Tests;

public class ServiceCollectionExtensionsTests
{
    private static ServiceCollection NewServices()
    {
        ServiceCollection services = new();
        services.AddLogging();
        return services;
    }

    [Fact]
    public void Core_RegistersTheOptionsAndTheSharedServices()
    {
        ServiceCollection services = NewServices();
        services.AddEventBusCore(options => options.ConsumerClientName = "OrderService");
        services.AddEventHandler<SampleEto, RecordingHandler>();
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Equal("OrderService", provider.GetRequiredService<IOptions<EventBusOptions>>().Value.ConsumerClientName);
        Assert.IsType<InMemoryEventBusSubscriptionManager>(provider.GetRequiredService<IEventBusSubscriptionManager>());
        Assert.IsType<EventDispatcher>(provider.GetRequiredService<IEventDispatcher>());
        Assert.NotNull(provider.GetRequiredService<EventEnvelopeFactory>());
    }

    [Fact]
    public void Core_WithoutConfiguration_KeepsTheDefaults()
    {
        ServiceCollection services = NewServices();
        services.AddEventBusCore();
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Equal("Client", provider.GetRequiredService<IOptions<EventBusOptions>>().Value.ConsumerClientName);
    }

    [Fact]
    public void Core_DoesNotReplaceWhatIsAlreadyRegistered()
    {
        ServiceCollection services = NewServices();
        services.AddSingleton<IEventDispatcher, NoopDispatcher>();
        services.AddEventBusCore();
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsType<NoopDispatcher>(provider.GetRequiredService<IEventDispatcher>());
    }

    private sealed class NoopDispatcher : IEventDispatcher
    {
        public Task DispatchAsync(string trimmedEventName, Type eventType, object messageEnvelope, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [Fact]
    public void Handler_IsRegisteredWithItsSubscription()
    {
        ServiceCollection services = NewServices();
        services.AddEventBusCore();
        services.AddEventHandler<SampleEto, RecordingHandler>(fetchCount: 4);
        using ServiceProvider provider = services.BuildServiceProvider();

        IntegrationEventInfo info = provider.GetRequiredService<IEventBusSubscriptionManager>().GetEventInfoByName("Sample")!;
        Assert.Equal((ushort)4, info.FetchCount);
        Assert.IsType<RecordingHandler>(provider.GetRequiredService<RecordingHandler>());
    }

    [Fact]
    public void Handler_OfAnotherEvent_IsRejected()
    {
        ServiceCollection services = NewServices();

        Assert.Throws<ArgumentException>(() => services.AddEventHandler(typeof(SampleEto), typeof(ThrowingSynchronouslyHandler).MakeArrayType()));
        Assert.Throws<ArgumentException>(() => services.AddEventHandler(typeof(string), typeof(RecordingHandler)));
        Assert.Throws<ArgumentException>(() => services.AddEventHandler(typeof(OtherEto), typeof(RecordingHandler)));
    }

    [Fact]
    public void AssemblyScan_RegistersEveryHandlerForEveryEventItHandles()
    {
        ServiceCollection services = NewServices();
        services.AddEventBusCore();
        services.AddEventHandlers(typeof(EventBusBaseTests).Assembly, new Dictionary<string, ushort> { ["OtherEto"] = 5, ["SampleEto"] = 0 });
        using ServiceProvider provider = services.BuildServiceProvider();

        IEventBusSubscriptionManager manager = provider.GetRequiredService<IEventBusSubscriptionManager>();
        Assert.True(manager.HasSubscriptionsForEvent("Sample"));
        Assert.True(manager.HasSubscriptionsForEvent("Other"));
        Assert.Equal((ushort)5, manager.GetEventInfoByName("Other")!.FetchCount);
        Assert.Equal((ushort)1, manager.GetEventInfoByName("Sample")!.FetchCount);
    }

    [Fact]
    public void AssemblyScan_WithoutFetchCounts_UsesOne()
    {
        ServiceCollection services = NewServices();
        services.AddEventBusCore();
        services.AddEventHandlers(typeof(EventBusBaseTests).Assembly);
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Equal((ushort)1, provider.GetRequiredService<IEventBusSubscriptionManager>().GetEventInfoByName("Other")!.FetchCount);
    }

    [Fact]
    public void HeaderProvider_IsUsedByTheEnvelopeFactory()
    {
        ServiceCollection services = NewServices();
        services.AddEventBusCore();
        services.AddEventHeaderProvider<ProductHeaders>();
        using ServiceProvider provider = services.BuildServiceProvider();

        MessageEnvelope<SampleEto> envelope = provider.GetRequiredService<EventEnvelopeFactory>().Create(new SampleEto("a", 1), null, null, false);

        Assert.Equal("product-corr", envelope.CorrelationId);
        Assert.Equal("product-user", envelope.GetHeader("UserId"));
    }

    private sealed class ProductHeaders : IEventHeaderProvider
    {
        public string? GetCorrelationId() => "product-corr";

        public void AddHeaders(IDictionary<string, string> headers) => headers["UserId"] = "product-user";
    }

    [Fact]
    public void NullArguments_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => EventBusServiceCollectionExtensions.AddEventBusCore(null!));
        Assert.Throws<ArgumentNullException>(() => EventBusServiceCollectionExtensions.AddEventHandler<SampleEto, RecordingHandler>(null!));
        Assert.Throws<ArgumentNullException>(() => NewServices().AddEventHandler(null!, typeof(RecordingHandler)));
        Assert.Throws<ArgumentNullException>(() => NewServices().AddEventHandler(typeof(SampleEto), null!));
        Assert.Throws<ArgumentNullException>(() => EventBusServiceCollectionExtensions.AddEventHandlers(null!, typeof(EventBusBaseTests).Assembly));
        Assert.Throws<ArgumentNullException>(() => NewServices().AddEventHandlers(null!));
        Assert.Throws<ArgumentNullException>(() => EventBusServiceCollectionExtensions.AddEventHeaderProvider<ProductHeaders>(null!));
    }
}
