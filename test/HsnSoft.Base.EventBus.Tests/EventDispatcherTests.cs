using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Masking;
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
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Information && entry.Message.Contains("handled by RecordingHandler", StringComparison.Ordinal));
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
    public async Task LogScope_CarriesTheChainAndTheMessage()
    {
        (EventDispatcher dispatcher, _, ListLogger<EventDispatcher> logger) = CreateRecording();
        MessageEnvelope<SampleEto> envelope = NewEnvelope();
        envelope.Producer = "OrderService";
        envelope.HopLevel = 2;
        Guid parent = Guid.NewGuid();
        envelope.ParentMessageId = parent;

        await dispatcher.DispatchAsync("Sample", typeof(SampleEto), envelope);

        IReadOnlyDictionary<string, object?> scope = logger.Scopes[0];
        Assert.Equal("corr", scope["CorrelationId"]);
        Assert.Matches("^[0-9a-f]{32}$", (string)scope["ScopeId"]!);
        Assert.Equal(envelope.MessageId, scope["MessageId"]);
        Assert.Equal(parent, scope["ParentMessageId"]);
        Assert.Equal("Sample", scope["EventName"]);
        Assert.Equal("OrderService", scope["Producer"]);
        Assert.Equal((ushort)2, scope["HopLevel"]);
    }

    [Fact]
    public async Task WhoCausedTheMessage_IsInTheLogs()
    {
        (EventDispatcher dispatcher, _, ListLogger<EventDispatcher> logger) = CreateRecording();
        MessageEnvelope<SampleEto> envelope = NewEnvelope();
        Guid user = Guid.NewGuid();
        envelope.UserId = user;
        envelope.UserRoles = ["admin", "customer"];
        envelope.SetHeader("ClientChannel", "mobile");
        envelope.SetHeader("ClientVersion", "3.2.1");
        envelope.SetHeader("ClientLat", "41.0");
        envelope.SetHeader("ClientLong", "29.0");
        envelope.SetHeader("Unrelated", "x");

        await dispatcher.DispatchAsync("Sample", typeof(SampleEto), envelope);

        IReadOnlyDictionary<string, object?> scope = logger.Scopes[0];
        Assert.Equal(user, scope["UserId"]);
        Assert.Equal("admin,customer", scope["UserRoles"]);
        Assert.Equal("mobile", scope["ClientChannel"]);
        Assert.Equal("3.2.1", scope["ClientVersion"]);
        Assert.Equal("41.0", scope["ClientLat"]);
        Assert.Equal("29.0", scope["ClientLong"]);
        Assert.False(scope.ContainsKey("Unrelated"));
    }

    [Fact]
    public async Task TheHandler_RunsAsTheCallerOfTheChain_AndNobodyAfterwards()
    {
        (EventDispatcher dispatcher, RecordingHandler handler, _) = CreateRecording();
        MessageEnvelope<SampleEto> envelope = NewEnvelope();
        Guid user = Guid.NewGuid();
        (envelope.UserId, envelope.UserName, envelope.UserRoles) = (user, "alice", ["customer"]);

        await dispatcher.DispatchAsync("Sample", typeof(SampleEto), envelope);

        HsnSoft.Base.Security.UserContext acting = Assert.IsType<HsnSoft.Base.Security.UserContext>(handler.ActingUserDuringHandling);
        Assert.Equal((user, "alice"), (acting.Id, acting.UserName));
        Assert.Equal(["customer"], acting.Roles);
        Assert.Null(HsnSoft.Base.Security.ActingUser.Current);
    }

    private sealed class RecordingScope(List<string> events, string name, bool failToBegin = false) : IEventHandlerScope
    {
        public IDisposable Begin(ParentMessageEnvelope envelope)
        {
            events.Add($"begin {name} {envelope.MessageId != Guid.Empty}");
            return failToBegin ? throw new InvalidOperationException("cannot begin") : new Ending(events, name);
        }

        private sealed class Ending(List<string> events, string name) : IDisposable
        {
            public void Dispose() => events.Add($"end {name}");
        }
    }

    [Fact]
    public async Task TheScopesOfThePackages_AreOpenedAroundTheHandler_AndClosedInReverse_EvenWhenItFails()
    {
        List<string> events = [];
        (EventDispatcher dispatcher, _, _) = Create(services =>
        {
            services.AddSingleton<RecordingHandler>(_ => new RecordingHandler { FailTimes = 1 });
            services.AddEventHandler<SampleEto, RecordingHandler>();
            services.AddScoped<IEventHandlerScope>(_ => new RecordingScope(events, "first"));
            services.AddScoped<IEventHandlerScope>(_ => new RecordingScope(events, "second"));
        }, maxHandlerAttempts: 2);

        await dispatcher.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope());

        // the first attempt fails, the second succeeds: each attempt opens and closes its own scopes
        string[] attempt = ["begin first True", "begin second True", "end second", "end first"];
        Assert.Equal([.. attempt, .. attempt], events);
    }

    [Fact]
    public async Task AScopeThatCannotBegin_ClosesTheOnesBeforeIt_AndTheMessageIsNotHandled()
    {
        List<string> events = [];
        (EventDispatcher dispatcher, ServiceProvider provider, _) = Create(services =>
        {
            services.AddSingleton<RecordingHandler>();
            services.AddEventHandler<SampleEto, RecordingHandler>();
            services.AddScoped<IEventHandlerScope>(_ => new RecordingScope(events, "first"));
            services.AddScoped<IEventHandlerScope>(_ => new RecordingScope(events, "broken", failToBegin: true));
        });

        await Assert.ThrowsAnyAsync<Exception>(() => dispatcher.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope()));

        Assert.Equal(["begin first True", "begin broken True", "end first"], events);
        Assert.Equal(0, provider.GetRequiredService<RecordingHandler>().Calls);
    }

    [Fact]
    public async Task AMessageWithoutACaller_RunsAsNobody_EvenInsideAnotherActingScope()
    {
        (EventDispatcher dispatcher, RecordingHandler handler, _) = CreateRecording();

        using (HsnSoft.Base.Security.ActingUser.Use(new HsnSoft.Base.Security.UserContext(Guid.NewGuid(), "someone", [])))
        {
            await dispatcher.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope());

            Assert.Null(handler.ActingUserDuringHandling);
            Assert.NotNull(HsnSoft.Base.Security.ActingUser.Current);
        }
    }

    [Fact]
    public async Task MessagesWithoutClientHeaders_HaveNoSuchFields()
    {
        (EventDispatcher dispatcher, _, ListLogger<EventDispatcher> logger) = CreateRecording();

        await dispatcher.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope());

        Assert.False(logger.Scopes[0].ContainsKey("UserId"));
    }

    [Fact]
    public async Task ACorrelationIdThatIsATraceId_IsTheTraceIdOfTheConsumeLogsToo()
    {
        (EventDispatcher dispatcher, _, ListLogger<EventDispatcher> logger) = CreateRecording();
        MessageEnvelope<SampleEto> traced = NewEnvelope();
        traced.CorrelationId = "cc55d74f7c0c25f06cef2eb55e0c6c23";
        MessageEnvelope<SampleEto> custom = NewEnvelope();
        custom.CorrelationId = "mobile-app-7";

        await dispatcher.DispatchAsync("Sample", typeof(SampleEto), traced);
        await dispatcher.DispatchAsync("Sample", typeof(SampleEto), custom);

        Assert.Equal("cc55d74f7c0c25f06cef2eb55e0c6c23", logger.Scopes[0]["TraceId"]);
        Assert.False(logger.Scopes.Last(scope => scope.ContainsKey("CorrelationId")).ContainsKey("TraceId"));
        Assert.Equal("mobile-app-7", logger.Scopes.Last(scope => scope.ContainsKey("CorrelationId"))["CorrelationId"]);
    }

    [Fact]
    public async Task Handling_IsLoggedAsAStartAndAnEnd_ThatShareTheScopeId()
    {
        (EventDispatcher dispatcher, _, ListLogger<EventDispatcher> logger) = CreateRecording();
        MessageEnvelope<SampleEto> envelope = NewEnvelope();
        envelope.SetHeader(ParentMessageEnvelope.FlowHeader, "Token-Login");

        await dispatcher.DispatchAsync("Sample", typeof(SampleEto), envelope);

        ListLogger<EventDispatcher>.LoggedEvent start = Assert.Single(logger.Events, e => e.EventName == "CONSUME_EVENT_START");
        ListLogger<EventDispatcher>.LoggedEvent end = Assert.Single(logger.Events, e => e.EventName == "CONSUME_EVENT_SUCCESS");
        Assert.Equal("ConsumeStart", start.Properties["LogType"]);
        Assert.Equal("ConsumeEnd", end.Properties["LogType"]);
        Assert.Equal(start.Properties["ScopeId"], end.Properties["ScopeId"]);
        Assert.Equal("corr", start.Properties["CorrelationId"]);
        Assert.Equal("token-login -> sample: started", start.Message);
        Assert.Equal("token-login", start.Properties["From"]);
        Assert.Equal("sample", end.Properties["To"]);
        Assert.IsType<long>(end.Properties["ElapsedMs"]);
        Assert.Equal(LogLevel.Information, end.Level);
    }

    [Fact]
    public async Task EachHandling_HasItsOwnScopeId()
    {
        (EventDispatcher dispatcher, _, ListLogger<EventDispatcher> logger) = CreateRecording();

        await dispatcher.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope());
        await dispatcher.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope());

        string?[] scopeIds = [.. logger.Events.Where(e => e.EventName == "CONSUME_EVENT_START").Select(e => e.Properties["ScopeId"] as string)];
        Assert.Equal(2, scopeIds.Distinct().Count());
    }

    [Fact]
    public async Task MessageWithoutAFlowHeader_ComesFromItsProducer_OrIsUnknown()
    {
        (EventDispatcher dispatcher, _, ListLogger<EventDispatcher> logger) = CreateRecording();
        MessageEnvelope<SampleEto> fromProducer = NewEnvelope();
        fromProducer.Producer = "HHS_OrderService";
        MessageEnvelope<SampleEto> anonymous = NewEnvelope();
        anonymous.Producer = null;

        await dispatcher.DispatchAsync("Sample", typeof(SampleEto), fromProducer);
        await dispatcher.DispatchAsync("Sample", typeof(SampleEto), anonymous);

        Assert.Equal(["hhs_orderservice", "unknown"], logger.Events.Where(e => e.EventName == "CONSUME_EVENT_START").Select(e => e.Properties["From"]));
    }

    [Fact]
    public async Task WhatTheHandlerPublishes_ComesFromTheEventItHandles()
    {
        (EventDispatcher dispatcher, RecordingHandler handler, _) = CreateRecording();

        await dispatcher.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope());

        Assert.Equal("sample", handler.FlowDuringHandling?.Name);
        Assert.Equal("corr", handler.FlowDuringHandling?.CorrelationId);
        Assert.Null(LogFlow.Current);
    }

    [Fact]
    public async Task AKnownError_RejectsTheMessage_AsAWarning_WithoutRetryAndWithoutThrowing()
    {
        (EventDispatcher dispatcher, RecordingHandler handler, ListLogger<EventDispatcher> logger) = CreateRecording(maxHandlerAttempts: 3);
        handler.FailTimes = 5;
        handler.Failure = new BusinessException("no_product", "The order has no product.");

        await dispatcher.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope());

        Assert.Equal(1, handler.Calls);
        ListLogger<EventDispatcher>.LoggedEvent end = Assert.Single(logger.Events, e => e.EventName == "CONSUME_EVENT_REJECTED");
        Assert.Equal(LogLevel.Warning, end.Level);
        Assert.Equal("ConsumeEnd", end.Properties["LogType"]);
        Assert.Equal("HsnSoft.Base.BusinessException", end.Properties["ExceptionType"]);
        Assert.Equal("no_product", end.Properties["ErrorCode"]);
        Assert.Equal("The order has no product.", end.Properties["Reason"]);
        Assert.StartsWith("Solution:", (string)end.Properties["ExceptionIdentifier"]!, StringComparison.Ordinal);
        Assert.EndsWith("rejected the message after " + end.Properties["ElapsedMs"] + " ms: The order has no product.", end.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(logger.Events, e => e.EventName is "CONSUME_EVENT_SUCCESS" or "CONSUME_EVENT_ERROR" or "CONSUME_EVENT_RETRY");
    }

    private sealed class UpperMasker : ILogMasker
    {
        public string MaskText(string? text) => text?.Replace("\"m\"", "\"*****\"", StringComparison.Ordinal) ?? string.Empty;

        public object MaskObject<T>(T value) => value!;
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    public async Task TheConsumeStartLog_HasTheMessageItStartedWith_Masked_UnlessItIsOffOrThereIsNoMasker(bool on, bool masker, bool logged)
    {
        EventBusOptions options = new() { LogConsumedMessage = on, MaxLoggedMessageLength = 200 };
        ServiceCollection services = new();
        services.AddSingleton<RecordingHandler>();
        services.AddEventHandler<SampleEto, RecordingHandler>();
        services.AddSingleton<IEventBusSubscriptionManager, InMemoryEventBusSubscriptionManager>();
        services.AddLogging();
        ServiceProvider provider = services.BuildServiceProvider();
        _providers.Add(provider);
        ListLogger<EventDispatcher> logger = new();
        EventDispatcher dispatcher = new(provider.GetRequiredService<IServiceScopeFactory>(), provider.GetRequiredService<IEventBusSubscriptionManager>(), Options.Create(options), logger, masker ? new UpperMasker() : null);

        await dispatcher.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope());

        ListLogger<EventDispatcher>.LoggedEvent start = Assert.Single(logger.Events, e => e.EventName == "CONSUME_EVENT_START");
        Assert.Equal(logged, start.Properties.ContainsKey("MessageBody"));
        if (logged)
        {
            string body = (string)start.Properties["MessageBody"]!;
            Assert.Contains("\"messageId\"", body, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"*****\"", body, StringComparison.Ordinal);
            Assert.DoesNotContain("\"m\"", body, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task AKnownErrorWithoutACode_IsRejectedToo()
    {
        (EventDispatcher dispatcher, RecordingHandler handler, ListLogger<EventDispatcher> logger) = CreateRecording();
        handler.FailTimes = 1;
        handler.Failure = new DomainException("a rule");

        await dispatcher.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope());

        Assert.Null(Assert.Single(logger.Events, e => e.EventName == "CONSUME_EVENT_REJECTED").Properties["ErrorCode"]);
    }

    [Fact]
    public async Task AnUnknownError_IsRetried_AndEndsAsAnError()
    {
        (EventDispatcher dispatcher, RecordingHandler handler, ListLogger<EventDispatcher> logger) = CreateRecording(maxHandlerAttempts: 2);
        handler.FailTimes = 5;
#pragma warning disable CA2201 // the exception a bug throws, on purpose
        handler.Failure = new NullReferenceException();
#pragma warning restore CA2201

        await Assert.ThrowsAsync<NullReferenceException>(() => dispatcher.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope()));

        Assert.Equal(2, handler.Calls);
        Assert.Equal(LogLevel.Error, Assert.Single(logger.Events, e => e.EventName == "CONSUME_EVENT_ERROR").Level);
        Assert.True(handler.Failure!.Data.Contains(EventDispatcher.ErrorLoggedKey));
        ListLogger<EventDispatcher>.LoggedEvent failed = Assert.Single(logger.Events, e => e.EventName == "CONSUME_EVENT_ERROR");
        Assert.Equal("Object reference not set to an instance of an object.", failed.Properties["Reason"]);
        Assert.NotNull(failed.Properties["ExceptionIdentifier"]);
        // how many times the handler was run before it gave up
        Assert.Equal(2, failed.Properties["Attempts"]);
        ListLogger<EventDispatcher>.LoggedEvent retried = Assert.Single(logger.Events, e => e.EventName == "CONSUME_EVENT_RETRY");
        Assert.Equal("Object reference not set to an instance of an object.", retried.Properties["Reason"]);
        Assert.DoesNotContain(logger.Events, e => e.EventName == "CONSUME_EVENT_REJECTED");
    }

    [Fact]
    public async Task FailedHandling_EndsWithAnErrorLog_AndAnInterruptedOneHasNoEnd()
    {
        (EventDispatcher failing, RecordingHandler failingHandler, ListLogger<EventDispatcher> failedLog) = CreateRecording();
        failingHandler.FailTimes = 1;
        failingHandler.Failure = new InvalidOperationException("boom");
        await Assert.ThrowsAsync<InvalidOperationException>(() => failing.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope()));

        ListLogger<EventDispatcher>.LoggedEvent error = Assert.Single(failedLog.Events, e => e.EventName == "CONSUME_EVENT_ERROR");
        Assert.Equal(LogLevel.Error, error.Level);
        Assert.Equal("ConsumeEnd", error.Properties["LogType"]);
        Assert.Equal("boom", error.Exception?.Message);

        (EventDispatcher interrupted, RecordingHandler interruptedHandler, ListLogger<EventDispatcher> interruptedLog) = CreateRecording();
        interruptedHandler.FailTimes = 1;
        interruptedHandler.Failure = new OperationCanceledException();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => interrupted.DispatchAsync("Sample", typeof(SampleEto), NewEnvelope()));

        Assert.Single(interruptedLog.Events, e => e.EventName == "CONSUME_EVENT_START");
        Assert.DoesNotContain(interruptedLog.Events, e => e.EventName is "CONSUME_EVENT_SUCCESS" or "CONSUME_EVENT_ERROR");
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
