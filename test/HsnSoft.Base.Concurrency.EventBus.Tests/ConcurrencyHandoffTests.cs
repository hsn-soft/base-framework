using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.Concurrency.EventBus.Tests;

public sealed record JobEto(string Name) : IIntegrationEventMessage;

public sealed record FollowUpEto(string Name) : IIntegrationEventMessage;

/// <summary>What the handler of the job does (a test says it) and what it saw of the place while it worked.</summary>
public sealed class JobHandler(IConcurrencyLimiter limiter) : IIntegrationEventHandler<JobEto>
{
    public Func<Task>? Work { get; set; }

    public bool? PlaceWasTakenWhileWorking { get; private set; }

    public async Task HandleAsync(MessageEnvelope<JobEto> envelope, CancellationToken cancellationToken)
    {
        // while the job works the place is held: nobody else gets it
        IConcurrencyLease? other = await limiter.TryAcquireAsync("nightly-job", 1, TimeSpan.FromMinutes(1), cancellationToken);
        PlaceWasTakenWhileWorking = other is null;
        if (other is not null)
        {
            await other.DisposeAsync();
        }

        if (Work is not null)
        {
            await Work();
        }
    }
}

/// <summary>
/// The place of a concurrency limit travels with an event: an endpoint takes it, publishes, answers at once; the handler of the event keeps it alive while it works and gives it back when it ends, whatever way, and never frees a place it no longer owns.
/// The pieces are the real ones (the header provider, the envelope factory, the dispatcher, the in-memory limiter).
/// </summary>
public sealed class ConcurrencyHandoffFlowTests : IDisposable
{
    private readonly ServiceProvider _provider;

    public ConcurrencyHandoffFlowTests()
    {
        ServiceCollection services = new();
        services.AddSingleton(Options.Create(new EventBusOptions { ExchangeName = "HHS", ConsumerClientName = "JobService", MaxHandlerAttempts = 1, RetryDelay = TimeSpan.FromMilliseconds(1) }));
        services.AddLogging();
        services.AddSingleton<IConcurrencyLimiter>(new InMemoryConcurrencyLimiter());
        services.AddConcurrencyHandoff();
        services.AddSingleton<JobHandler>();
        services.AddEventHandler<JobEto, JobHandler>();
        services.AddSingleton<IEventBusSubscriptionManager, InMemoryEventBusSubscriptionManager>();
        services.AddSingleton<IEventDispatcher, EventDispatcher>();
        services.AddSingleton<EventEnvelopeFactory>();
        _provider = services.BuildServiceProvider();
    }

    public void Dispose() => _provider.Dispose();

    private IConcurrencyLimiter Limiter => _provider.GetRequiredService<IConcurrencyLimiter>();

    private JobHandler Handler => _provider.GetRequiredService<JobHandler>();

    /// <summary>What the endpoint does: takes the place, opens the slot, publishes (the envelope is made while the slot is open) and answers.</summary>
    private async Task<(MessageEnvelope<JobEto> Envelope, ConcurrencyHandoffSlot Slot, IConcurrencyLease Lease)> EndpointPublishesAsync()
    {
        IConcurrencyLease lease = (await Limiter.TryAcquireAsync("nightly-job", 1, TimeSpan.FromSeconds(30)))!;
        ConcurrencyHandoffSlot slot = ConcurrencyHandoffContext.Open(lease, TimeSpan.FromSeconds(30));
        MessageEnvelope<JobEto> envelope = _provider.GetRequiredService<EventEnvelopeFactory>().Create(new JobEto("nightly"), null, null, isReQueuePublish: false);
        return (envelope, slot, lease);
    }

    private async Task<bool> PlaceIsFreeAsync()
    {
        IConcurrencyLease? lease = await Limiter.TryAcquireAsync("nightly-job", 1, TimeSpan.FromMinutes(1));
        if (lease is null)
        {
            return false;
        }

        await lease.DisposeAsync();
        return true;
    }

