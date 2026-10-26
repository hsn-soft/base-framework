using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus.Inbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus.Tests;

public sealed class EventDispatcherInboxTests : IDisposable
{
    private readonly List<ServiceProvider> _providers = [];

    public void Dispose()
    {
        foreach (ServiceProvider provider in _providers)
        {
            provider.Dispose();
        }
    }

    private sealed class FakeInbox : IInboxStore
    {
        public InboxStart Start { get; set; } = InboxStart.Started;

        public Exception? StartFailure { get; set; }

        public List<string> Calls { get; } = [];

        public Task<InboxStart> TryStartAsync(Guid messageId, string eventName, string? correlationId, CancellationToken cancellationToken = default)
        {
            Calls.Add($"start:{eventName}:{correlationId}");
            return StartFailure is null ? Task.FromResult(Start) : Task.FromException<InboxStart>(StartFailure);
        }

        public Task CompleteAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            Calls.Add("complete");
            return Task.CompletedTask;
        }

        public Task FailAsync(Guid messageId, string reason, CancellationToken cancellationToken = default)
        {
            Calls.Add($"fail:{reason}");
            return Task.CompletedTask;
        }
    }

    private (EventDispatcher Dispatcher, RecordingHandler Handler, FakeInbox Inbox, ListLogger<EventDispatcher> Logger) Create(bool withInbox = true)
    {
        EventBusOptions options = new() { MaxHandlerAttempts = 1, RetryDelay = TimeSpan.FromMilliseconds(1) };
        FakeInbox inbox = new();
        ServiceCollection services = new();
        services.AddSingleton(Options.Create(options));
        services.AddLogging();
        services.AddSingleton<RecordingHandler>();
        services.AddEventHandler<SampleEto, RecordingHandler>();
        services.AddSingleton<IEventBusSubscriptionManager, InMemoryEventBusSubscriptionManager>();
        services.AddSingleton<IInboxStore>(inbox);

        ServiceProvider provider = services.BuildServiceProvider();
        _providers.Add(provider);
        ListLogger<EventDispatcher> logger = new();
        EventDispatcher dispatcher = new(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<IEventBusSubscriptionManager>(),
            Options.Create(options),
            logger,
            inbox: withInbox ? new InboxEnabled() : null);
        return (dispatcher, provider.GetRequiredService<RecordingHandler>(), inbox, logger);
    }

    private static MessageEnvelope<SampleEto> NewEnvelope() => new()
    {
        MessageId = Guid.CreateVersion7(),
        CorrelationId = "corr",
        HopLevel = 1,
        Message = new SampleEto("m", 1),
    };

    [Fact]
    public async Task AMessageTheInboxHands_IsHandled_AndThenCompleted()
    {
        (EventDispatcher dispatcher, RecordingHandler handler, FakeInbox inbox, _) = Create();

        await dispatcher.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope());

        Assert.Single(handler.Handled);
        Assert.Equal(["start:Sample:corr", "complete"], inbox.Calls);
    }

    [Theory]
    [InlineData(InboxStart.AlreadyHandled, LogLevel.Information)]
    [InlineData(InboxStart.InProgress, LogLevel.Warning)]
    [InlineData(InboxStart.RetryLimitReached, LogLevel.Warning)]
    public async Task AMessageTheInboxDoesNotHand_IsLeftAlone_WithoutAnError(InboxStart start, LogLevel level)
    {
        (EventDispatcher dispatcher, RecordingHandler handler, FakeInbox inbox, ListLogger<EventDispatcher> logger) = Create();
        inbox.Start = start;

        await dispatcher.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope());

        Assert.Equal(0, handler.Calls);
        Assert.Equal(["start:Sample:corr"], inbox.Calls);
        Assert.Contains(logger.Entries, entry => entry.Level == level && entry.Message.Contains("left", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AHandlerThatFails_FailsTheInboxRecordWithItsMessage_AndTheErrorGoesOn()
    {
        (EventDispatcher dispatcher, RecordingHandler handler, FakeInbox inbox, _) = Create();
        handler.FailTimes = 1;
        handler.Failure = new InvalidOperationException("the database is down");

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope()));

        Assert.Equal("the database is down", failure.Message);
        Assert.Equal(["start:Sample:corr", "fail:the database is down"], inbox.Calls);
    }

    [Fact]
    public async Task AMessageTheApplicationSaysNoTo_IsCompleted_NotFailed()
    {
        (EventDispatcher dispatcher, RecordingHandler handler, FakeInbox inbox, _) = Create();
        handler.FailTimes = 1;
        handler.Failure = new BusinessRuleException("not allowed");

        await dispatcher.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope());

        Assert.Equal(["start:Sample:corr", "complete"], inbox.Calls);
    }

    [Fact]
    public async Task AHandlingCutShortByAShutdown_LeavesTheRecordStarted()
    {
        (EventDispatcher dispatcher, RecordingHandler handler, FakeInbox inbox, _) = Create();
        handler.FailTimes = 1;
        handler.Failure = new OperationCanceledException();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => dispatcher.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope()));

        Assert.Equal(["start:Sample:corr"], inbox.Calls);
    }

    [Fact]
    public async Task AnInboxThatCannotBeAsked_FailsTheDelivery_SoTheMessageIsNotDropped()
    {
        (EventDispatcher dispatcher, RecordingHandler handler, FakeInbox inbox, _) = Create();
        inbox.StartFailure = new TimeoutException("the inbox database does not answer");

        await Assert.ThrowsAsync<TimeoutException>(() => dispatcher.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope()));

        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task WithoutAnInbox_TheStoreIsNotAsked()
    {
        (EventDispatcher dispatcher, RecordingHandler handler, FakeInbox inbox, _) = Create(withInbox: false);

        await dispatcher.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope());

        Assert.Single(handler.Handled);
        Assert.Empty(inbox.Calls);
    }
}
