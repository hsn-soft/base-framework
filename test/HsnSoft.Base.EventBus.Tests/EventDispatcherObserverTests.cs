using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus.Inbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus.Tests;

/// <summary>The observers of the whole handling of a message: they begin when the handling starts, are told how it ended (handled, rejected, failed), and are not told of a handling cut short by a shutdown or of a delivery that is left alone.</summary>
public sealed class EventDispatcherObserverTests : IDisposable
{
    private readonly List<ServiceProvider> _providers = [];

    public void Dispose()
    {
        foreach (ServiceProvider provider in _providers)
        {
            provider.Dispose();
        }
    }

    private sealed class Journal
    {
        public List<string> Steps { get; } = [];
    }

    private sealed class Companion(Journal journal) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            journal.Steps.Add("companion disposed");
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingObserver(Journal journal) : IEventHandlingObserver
    {
        public bool ThrowOnStart { get; set; }

        public bool ThrowOnEnd { get; set; }

        public bool WithCompanion { get; set; } = true;

        public ValueTask<IAsyncDisposable?> StartedAsync(ParentMessageEnvelope envelope, CancellationToken cancellationToken)
        {
            journal.Steps.Add("started");
            return ThrowOnStart ? throw new InvalidOperationException("start") : ValueTask.FromResult<IAsyncDisposable?>(WithCompanion ? new Companion(journal) : null);
        }

        public ValueTask EndedAsync(ParentMessageEnvelope envelope, EventHandlingOutcome outcome, CancellationToken cancellationToken)
        {
            journal.Steps.Add($"ended {outcome} (token cancelled: {cancellationToken.IsCancellationRequested})");
            return ThrowOnEnd ? throw new InvalidOperationException("end") : ValueTask.CompletedTask;
        }
    }

    private sealed class JournalHandler(Journal journal) : IIntegrationEventHandler<SampleEto>
    {
        public Func<Task>? Behaviour { get; set; }

        public async Task HandleAsync(MessageEnvelope<SampleEto> envelope, CancellationToken cancellationToken)
        {
            journal.Steps.Add("handler");
            if (Behaviour is not null)
            {
                await Behaviour();
            }
        }
    }

    private sealed class FakeInbox(InboxStart start) : IInboxStore
    {
        public Task<InboxStart> TryStartAsync(Guid messageId, string eventName, string? correlationId, CancellationToken cancellationToken = default) => Task.FromResult(start);

        public Task CompleteAsync(Guid messageId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task FailAsync(Guid messageId, string reason, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private (EventDispatcher Dispatcher, Journal Journal, RecordingObserver Observer, JournalHandler Handler, ListLogger<EventDispatcher> Logger) Create(InboxStart? inbox = null, int attempts = 1)
    {
        EventBusOptions options = new() { MaxHandlerAttempts = attempts, RetryDelay = TimeSpan.FromMilliseconds(1) };
        Journal journal = new();
        RecordingObserver observer = new(journal);
        ServiceCollection services = new();
        services.AddSingleton(Options.Create(options));
        services.AddLogging();
        services.AddSingleton(journal);
        services.AddSingleton<JournalHandler>();
        services.AddEventHandler<SampleEto, JournalHandler>();
        services.AddSingleton<IEventBusSubscriptionManager, InMemoryEventBusSubscriptionManager>();
        if (inbox is { } start)
        {
            services.AddSingleton<IInboxStore>(new FakeInbox(start));
        }

        ServiceProvider provider = services.BuildServiceProvider();
        _providers.Add(provider);
        ListLogger<EventDispatcher> logger = new();
        EventDispatcher dispatcher = new(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<IEventBusSubscriptionManager>(),
            Options.Create(options),
            logger,
            inbox: inbox is null ? null : new InboxEnabled(),
            observers: [observer]);
        return (dispatcher, journal, observer, provider.GetRequiredService<JournalHandler>(), logger);
    }

    private static MessageEnvelope<SampleEto> NewEnvelope() => new() { MessageId = Guid.CreateVersion7(), CorrelationId = "corr", HopLevel = 1, Message = new SampleEto("m", 1) };

    [Fact]
    public async Task AHandledMessage_StartsTheObserver_BeforeTheHandler_AndEndsItAfterwardsAsHandled()
    {
        (EventDispatcher dispatcher, Journal journal, _, _, _) = Create();

        await dispatcher.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope());

        Assert.Equal(["started", "handler", "companion disposed", "ended Handled (token cancelled: False)"], journal.Steps);
    }

    [Fact]
    public async Task AMessageTheApplicationSaysNoTo_EndsAsRejected()
    {
        (EventDispatcher dispatcher, Journal journal, _, JournalHandler handler, _) = Create();
        handler.Behaviour = () => throw new BusinessException("rule_broken", "no");

        await dispatcher.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope());

        Assert.Equal(["started", "handler", "companion disposed", "ended Rejected (token cancelled: False)"], journal.Steps);
    }

