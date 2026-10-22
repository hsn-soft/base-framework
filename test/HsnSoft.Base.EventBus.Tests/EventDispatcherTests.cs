using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus.Tests;

public class EventDispatcherTests
{
    private readonly RecordingHandler _handler = new();
    private readonly ServiceProvider _provider;
    private readonly EventSubscription _subscription;

    public EventDispatcherTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_handler);
        services.AddEventHandler<SampleEvent, RecordingHandler>();
        // the handler instance is shared with the test, so replace the scoped registration with the singleton
        services.AddScoped<IIntegrationEventHandler<SampleEvent>>(provider => provider.GetRequiredService<RecordingHandler>());
        _provider = services.BuildServiceProvider();
        _subscription = _provider.GetServices<EventSubscription>().Single();
    }

    private EventDispatcher CreateDispatcher(int maxAttempts = 3)
        => new(_provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new EventBusOptions { MaxHandlerAttempts = maxAttempts, RetryDelay = TimeSpan.Zero }),
            NullLogger<EventDispatcher>.Instance);

    private static byte[] Body(SampleEvent integrationEvent) => EventSerializer.Serialize(integrationEvent);

    [Fact]
    public async Task Dispatch_HandsTheEventToItsHandler()
    {
        var sent = new SampleEvent("a", 1);

        await CreateDispatcher().DispatchAsync(_subscription, Body(sent), CancellationToken.None);

        Assert.Equal(sent, Assert.Single(_handler.Handled));
    }

    [Fact]
    public async Task Dispatch_RetriesUntilTheHandlerSucceeds()
    {
        _handler.FailTimes = 2;

        await CreateDispatcher(maxAttempts: 3).DispatchAsync(_subscription, Body(new SampleEvent("a", 1)), CancellationToken.None);

        Assert.Equal(3, _handler.Calls);
        Assert.Single(_handler.Handled);
    }

    [Fact]
    public async Task Dispatch_AfterTheLastAttempt_ThrowsTheHandlerException()
    {
        _handler.FailTimes = 10;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateDispatcher(maxAttempts: 3).DispatchAsync(_subscription, Body(new SampleEvent("a", 1)), CancellationToken.None));

        Assert.Equal(3, _handler.Calls);
        Assert.Empty(_handler.Handled);
    }

    [Fact]
    public async Task Dispatch_WithZeroAttempts_StillTriesOnce()
    {
        _handler.FailTimes = 10;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateDispatcher(maxAttempts: 0).DispatchAsync(_subscription, Body(new SampleEvent("a", 1)), CancellationToken.None));

        Assert.Equal(1, _handler.Calls);
    }

    [Fact]
    public async Task Dispatch_DoesNotRetryACancellation()
    {
        _handler.FailTimes = 10;
        _handler.Failure = new OperationCanceledException();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => CreateDispatcher().DispatchAsync(_subscription, Body(new SampleEvent("a", 1)), CancellationToken.None));

        Assert.Equal(1, _handler.Calls);
    }

    [Fact]
    public async Task Dispatch_UnreadableMessage_ThrowsWithoutCallingTheHandler()
    {
        await Assert.ThrowsAnyAsync<JsonException>(
            () => CreateDispatcher().DispatchAsync(_subscription, "{ broken"u8.ToArray(), CancellationToken.None));

        Assert.Equal(0, _handler.Calls);
    }

    [Fact]
    public async Task Dispatch_NullSubscription_Throws()
        => await Assert.ThrowsAsync<ArgumentNullException>(() => CreateDispatcher().DispatchAsync(null!, Body(new SampleEvent("a", 1)), CancellationToken.None));
}
