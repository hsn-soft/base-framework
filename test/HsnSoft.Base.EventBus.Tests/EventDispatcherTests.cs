using HsnSoft.Base.Domain.Entities.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus.Tests;

public sealed class EventDispatcherTests : IDisposable
{
    private readonly List<ServiceProvider> _providers = [];

    public void Dispose()
    {
        foreach (ServiceProvider provider in _providers)
        {
            provider.Dispose();
        }
    }

    private static MessageEnvelope<SampleEto> NewEnvelope() => new()
    {
        MessageId = Guid.CreateVersion7(),
        CorrelationId = "corr",
        HopLevel = 1,
        Message = new SampleEto("m", 1),
    };

    private (EventDispatcher Dispatcher, ServiceProvider Provider, ListLogger<EventDispatcher> Logger) Create(
        Action<IServiceCollection> register,
        int maxHandlerAttempts = 1,
        TimeSpan? retryDelay = null)
    {
        EventBusOptions options = new() { MaxHandlerAttempts = maxHandlerAttempts, RetryDelay = retryDelay ?? TimeSpan.FromMilliseconds(1) };

        ServiceCollection services = new();
        services.AddSingleton(Options.Create(options));
        services.AddLogging();
        register(services);
        services.AddSingleton<IEventBusSubscriptionManager, InMemoryEventBusSubscriptionManager>();

        ServiceProvider provider = services.BuildServiceProvider();
        _providers.Add(provider);
        ListLogger<EventDispatcher> logger = new();
        EventDispatcher dispatcher = new(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<IEventBusSubscriptionManager>(),
            Options.Create(options),
            logger);
        return (dispatcher, provider, logger);
    }

    private (EventDispatcher Dispatcher, RecordingHandler Handler, ListLogger<EventDispatcher> Logger) CreateRecording(int maxHandlerAttempts = 1)
    {
        (EventDispatcher dispatcher, ServiceProvider provider, ListLogger<EventDispatcher> logger) = Create(
            services =>
            {
                services.AddSingleton<RecordingHandler>();
                services.AddEventHandler<SampleEto, RecordingHandler>();
            },
            maxHandlerAttempts);
        return (dispatcher, provider.GetRequiredService<RecordingHandler>(), logger);
    }