    [Fact]
    public async Task TheEventCarriesThePlace_TheHandlerWorksWhileItIsHeld_AndGivesItBackWhenItEnds()
    {
        (MessageEnvelope<JobEto> envelope, ConcurrencyHandoffSlot slot, IConcurrencyLease lease) = await EndpointPublishesAsync();
        using (slot)
        {
            Assert.True(slot.WasTaken);   // the endpoint does not give the place back: the event took it
        }

        Assert.Equal((lease.Name, lease.Holder, "30"), (envelope.GetHeader(ConcurrencyHandoffHeaders.Name), envelope.GetHeader(ConcurrencyHandoffHeaders.Holder), envelope.GetHeader(ConcurrencyHandoffHeaders.LeaseSeconds)));
        Assert.False(await PlaceIsFreeAsync());   // the endpoint has answered and the job has not started: still held

        await _provider.GetRequiredService<IEventDispatcher>().DispatchAsync("Job", typeof(JobEto), envelope);

        Assert.True(Handler.PlaceWasTakenWhileWorking);
        Assert.True(await PlaceIsFreeAsync());   // the job ended: the place is given back
    }

    [Fact]
    public async Task AHandlerThatFails_GivesThePlaceBackToo_TheLeaseIsOnlyTheSafetyForAPodThatDies()
    {
        (MessageEnvelope<JobEto> envelope, ConcurrencyHandoffSlot slot, _) = await EndpointPublishesAsync();
        slot.Dispose();
        Handler.Work = () => throw new InvalidOperationException("the job failed");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _provider.GetRequiredService<IEventDispatcher>().DispatchAsync("Job", typeof(JobEto), envelope));

