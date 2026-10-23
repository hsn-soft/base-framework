using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus.Tests;

public class EventBusServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEventBusCore_RegistersTheDispatcherAndTheDefaults()
    {
        ServiceProvider provider = new ServiceCollection().AddLogging().AddEventBusCore().BuildServiceProvider();

        Assert.IsType<EventDispatcher>(provider.GetRequiredService<IEventDispatcher>());
        EventBusOptions options = provider.GetRequiredService<IOptions<EventBusOptions>>().Value;
        Assert.Equal("service", options.ServiceName);
        Assert.Equal(3, options.MaxHandlerAttempts);
        Assert.Equal(TimeSpan.FromMilliseconds(200), options.RetryDelay);
    }

    [Fact]
    public void AddEventBusCore_AppliesTheConfiguration()
    {
        ServiceProvider provider = new ServiceCollection().AddLogging().AddEventBusCore(o => o.ServiceName = "order-service").BuildServiceProvider();

        Assert.Equal("order-service", provider.GetRequiredService<IOptions<EventBusOptions>>().Value.ServiceName);
    }

    [Fact]
    public void AddEventBusCore_CalledTwice_KeepsTheFirstDispatcher()
    {
        IServiceCollection services = new ServiceCollection().AddLogging().AddEventBusCore().AddEventBusCore();

        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IEventDispatcher));
    }

    [Fact]
    public async Task AddEventHandler_RegistersASubscriptionThatInvokesTheHandler()
    {
        ServiceProvider provider = new ServiceCollection().AddEventHandler<SampleEvent, RecordingHandler>().BuildServiceProvider();
        EventSubscription subscription = provider.GetRequiredService<EventSubscription>();
        var sent = new SampleEvent("a", 1);

        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        await subscription.Handle(scope.ServiceProvider, sent, CancellationToken.None);

        Assert.Equal(typeof(SampleEvent), subscription.EventType);
        // a fresh handler per scope: it recorded the event it was given
        Assert.Equal(sent, Assert.Single(((RecordingHandler)scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<SampleEvent>>()).Handled));
    }

    [Fact]
    public void AddEventHandler_ForSeveralEvents_RegistersOneSubscriptionEach()
    {
        ServiceProvider provider = new ServiceCollection()
            .AddEventHandler<SampleEvent, RecordingHandler>()
            .AddEventHandler<OtherEvent, OtherHandler>()
            .BuildServiceProvider();

        Assert.Equal([typeof(SampleEvent), typeof(OtherEvent)], provider.GetServices<EventSubscription>().Select(x => x.EventType));
    }

    [Fact]
    public void NullServices_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => EventBusServiceCollectionExtensions.AddEventBusCore(null!));
        Assert.Throws<ArgumentNullException>(() => EventBusServiceCollectionExtensions.AddEventHandler<SampleEvent, RecordingHandler>(null!));
    }

    private sealed class OtherHandler : IIntegrationEventHandler<OtherEvent>
    {
        public Task HandleAsync(OtherEvent integrationEvent, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
