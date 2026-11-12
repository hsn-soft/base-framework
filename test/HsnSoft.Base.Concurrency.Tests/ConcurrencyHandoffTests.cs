namespace HsnSoft.Base.Concurrency.Tests;

/// <summary>A place that is handed over: the process that gets the holder can renew it and give it back, and never frees a place that was given to another.</summary>
public class ConcurrencyHandoffTests
{
    [Fact]
    public async Task ThePlaceIsGivenBackByWhoGetsItsHolder_AndThenTakenByAnother()
    {
        InMemoryConcurrencyLimiter limiter = new();
        IConcurrencyLease lease = (await limiter.TryAcquireAsync("job", 1, TimeSpan.FromMinutes(1)))!;
        Assert.Null(await limiter.TryAcquireAsync("job", 1, TimeSpan.FromMinutes(1)));

        // the holder travels (an event); the other side makes the lease of the same place and gives it back
        IConcurrencyLease attached = limiter.Attach("job", lease.Holder);
        Assert.Equal(("job", lease.Holder), (attached.Name, attached.Holder));
        Assert.True(await attached.RenewAsync(TimeSpan.FromMinutes(1)));
        await attached.DisposeAsync();

        Assert.NotNull(await limiter.TryAcquireAsync("job", 1, TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public async Task AHolderThatArrivesLate_NeverFreesAPlaceThatWasGivenToAnother()
    {
        InMemoryConcurrencyLimiter limiter = new();
        IConcurrencyLease first = (await limiter.TryAcquireAsync("job", 1, TimeSpan.FromMinutes(1)))!;
        await first.DisposeAsync();
        IConcurrencyLease second = (await limiter.TryAcquireAsync("job", 1, TimeSpan.FromMinutes(1)))!;

        IConcurrencyLease late = limiter.Attach("job", first.Holder);

        Assert.False(await late.RenewAsync(TimeSpan.FromMinutes(1)));   // the first place ended
        await late.DisposeAsync();
        Assert.Null(await limiter.TryAcquireAsync("job", 1, TimeSpan.FromMinutes(1)));   // the second is still held
        Assert.NotEqual(first.Holder, second.Holder);
    }

    [Fact]
    public void AnHolderThatIsNotOfThisLimiter_AndMissingNames_AreRefused()
    {
        InMemoryConcurrencyLimiter limiter = new();

        Assert.Throws<ArgumentException>(() => limiter.Attach("job", "not-a-holder"));
        Assert.Throws<ArgumentException>(() => limiter.Attach(" ", Guid.NewGuid().ToString("N")));
        Assert.Throws<ArgumentException>(() => limiter.Attach("job", " "));
        Assert.Throws<ArgumentNullException>(() => limiter.Attach(null!, Guid.NewGuid().ToString("N")));
    }

    [Fact]
    public async Task AnEndpointsSlot_IsTakenByTheFirstEventOnly_AndSaysSo()
    {
        InMemoryConcurrencyLimiter limiter = new();
        IConcurrencyLease lease = (await limiter.TryAcquireAsync("job", 1, TimeSpan.FromMinutes(1)))!;

        using ConcurrencyHandoffSlot slot = ConcurrencyHandoffContext.Open(lease, TimeSpan.FromSeconds(90));
        Assert.False(slot.WasTaken);

        ConcurrencyHandoff? first = ConcurrencyHandoffContext.TryTake();
        ConcurrencyHandoff? second = ConcurrencyHandoffContext.TryTake();

        Assert.Equal(new ConcurrencyHandoff("job", lease.Holder, TimeSpan.FromSeconds(90)), first);
        Assert.Null(second);   // the place is handed to one event
        Assert.True(slot.WasTaken);
    }

    [Fact]
    public async Task TheSlot_FollowsTheAsyncCallsOfTheEndpoint_AndEndsWithIt()
    {
        InMemoryConcurrencyLimiter limiter = new();
        IConcurrencyLease lease = (await limiter.TryAcquireAsync("job", 1, TimeSpan.FromMinutes(1)))!;

        Assert.Null(ConcurrencyHandoffContext.TryTake());   // no endpoint hands anything over
        ConcurrencyHandoff? taken;
        using (ConcurrencyHandoffContext.Open(lease, TimeSpan.FromSeconds(30)))
        {
            taken = await Task.Run(async () =>
            {
                await Task.Yield();
                return ConcurrencyHandoffContext.TryTake();
            });
        }

        Assert.Equal("job", taken!.Name);
        Assert.Null(ConcurrencyHandoffContext.TryTake());
    }

    [Fact]
    public async Task ASlotInsideASlot_RestoresTheOuterOne_WhenItEnds()
    {
        InMemoryConcurrencyLimiter limiter = new();
        IConcurrencyLease outer = (await limiter.TryAcquireAsync("outer", 1, TimeSpan.FromMinutes(1)))!;
        IConcurrencyLease inner = (await limiter.TryAcquireAsync("inner", 1, TimeSpan.FromMinutes(1)))!;

        using (ConcurrencyHandoffContext.Open(outer, TimeSpan.FromSeconds(30)))
        {
            using (ConcurrencyHandoffContext.Open(inner, TimeSpan.FromSeconds(30)))
            {
                Assert.Equal("inner", ConcurrencyHandoffContext.TryTake()!.Name);
            }

            Assert.Equal("outer", ConcurrencyHandoffContext.TryTake()!.Name);
        }
    }

    [Fact]
    public async Task TheSlotNeedsALeaseAndATime()
    {
        InMemoryConcurrencyLimiter limiter = new();
        IConcurrencyLease lease = (await limiter.TryAcquireAsync("job", 1, TimeSpan.FromMinutes(1)))!;

        Assert.Throws<ArgumentNullException>(() => ConcurrencyHandoffContext.Open(null!, TimeSpan.FromSeconds(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => ConcurrencyHandoffContext.Open(lease, TimeSpan.Zero));
    }

    [Fact]
    public void TheHeadersAreLocalToTheMessage()
    {
        Assert.All([ConcurrencyHandoffHeaders.Name, ConcurrencyHandoffHeaders.Holder, ConcurrencyHandoffHeaders.LeaseSeconds], name => Assert.StartsWith("Local-", name, StringComparison.Ordinal));
    }
}