    [Fact]
    public async Task Handler_ReceivesTheEnvelope_AndSuccessIsLogged()
    {
        (EventDispatcher dispatcher, RecordingHandler handler, ListLogger<EventDispatcher> logger) = CreateRecording();
        MessageEnvelope<SampleEto> envelope = NewEnvelope();

        await dispatcher.DispatchAsync("Sample", typeof(SampleEto), envelope);

        Assert.Same(envelope, Assert.Single(handler.Handled));
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Information && entry.Message.Contains("RecordingHandler handled Sample", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Handler_RunsWithTheMessageAsTheCurrentContext_ThatEndsAfterwards()
    {
        (EventDispatcher dispatcher, RecordingHandler handler, _) = CreateRecording();
        MessageEnvelope<SampleEto> envelope = NewEnvelope();

        await dispatcher.DispatchAsync("Sample", typeof(SampleEto), envelope);

        Assert.Same(envelope, handler.ContextDuringHandling);
        Assert.Null(EventContext.Current);
    }

    [Fact]
    public async Task LogScope_CarriesTheCorrelationAndMessageIds()
    {
        (EventDispatcher dispatcher, _, ListLogger<EventDispatcher> logger) = CreateRecording();
        MessageEnvelope<SampleEto> envelope = NewEnvelope();

        await dispatcher.DispatchAsync("Sample", typeof(SampleEto), envelope);

        IReadOnlyDictionary<string, object?> scope = Assert.Single(logger.Scopes);
        Assert.Equal("corr", scope["CorrelationId"]);
        Assert.Equal(envelope.MessageId, scope["MessageId"]);
        Assert.Equal("Sample", scope["EventName"]);
    }

    [Fact]
    public async Task EventNobodyListensTo_DoesNothing()
    {
        (EventDispatcher dispatcher, RecordingHandler handler, _) = CreateRecording();

        await dispatcher.DispatchAsync("Unknown", typeof(OtherEto), new MessageEnvelope<OtherEto> { Message = new OtherEto(Guid.NewGuid()) });

        Assert.Empty(handler.Handled);
    }

    [Fact]
    public async Task HandlerThatFails_IsRethrown_WithoutRetryByDefault()
    {
        (EventDispatcher dispatcher, RecordingHandler handler, ListLogger<EventDispatcher> logger) = CreateRecording();
        handler.FailTimes = 5;

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope()));

        Assert.Equal("failure 1", failure.Message);
        Assert.Equal(1, handler.Calls);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error);
    }

    [Fact]
    public async Task HandlerThatFailsTwice_IsRetriedWhenAttemptsAreAllowed()
    {
        (EventDispatcher dispatcher, RecordingHandler handler, ListLogger<EventDispatcher> logger) = CreateRecording(maxHandlerAttempts: 3);
        handler.FailTimes = 2;

        await dispatcher.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope());

        Assert.Equal(3, handler.Calls);
        Assert.Single(handler.Handled);
        Assert.Equal(2, logger.Entries.Count(entry => entry.Level == LogLevel.Warning));
    }

    [Fact]
    public async Task HandlerThatKeepsFailing_IsThrownAfterAllAttempts()
    {
        (EventDispatcher dispatcher, RecordingHandler handler, _) = CreateRecording(maxHandlerAttempts: 3);
        handler.FailTimes = 10;

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope()));

        Assert.Equal("failure 3", failure.Message);
        Assert.Equal(3, handler.Calls);
    }

    [Fact]
    public async Task HandlerThatThrowsBeforeItsFirstAwait_FailsWithItsOwnException()
    {
        (EventDispatcher dispatcher, _, _) = Create(services => services.AddEventHandler<SampleEto, ThrowingSynchronouslyHandler>());

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope()));

        Assert.Equal("thrown synchronously", failure.Message);
    }

    [Fact]
    public async Task HandlerTheContainerCannotCreate_IsNotRetried_AndSaysSo()
    {
        (EventDispatcher dispatcher, _, _) = Create(
            services => services.AddSingleton(new EventSubscription(typeof(SampleEto), typeof(RecordingHandler), 1)),
            maxHandlerAttempts: 5,
            retryDelay: TimeSpan.FromMinutes(10));

        InvalidOperationException failure = await Assert.ThrowsAnyAsync<InvalidOperationException>(() => dispatcher.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope()));

        Assert.Contains("No handler resolved for", failure.Message, StringComparison.Ordinal);
        Assert.Contains("RecordingHandler", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Cancellation_IsPassedOn_AndNotLoggedAsAFailure()
    {
        (EventDispatcher dispatcher, _, ListLogger<EventDispatcher> logger) = Create(
            services => services.AddEventHandler<SampleEto, CancellingHandler>(),
            maxHandlerAttempts: 3);

        await Assert.ThrowsAsync<OperationCanceledException>(() => dispatcher.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope()));

        Assert.DoesNotContain(logger.Entries, entry => entry.Level >= LogLevel.Warning);
    }

    [Fact]
    public async Task EveryAttempt_RunsInItsOwnScope()
    {
        List<Guid> seen = [];
        (EventDispatcher dispatcher, _, _) = Create(
            services =>
            {
                services.AddSingleton(seen);
                services.AddScoped<ScopedMarker>();
                services.AddEventHandler<SampleEto, ScopedHandler>();
            },
            maxHandlerAttempts: 3);

        await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope()));

        Assert.Equal(3, seen.Count);
        Assert.Equal(3, seen.Distinct().Count());
    }

    [Fact]
    public async Task NullArguments_AreRejected()
    {
        (EventDispatcher dispatcher, _, _) = CreateRecording();

        await Assert.ThrowsAsync<ArgumentNullException>(() => dispatcher.DispatchAsync(null!, typeof(SampleEto), NewEnvelope()));
        await Assert.ThrowsAsync<ArgumentNullException>(() => dispatcher.DispatchAsync("Sample", null!, NewEnvelope()));
        await Assert.ThrowsAsync<ArgumentNullException>(() => dispatcher.DispatchAsync("Sample", typeof(SampleEto), null!));
    }
}