        Assert.True(await PlaceIsFreeAsync());
    }

    [Fact]
    public async Task AHandlingCutShortByAShutdown_KeepsThePlace_TheMessageComesBackAndTheLeaseEndsByItself()
    {
        (MessageEnvelope<JobEto> envelope, ConcurrencyHandoffSlot slot, _) = await EndpointPublishesAsync();
        slot.Dispose();
        Handler.Work = () => throw new OperationCanceledException();

        await Assert.ThrowsAsync<OperationCanceledException>(() => _provider.GetRequiredService<IEventDispatcher>().DispatchAsync("Job", typeof(JobEto), envelope));

        Assert.False(await PlaceIsFreeAsync());
    }

    [Fact]
    public async Task TheEventsTheHandlerPublishes_DoNotCarryThePlaceOn()
    {
        (MessageEnvelope<JobEto> envelope, ConcurrencyHandoffSlot slot, _) = await EndpointPublishesAsync();
        slot.Dispose();

        MessageEnvelope<FollowUpEto> followUp = _provider.GetRequiredService<EventEnvelopeFactory>().Create(new FollowUpEto("next"), envelope, null, isReQueuePublish: false);

        Assert.Null(followUp.GetHeader(ConcurrencyHandoffHeaders.Holder));
        Assert.Null(followUp.GetHeader(ConcurrencyHandoffHeaders.Name));
    }

    [Fact]
    public async Task OnlyTheFirstEventOfTheEndpoint_CarriesThePlace()
    {
        (MessageEnvelope<JobEto> first, ConcurrencyHandoffSlot slot, _) = await EndpointPublishesAsync();
        using (slot)
        {
            MessageEnvelope<FollowUpEto> second = _provider.GetRequiredService<EventEnvelopeFactory>().Create(new FollowUpEto("also"), null, null, isReQueuePublish: false);

            Assert.NotNull(first.GetHeader(ConcurrencyHandoffHeaders.Holder));
            Assert.Null(second.GetHeader(ConcurrencyHandoffHeaders.Holder));
        }
    }

    [Fact]
    public async Task AMessageThatIsHandledAgainAfterItsPlaceWasGivenAway_NeverFreesAPlaceOfAnotherRun()
    {
        (MessageEnvelope<JobEto> envelope, ConcurrencyHandoffSlot slot, _) = await EndpointPublishesAsync();
        slot.Dispose();
        await _provider.GetRequiredService<IEventDispatcher>().DispatchAsync("Job", typeof(JobEto), envelope);   // the job ran: the place is free
        IConcurrencyLease nextRun = (await Limiter.TryAcquireAsync("nightly-job", 1, TimeSpan.FromMinutes(1)))!;   // the next run of the job took it

        // the same message is delivered again (a requeue): it has the old holder, and the old holder owns nothing
        await _provider.GetRequiredService<IEventDispatcher>().DispatchAsync("Job", typeof(JobEto), envelope);

        Assert.Null(await Limiter.TryAcquireAsync("nightly-job", 1, TimeSpan.FromMinutes(1)));   // the next run still holds its place
        await nextRun.DisposeAsync();
    }

    [Fact]
    public async Task AMessageWithNoPlace_IsLeftAlone()
    {
        MessageEnvelope<JobEto> envelope = _provider.GetRequiredService<EventEnvelopeFactory>().Create(new JobEto("plain"), null, null, isReQueuePublish: false);
        IConcurrencyLease held = (await Limiter.TryAcquireAsync("nightly-job", 1, TimeSpan.FromMinutes(1)))!;

        await _provider.GetRequiredService<IEventDispatcher>().DispatchAsync("Job", typeof(JobEto), envelope);

        Assert.Null(envelope.GetHeader(ConcurrencyHandoffHeaders.Holder));
        Assert.False(await PlaceIsFreeAsync());   // what is held by someone else stays held
        await held.DisposeAsync();
    }

    [Fact]
    public void TheHandoffIsRegistered_AsTheHeaderProviderOfTheEventsAndTheObserverOfTheirHandling()
    {
        ServiceCollection services = new();

        services.AddConcurrencyHandoff();

        Assert.Contains(services, service => service.ServiceType == typeof(IEventHeaderProvider) && service.ImplementationType == typeof(ConcurrencyHandoffHeaderProvider));
        Assert.Contains(services, service => service.ServiceType == typeof(IEventHandlingObserver) && service.ImplementationType == typeof(ConcurrencyHandoffObserver));
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddConcurrencyHandoff());
    }

    [Fact]
    public void TheHeadersAreTheLocalOnesOfTheEventBus_SoTheyAreNotInherited()
    {
        Assert.All([ConcurrencyHandoffHeaders.Name, ConcurrencyHandoffHeaders.Holder, ConcurrencyHandoffHeaders.LeaseSeconds], header => Assert.StartsWith(ParentMessageEnvelope.LocalHeaderPrefix, header, StringComparison.Ordinal));
    }
}

/// <summary>The observer on its own: what it reads from the headers and how it keeps a place alive.</summary>
public sealed class ConcurrencyHandoffObserverTests
{
    private sealed class CountingLease(string name, string holder, bool renews = true) : IConcurrencyLease
    {
        private int _renewals;
        private int _disposals;

        public string Name => name;

        public string Holder => holder;

        public int Renewals => Volatile.Read(ref _renewals);

        public int Disposals => Volatile.Read(ref _disposals);

        public TaskCompletionSource Renewed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ValueTask<bool> RenewAsync(TimeSpan leaseTime, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _renewals) >= 2)
            {
                Renewed.TrySetResult();
            }

