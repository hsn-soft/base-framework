using HsnSoft.Base.RateLimiting;
using HsnSoft.Base.RateLimiting.StackExchangeRedis;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace HsnSoft.Base.IntegrationTests.Caching;

/// <summary>The rate limiter against a real Redis, from several "pods" at once: each pod is a connection and a limiter of its own, as the pods of a service are.</summary>
[Collection(Collections.Redis)]
public sealed class RedisRateLimitTests(RedisFixture redis) : IAsyncLifetime
{
    private static readonly TimeSpan s_minute = TimeSpan.FromMinutes(1);

    private readonly List<ConnectionMultiplexer> _pods = [];

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (ConnectionMultiplexer pod in _pods)
        {
            await pod.DisposeAsync();
        }
    }

    private async Task<IRateLimiter> PodAsync(string prefix)
    {
        ConnectionMultiplexer connection = await ConnectionMultiplexer.ConnectAsync(redis.Container.GetConnectionString());
        _pods.Add(connection);
        return new RedisRateLimiter(connection, Options.Create(new RateLimitOptions { KeyPrefix = prefix }));
    }

    private static string Prefix() => $"test-{Guid.NewGuid():N}";

    [Fact]
    public async Task PodsThatCountAtTheSameTime_NeverLetMoreThanThePermitsThroughTogether()
    {
        string prefix = Prefix();
        IRateLimiter[] pods = [.. await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => PodAsync(prefix)))];
        int allowed = 0;

        // 6 pods x 8 callers x 10 requests = 480 requests of one key against 50 permits: exactly 50 go through, however the runner schedules them
        await Task.WhenAll(pods.SelectMany(pod => Enumerable.Range(0, 8).Select(_ => Task.Run(async () =>
        {
            for (int round = 0; round < 10; round++)
            {
                if ((await pod.TryAcquireAsync("sign-in", "ip:1", 50, s_minute)).Allowed)
                {
                    Interlocked.Increment(ref allowed);
                }
            }
        }))));

        Assert.Equal(50, allowed);
    }

    [Fact]
    public async Task TheCountIsOneForEveryPod_WithWhatIsLeftAndWhenTheWindowEnds()
    {
        string prefix = Prefix();
        IRateLimiter first = await PodAsync(prefix);
        IRateLimiter second = await PodAsync(prefix);

        RateLimitResult one = await first.TryAcquireAsync("a", "k", 2, s_minute);
        RateLimitResult two = await second.TryAcquireAsync("a", "k", 2, s_minute);
        RateLimitResult three = await first.TryAcquireAsync("a", "k", 2, s_minute);

        Assert.Equal((true, 1), (one.Allowed, one.Remaining));
        Assert.Equal((true, 0), (two.Allowed, two.Remaining));
        Assert.Equal((false, 0), (three.Allowed, three.Remaining));
        Assert.InRange(three.RetryAfter, TimeSpan.FromSeconds(1), s_minute);
    }

    [Fact]
    public async Task EachKeyEachNameAndEachPrefix_HasACountOfItsOwn()
    {
        string prefix = Prefix();
        IRateLimiter pod = await PodAsync(prefix);
        IRateLimiter other = await PodAsync(Prefix());
        await pod.TryAcquireAsync("a", "k", 1, s_minute);

        Assert.False((await pod.TryAcquireAsync("a", "k", 1, s_minute)).Allowed);
        Assert.True((await pod.TryAcquireAsync("a", "other-key", 1, s_minute)).Allowed);
        Assert.True((await pod.TryAcquireAsync("other-name", "k", 1, s_minute)).Allowed);
        Assert.True((await other.TryAcquireAsync("a", "k", 1, s_minute)).Allowed);   // a service with its own prefix does not meet the counts of this one
    }

    [Fact]
    public async Task TheWindowEnds_ByTheClockOfRedis_AndTheCallerStartsAgain()
    {
        IRateLimiter pod = await PodAsync(Prefix());
        Assert.True((await pod.TryAcquireAsync("short", "k", 1, TimeSpan.FromSeconds(1))).Allowed);
        Assert.False((await pod.TryAcquireAsync("short", "k", 1, TimeSpan.FromSeconds(1))).Allowed);

        // the refused requests do not lengthen the window: it ends one second after the first request
        await Support.EventuallyAsync(async () => (await pod.TryAcquireAsync("short", "k", 1, TimeSpan.FromSeconds(1))).Allowed, "the window of the first request ended", TimeSpan.FromSeconds(15));
    }
}