    [Fact]
    public async Task AHandlerThatFailsOnEveryAttempt_EndsAsFailed_AndTheFailureStillReachesTheBroker()
    {
        (EventDispatcher dispatcher, Journal journal, _, JournalHandler handler, _) = Create(attempts: 2);
        handler.Behaviour = () => throw new InvalidOperationException("boom");

        await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope()));

        Assert.Equal(["started", "handler", "handler", "companion disposed", "ended Failed (token cancelled: False)"], journal.Steps);   // the observer lives through the attempts: it is one handling
    }

    [Fact]
    public async Task AHandlingCutShortByAShutdown_StopsWhatKeepsItCompany_ButIsNotToldItEnded()
    {
        (EventDispatcher dispatcher, Journal journal, _, JournalHandler handler, _) = Create();
        handler.Behaviour = () => throw new OperationCanceledException();

        await Assert.ThrowsAsync<OperationCanceledException>(() => dispatcher.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope()));

        Assert.Equal(["started", "handler", "companion disposed"], journal.Steps);   // the message comes back: nothing is given away
    }

    [Theory]
    [InlineData(InboxStart.AlreadyHandled)]
    [InlineData(InboxStart.RetryLimitReached)]
    public async Task ADeliveryThatIsLeftAlone_IsNotObserved(InboxStart start)
    {
        (EventDispatcher dispatcher, Journal journal, _, _, _) = Create(start);

        await dispatcher.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope());

        Assert.Empty(journal.Steps);
    }

    [Fact]
    public async Task ADeliveryThatAnotherHandlingHolds_IsNotObserved_AndIsPutBack()
    {
        (EventDispatcher dispatcher, Journal journal, _, _, _) = Create(InboxStart.InProgress);

        await Assert.ThrowsAsync<MessageInProgressException>(() => dispatcher.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope()));

        Assert.Empty(journal.Steps);
    }

    [Fact]
    public async Task AnObserverThatFails_NeverFailsTheHandling_ItIsLogged()
    {
        (EventDispatcher dispatcher, Journal journal, RecordingObserver observer, _, ListLogger<EventDispatcher> logger) = Create();
        observer.ThrowOnStart = true;
        observer.ThrowOnEnd = true;

        await dispatcher.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope());

        Assert.Equal(["started", "handler", "ended Handled (token cancelled: False)"], journal.Steps);
        Assert.Equal(2, logger.Entries.Count(entry => entry.Level == LogLevel.Error && entry.Message.Contains("observer", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task AnObserverThatKeepsNothingCompany_IsStillToldHowTheHandlingEnded()
    {
        (EventDispatcher dispatcher, Journal journal, RecordingObserver observer, _, _) = Create();
        observer.WithCompanion = false;

        await dispatcher.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope());

        Assert.Equal(["started", "handler", "ended Handled (token cancelled: False)"], journal.Steps);
    }

    [Fact]
    public async Task TheEndIsToldWithATokenThatCannotBeCancelled_SoAPlaceIsNotKeptForAMessageThatIsFinished()
    {
        (EventDispatcher dispatcher, Journal journal, _, _, _) = Create();
        using CancellationTokenSource stopping = new();

        // the host is asked to stop after the work is done: the end is still told, and with no token that is cancelled
        await dispatcher.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope(), stopping.Token);
        await stopping.CancelAsync();

        Assert.Contains("ended Handled (token cancelled: False)", journal.Steps);
    }

    [Fact]
    public void TheObserverIsRegisteredAsASingleton()
    {
        ServiceCollection services = new();

        services.AddEventHandlingObserver<RecordingObserver>();

        ServiceDescriptor descriptor = Assert.Single(services, service => service.ServiceType == typeof(IEventHandlingObserver));
        Assert.Equal((ServiceLifetime.Singleton, typeof(RecordingObserver)), (descriptor.Lifetime, descriptor.ImplementationType));
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddEventHandlingObserver<RecordingObserver>());
    }
}