            return ValueTask.FromResult(renews);
        }

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposals);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeLimiter(CountingLease lease) : IConcurrencyLimiter
    {
        public List<(string Name, string Holder)> Attached { get; } = [];

        public ValueTask<IConcurrencyLease?> TryAcquireAsync(string name, int maxConcurrent, TimeSpan leaseTime, CancellationToken cancellationToken = default) => ValueTask.FromResult<IConcurrencyLease?>(null);

        public IConcurrencyLease Attach(string name, string holder)
        {
            Attached.Add((name, holder));
            return lease;
        }
    }

    private static MessageEnvelope<JobEto> Envelope(string? name = "job", string? holder = "abc", string? seconds = "1")
    {
        MessageEnvelope<JobEto> envelope = new() { MessageId = Guid.NewGuid(), Message = new JobEto("x") };
        envelope.SetHeader(ConcurrencyHandoffHeaders.Name, name);
        envelope.SetHeader(ConcurrencyHandoffHeaders.Holder, holder);
        envelope.SetHeader(ConcurrencyHandoffHeaders.LeaseSeconds, seconds);
        return envelope;
    }

    private static ConcurrencyHandoffObserver Observer(FakeLimiter limiter) => new(limiter, NullLogger<ConcurrencyHandoffObserver>.Instance);

    [Fact]
    public async Task WhileTheHandlerWorks_ThePlaceIsRenewed_UntilTheHandlingEnds()
    {
        CountingLease lease = new("job", "abc");
        FakeLimiter limiter = new(lease);

        IAsyncDisposable? heartbeat = await Observer(limiter).StartedAsync(Envelope(), CancellationToken.None);
        await lease.Renewed.Task.WaitAsync(TimeSpan.FromSeconds(30));   // renewed again and again, three times in a lease time
        await heartbeat!.DisposeAsync();
        int atTheEnd = lease.Renewals;
        await Task.Delay(100);

        Assert.Equal(("job", "abc"), Assert.Single(limiter.Attached));
        Assert.Equal(atTheEnd, lease.Renewals);   // it stopped with the handling
        Assert.Equal(0, lease.Disposals);   // it renews: giving back is the end of the handling
    }

    [Fact]
    public async Task APlaceThatHasEnded_IsToldOnceAndNotRenewedAgain()
    {
        CountingLease lease = new("job", "abc", renews: false);
        IAsyncDisposable? heartbeat = await Observer(new FakeLimiter(lease)).StartedAsync(Envelope(), CancellationToken.None);

        await Task.Delay(1500);
        await heartbeat!.DisposeAsync();

        Assert.Equal(1, lease.Renewals);
    }

    [Theory]
    [InlineData(null, "abc", "1")]
    [InlineData("job", null, "1")]
    [InlineData("job", "abc", null)]
    [InlineData("job", "abc", "0")]
    [InlineData("job", "abc", "soon")]
    public async Task AMessageWithoutACompletePlace_IsNotObserved(string? name, string? holder, string? seconds)
    {
        CountingLease lease = new("job", "abc");
        FakeLimiter limiter = new(lease);
        ConcurrencyHandoffObserver observer = Observer(limiter);

        IAsyncDisposable? heartbeat = await observer.StartedAsync(Envelope(name, holder, seconds), CancellationToken.None);
        await observer.EndedAsync(Envelope(name, holder, seconds), EventHandlingOutcome.Handled, CancellationToken.None);

        Assert.Null(heartbeat);
        Assert.Empty(limiter.Attached);
        Assert.Equal(0, lease.Disposals);
    }

    [Theory]
    [InlineData(EventHandlingOutcome.Handled)]
    [InlineData(EventHandlingOutcome.Rejected)]
    [InlineData(EventHandlingOutcome.Failed)]
    public async Task WhateverWayTheHandlingEnds_ThePlaceIsGivenBackByItsHolder(EventHandlingOutcome outcome)
    {
        CountingLease lease = new("job", "abc");
        FakeLimiter limiter = new(lease);

        await Observer(limiter).EndedAsync(Envelope(), outcome, CancellationToken.None);

        Assert.Equal(("job", "abc"), Assert.Single(limiter.Attached));
        Assert.Equal(1, lease.Disposals);
    }

    [Fact]
    public async Task TheInputsAreRequired()
    {
        ConcurrencyHandoffObserver observer = Observer(new FakeLimiter(new CountingLease("job", "abc")));

        await Assert.ThrowsAsync<ArgumentNullException>(async () => await observer.StartedAsync(null!, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await observer.EndedAsync(null!, EventHandlingOutcome.Handled, CancellationToken.None));
        Assert.Throws<ArgumentNullException>(() => new ConcurrencyHandoffHeaderProvider().AddHeaders(null!));
        Assert.Null(new ConcurrencyHandoffHeaderProvider().GetCorrelationId());
    }
}
