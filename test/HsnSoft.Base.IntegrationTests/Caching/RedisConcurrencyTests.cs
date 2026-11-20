using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.Concurrency;
using HsnSoft.Base.Concurrency.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace HsnSoft.Base.IntegrationTests.Caching;

/// <summary>The limiter against a real Redis, from several "pods" at once: each pod is a connection and a limiter of its own, as the pods of a service are.</summary>
[Collection(Collections.Redis)]
public sealed class RedisConcurrencyTests(RedisFixture redis) : IAsyncLifetime
{
    private readonly List<ConnectionMultiplexer> _pods = [];

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (ConnectionMultiplexer pod in _pods)
        {
            await pod.DisposeAsync();
        }
    }

    private async Task<IConcurrencyLimiter> PodAsync(string prefix)
    {
        ConnectionMultiplexer connection = await ConnectionMultiplexer.ConnectAsync(redis.Container.GetConnectionString());
        _pods.Add(connection);
        return new RedisConcurrencyLimiter(connection, Options.Create(new ConcurrencyOptions { KeyPrefix = prefix }));
    }

    private static string Prefix() => $"test-{Guid.NewGuid():N}";

    [Fact]
    public async Task PodsThatAskAtTheSameTime_NeverHoldMoreThanTheMaximumTogether()
    {
        string prefix = Prefix();
        IConcurrencyLimiter[] pods = [.. await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => PodAsync(prefix)))];
        int running = 0;
        int highest = 0;
        int taken = 0;

        await Task.WhenAll(pods.SelectMany(pod => Enumerable.Range(0, 8).Select(_ => Task.Run(async () =>
        {
            for (int round = 0; round < 15; round++)
            {
                IConcurrencyLease? lease = await pod.TryAcquireAsync("endpoint", 2, TimeSpan.FromSeconds(30));
                if (lease is null)
                {
                    continue;
                }

                int now = Interlocked.Increment(ref running);
                int seen;
                do
                {
                    seen = Volatile.Read(ref highest);
                }
                while (now > seen && Interlocked.CompareExchange(ref highest, now, seen) != seen);

                Interlocked.Increment(ref taken);
                await Task.Delay(2);
                Interlocked.Decrement(ref running);
                await lease.DisposeAsync();
            }
        }))));

        Assert.InRange(highest, 1, 2);
        Assert.True(taken > 0);   // how many were refused depends on how the runner schedules the callers, so it is not asserted: that the places are refused when they are taken is the test above this one's job
        Assert.Equal(0, running);
    }

    [Fact]
    public async Task APlaceIsGivenBack_ToAnyPod_AndEachNameIsALimitOfItsOwn()
    {
        string prefix = Prefix();
        IConcurrencyLimiter first = await PodAsync(prefix);
        IConcurrencyLimiter second = await PodAsync(prefix);
        IConcurrencyLease held = (await first.TryAcquireAsync("a", 1, TimeSpan.FromSeconds(30)))!;

        Assert.Null(await second.TryAcquireAsync("a", 1, TimeSpan.FromSeconds(30)));
        Assert.NotNull(await second.TryAcquireAsync("b", 1, TimeSpan.FromSeconds(30)));
        await held.DisposeAsync();

        Assert.NotNull(await second.TryAcquireAsync("a", 1, TimeSpan.FromSeconds(30)));
        Assert.Equal("a", held.Name);
    }

    [Fact]
    public async Task APodThatDies_LosesItsPlaceWhenItsLeaseEnds()
    {
        string prefix = Prefix();
        IConcurrencyLimiter dying = await PodAsync(prefix);
        IConcurrencyLimiter living = await PodAsync(prefix);
        await dying.TryAcquireAsync("work", 1, TimeSpan.FromMilliseconds(600));

        Assert.Null(await living.TryAcquireAsync("work", 1, TimeSpan.FromSeconds(30)));
        await Support.EventuallyAsync(async () => await living.TryAcquireAsync("work", 1, TimeSpan.FromSeconds(30)) is not null, "the lease of the pod that did not give its place back ended", TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task ALeaseThatIsRenewed_Lasts_AndOneThatEndedCannotBeRenewed()
    {
        string prefix = Prefix();
        IConcurrencyLimiter pod = await PodAsync(prefix);
        IConcurrencyLease renewed = (await pod.TryAcquireAsync("long", 1, TimeSpan.FromMilliseconds(800)))!;

        await Task.Delay(500);
        Assert.True(await renewed.RenewAsync(TimeSpan.FromSeconds(30)));
        await Task.Delay(600);
        Assert.Null(await pod.TryAcquireAsync("long", 1, TimeSpan.FromSeconds(30)));

        IConcurrencyLease shortLived = (await pod.TryAcquireAsync("short", 1, TimeSpan.FromMilliseconds(300)))!;
        await Task.Delay(500);
        Assert.False(await shortLived.RenewAsync(TimeSpan.FromSeconds(30)));
        Assert.NotNull(await pod.TryAcquireAsync("short", 1, TimeSpan.FromSeconds(30)));
    }

    [Fact]
    public async Task TheLimiterAndTheCache_ShareOneConnection_WhenTheServiceRegistersBoth()
    {
        IConfiguration configuration = Support.Configuration(("ConnectionStrings:Redis", redis.Container.GetConnectionString()), ("Concurrency:KeyPrefix", Prefix()));
        using IHost host = await Support.StartHostAsync(services => services.AddBaseRedis(configuration).AddRedisConcurrencyLimiter(configuration));

        IConcurrencyLimiter limiter = host.Services.GetRequiredService<IConcurrencyLimiter>();
        IConcurrencyLease? lease = await limiter.TryAcquireAsync("registered", 1, TimeSpan.FromSeconds(30));

        Assert.NotNull(lease);
        Assert.Null(await limiter.TryAcquireAsync("registered", 1, TimeSpan.FromSeconds(30)));
        Assert.IsType<RedisConcurrencyLimiter>(limiter);
    }

    [Fact]
    public async Task APlaceTakenByOnePod_IsGivenBackByAnotherPod_WhichGetsItsHolder_AndNeverFreesAPlaceItDoesNotOwn()
    {
        string prefix = Prefix();
        IConcurrencyLimiter endpointPod = await PodAsync(prefix);
        IConcurrencyLimiter handlerPod = await PodAsync(prefix);

        // the endpoint of one pod takes the place and hands it over (its holder travels with an event); the handler of another pod keeps it alive and gives it back
        IConcurrencyLease taken = (await endpointPod.TryAcquireAsync("nightly-job", 1, TimeSpan.FromSeconds(30)))!;
        IConcurrencyLease attached = handlerPod.Attach("nightly-job", taken.Holder);

        Assert.Null(await endpointPod.TryAcquireAsync("nightly-job", 1, TimeSpan.FromSeconds(30)));
        Assert.True(await attached.RenewAsync(TimeSpan.FromSeconds(30)));
        await attached.DisposeAsync();
        IConcurrencyLease next = (await endpointPod.TryAcquireAsync("nightly-job", 1, TimeSpan.FromSeconds(30)))!;   // the place is free for the next run

        // a late holder (the same message delivered again) owns nothing: it cannot renew, and giving it back leaves the next run's place alone
        IConcurrencyLease late = handlerPod.Attach("nightly-job", taken.Holder);
        Assert.False(await late.RenewAsync(TimeSpan.FromSeconds(30)));
        await late.DisposeAsync();
        Assert.Null(await endpointPod.TryAcquireAsync("nightly-job", 1, TimeSpan.FromSeconds(30)));
        Assert.NotEqual(taken.Holder, next.Holder);
    }
}
