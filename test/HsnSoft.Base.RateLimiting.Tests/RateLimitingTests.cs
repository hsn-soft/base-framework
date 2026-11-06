using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.RateLimiting.Tests;

public class RateLimitingTests
{
    private sealed class ManualTime : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 11, 6, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Pass(TimeSpan span) => _now += span;
    }

    private static readonly TimeSpan s_minute = TimeSpan.FromMinutes(1);

    [Fact]
    public async Task ACallerMayMakeThePermits_AndTheNextIsRefused_WithWhatIsLeftAndWhenItEnds()
    {
        ManualTime time = new();
        InMemoryRateLimiter limiter = new(time);

        RateLimitResult first = await limiter.TryAcquireAsync("sign-in", "ip:1", 3, s_minute);
        time.Pass(TimeSpan.FromSeconds(20));
        RateLimitResult second = await limiter.TryAcquireAsync("sign-in", "ip:1", 3, s_minute);
        RateLimitResult third = await limiter.TryAcquireAsync("sign-in", "ip:1", 3, s_minute);
        RateLimitResult fourth = await limiter.TryAcquireAsync("sign-in", "ip:1", 3, s_minute);

        Assert.Equal(new RateLimitResult(true, 2, s_minute), first);
        Assert.Equal(new RateLimitResult(true, 1, TimeSpan.FromSeconds(40)), second);
        Assert.True(third.Allowed);
        Assert.Equal(0, third.Remaining);
        Assert.Equal(new RateLimitResult(false, 0, TimeSpan.FromSeconds(40)), fourth);
    }

    [Fact]
    public async Task ARefusedRequestIsCounted_SoACallerThatKeepsKnockingStaysRefusedUntilTheWindowEnds()
    {
        ManualTime time = new();
        InMemoryRateLimiter limiter = new(time);
        await limiter.TryAcquireAsync("a", "k", 1, s_minute);

        for (int attempt = 0; attempt < 5; attempt++)
        {
            time.Pass(TimeSpan.FromSeconds(10));
            Assert.False((await limiter.TryAcquireAsync("a", "k", 1, s_minute)).Allowed);
        }

        time.Pass(TimeSpan.FromSeconds(10));   // the window of the first request has ended: the count starts again
        Assert.True((await limiter.TryAcquireAsync("a", "k", 1, s_minute)).Allowed);
    }

    [Fact]
    public async Task EachKeyAndEachName_HasACountOfItsOwn()
    {
        InMemoryRateLimiter limiter = new(new ManualTime());
        await limiter.TryAcquireAsync("a", "one", 1, s_minute);

        Assert.False((await limiter.TryAcquireAsync("a", "one", 1, s_minute)).Allowed);
        Assert.True((await limiter.TryAcquireAsync("a", "two", 1, s_minute)).Allowed);
        Assert.True((await limiter.TryAcquireAsync("b", "one", 1, s_minute)).Allowed);
    }

    [Fact]
    public async Task TheEndedWindows_AreDroppedAsTheLimiterIsUsed()
    {
        ManualTime time = new();
        InMemoryRateLimiter limiter = new(time);
        for (int caller = 0; caller < 1500; caller++)
        {
            await limiter.TryAcquireAsync("a", "k" + caller, 1, s_minute);
        }

        time.Pass(TimeSpan.FromMinutes(2));
        for (int caller = 0; caller < 1100; caller++)
        {
            Assert.True((await limiter.TryAcquireAsync("a", "k" + caller, 1, s_minute)).Allowed);   // every window had ended, swept or not
        }
    }

    [Fact]
    public async Task TheArgumentsAreChecked()
    {
        InMemoryRateLimiter limiter = new();
        using CancellationTokenSource cancelled = new();
        await cancelled.CancelAsync();

        await Assert.ThrowsAsync<ArgumentException>(async () => await limiter.TryAcquireAsync(" ", "k", 1, s_minute));
        await Assert.ThrowsAsync<ArgumentException>(async () => await limiter.TryAcquireAsync("a", "", 1, s_minute));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await limiter.TryAcquireAsync("a", "k", 0, s_minute));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await limiter.TryAcquireAsync("a", "k", 1, TimeSpan.Zero));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await limiter.TryAcquireAsync("a", "k", 1, s_minute, cancelled.Token));
    }

    [Fact]
    public void TheSettings_WinOverWhatTheCodeSaid_AndACodeValueIsUsedWhenTheyAreSilent()
    {
        RateLimitOptions options = new() { Limits = { ["sign-in"] = new RateLimitSetting { Permits = 5 }, ["export"] = new RateLimitSetting { Permits = 2, WindowSeconds = 30 } } };

        Assert.Equal((5, TimeSpan.FromSeconds(60)), options.Resolve("SIGN-IN", 10, 60));   // the name has no case
        Assert.Equal((2, TimeSpan.FromSeconds(30)), options.Resolve("export", 10, 60));
        Assert.Equal((10, TimeSpan.FromSeconds(60)), options.Resolve("other", 10, 60));
    }

    [Fact]
    public void ALimitThatCannotBeRight_IsRefused()
    {
        RateLimitOptions options = new() { Limits = { ["a"] = new RateLimitSetting { Permits = 0 }, ["b"] = new RateLimitSetting { WindowSeconds = 0 } } };

        Assert.Throws<InvalidOperationException>(() => options.Resolve("a", 1, 60));
        Assert.Throws<InvalidOperationException>(() => options.Resolve("b", 1, 60));
        Assert.Throws<InvalidOperationException>(() => options.Resolve("c", 0, 60));
        Assert.Throws<ArgumentException>(() => options.Resolve(" ", 1, 60));
    }

    [Fact]
    public async Task TheRegistration_ReadsTheSettings_AndUsesTheTimeOfTheContainer()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["RateLimiting:KeyPrefix"] = "acme", ["RateLimiting:FailOpen"] = "true", ["RateLimiting:Limits:a:Permits"] = "7" }).Build();
        ManualTime time = new();
        ServiceCollection services = new();
        services.AddSingleton<TimeProvider>(time);
        services.AddInMemoryRateLimiter(configuration);
        await using ServiceProvider provider = services.BuildServiceProvider();
        RateLimitOptions options = provider.GetRequiredService<IOptions<RateLimitOptions>>().Value;
        IRateLimiter limiter = provider.GetRequiredService<IRateLimiter>();

        Assert.Equal("acme", options.KeyPrefix);
        Assert.True(options.FailOpen);
        Assert.Equal(7, options.Limits["a"].Permits);
        await limiter.TryAcquireAsync("x", "k", 1, s_minute);
        Assert.False((await limiter.TryAcquireAsync("x", "k", 1, s_minute)).Allowed);
        time.Pass(TimeSpan.FromMinutes(2));
        Assert.True((await limiter.TryAcquireAsync("x", "k", 1, s_minute)).Allowed);   // it counts by the clock of the container
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddInMemoryRateLimiter(configuration));
        Assert.Throws<ArgumentNullException>(() => services.AddInMemoryRateLimiter(null!));
    }
}
