using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.Concurrency.Tests;

public class ConcurrencyTests
{
    private sealed class ManualTime : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 11, 3, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Pass(TimeSpan span) => _now += span;
    }

    private static readonly TimeSpan s_minute = TimeSpan.FromMinutes(1);

    [Fact]
    public async Task NoMoreThanTheMaximum_HoldAName_AndTheOthersAreToldAtOnce()
    {
        InMemoryConcurrencyLimiter limiter = new();

        IConcurrencyLease? first = await limiter.TryAcquireAsync("invoice", 2, s_minute);
        IConcurrencyLease? second = await limiter.TryAcquireAsync("invoice", 2, s_minute);
        IConcurrencyLease? third = await limiter.TryAcquireAsync("invoice", 2, s_minute);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Null(third);
        Assert.Equal("invoice", first.Name);
    }

    [Fact]
    public async Task APlaceGivenBack_IsTakenByTheNext_AndEachNameIsALimitOfItsOwn()
    {
        InMemoryConcurrencyLimiter limiter = new();
        IConcurrencyLease held = (await limiter.TryAcquireAsync("a", 1, s_minute))!;

        Assert.Null(await limiter.TryAcquireAsync("a", 1, s_minute));
        Assert.NotNull(await limiter.TryAcquireAsync("b", 1, s_minute));
        await held.DisposeAsync();
        await held.DisposeAsync();

        Assert.NotNull(await limiter.TryAcquireAsync("a", 1, s_minute));
    }

    [Fact]
    public async Task ALeaseThatEnds_GivesItsPlaceToAnother_AndARenewalExtendsIt()
    {
        ManualTime time = new();
        InMemoryConcurrencyLimiter limiter = new(time);
        IConcurrencyLease lost = (await limiter.TryAcquireAsync("work", 1, TimeSpan.FromSeconds(30)))!;

        time.Pass(TimeSpan.FromSeconds(31));
        IConcurrencyLease taken = (await limiter.TryAcquireAsync("work", 1, TimeSpan.FromSeconds(30)))!;

        Assert.False(await lost.RenewAsync(s_minute));
        Assert.Null(await limiter.TryAcquireAsync("work", 1, TimeSpan.FromSeconds(30)));
        time.Pass(TimeSpan.FromSeconds(20));
        Assert.True(await taken.RenewAsync(TimeSpan.FromSeconds(30)));
        time.Pass(TimeSpan.FromSeconds(20));
        Assert.Null(await limiter.TryAcquireAsync("work", 1, TimeSpan.FromSeconds(30)));
        time.Pass(TimeSpan.FromSeconds(11));
        Assert.NotNull(await limiter.TryAcquireAsync("work", 1, TimeSpan.FromSeconds(30)));
    }

    [Fact]
    public async Task WhateverTheNumberOfCallersAtOnce_NeverMoreThanTheMaximumHoldAtTheSameTime()
    {
        InMemoryConcurrencyLimiter limiter = new();
        int running = 0;
        int highest = 0;
        int refused = 0;

        await Task.WhenAll(Enumerable.Range(0, 200).Select(_ => Task.Run(async () =>
        {
            for (int round = 0; round < 20; round++)
            {
                IConcurrencyLease? lease = await limiter.TryAcquireAsync("hammer", 3, s_minute);
                if (lease is null)
                {
                    Interlocked.Increment(ref refused);
                    continue;
                }

                int now = Interlocked.Increment(ref running);
                int seen;
                do
                {
                    seen = Volatile.Read(ref highest);
                }
                while (now > seen && Interlocked.CompareExchange(ref highest, now, seen) != seen);

                await Task.Yield();
                Interlocked.Decrement(ref running);
                await lease.DisposeAsync();
            }
        })));

        Assert.InRange(highest, 1, 3);
        Assert.True(refused > 0);
        Assert.Equal(0, running);
    }

    [Fact]
    public async Task TheArgumentsAreChecked()
    {
        InMemoryConcurrencyLimiter limiter = new();
        IConcurrencyLease lease = (await limiter.TryAcquireAsync("x", 1, s_minute))!;
        using CancellationTokenSource cancelled = new();
        await cancelled.CancelAsync();

        await Assert.ThrowsAsync<ArgumentException>(async () => await limiter.TryAcquireAsync(" ", 1, s_minute));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await limiter.TryAcquireAsync("x", 0, s_minute));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await limiter.TryAcquireAsync("x", 1, TimeSpan.Zero));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await limiter.TryAcquireAsync("x", 1, s_minute, cancelled.Token));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await lease.RenewAsync(TimeSpan.Zero));
    }

    [Fact]
    public void TheSettings_WinOverWhatTheCodeSaid()
    {
        ConcurrencyOptions options = new();
        options.Limits["send-invoice"] = new ConcurrencyLimitSetting { Max = 5, LeaseSeconds = 120 };
        options.Limits["lease-only"] = new ConcurrencyLimitSetting { LeaseSeconds = 7 };

        Assert.Equal((5, TimeSpan.FromSeconds(120)), options.Resolve("send-invoice", 2, TimeSpan.FromSeconds(10)));
        Assert.Equal((2, TimeSpan.FromSeconds(7)), options.Resolve("lease-only", 2));
        Assert.Equal((2, TimeSpan.FromSeconds(60)), options.Resolve("other", 2));
        Assert.Equal((2, TimeSpan.FromSeconds(10)), options.Resolve("OTHER", 2, TimeSpan.FromSeconds(10)));
        Assert.Equal((5, TimeSpan.FromSeconds(120)), options.Resolve("SEND-INVOICE", 1));
        Assert.Throws<InvalidOperationException>(() => options.Resolve("bad", 0));
        Assert.Throws<InvalidOperationException>(() => options.Resolve("bad", 1, TimeSpan.Zero));
        Assert.Throws<ArgumentException>(() => options.Resolve(" ", 1));
    }

    [Fact]
    public void TheRegistration_ReadsTheSettings_AndGivesTheInMemoryLimiter()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Concurrency:KeyPrefix"] = "acme",
            ["Concurrency:RetryAfterSeconds"] = "7",
            ["Concurrency:Limits:send-invoice:Max"] = "3",
        }).Build();
        ServiceCollection services = new();
        services.AddInMemoryConcurrencyLimiter(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        ConcurrencyOptions options = provider.GetRequiredService<IOptions<ConcurrencyOptions>>().Value;

        Assert.IsType<InMemoryConcurrencyLimiter>(provider.GetRequiredService<IConcurrencyLimiter>());
        Assert.Equal(("acme", 7, 3), (options.KeyPrefix, options.RetryAfterSeconds, options.Limits["send-invoice"].Max));
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddInMemoryConcurrencyLimiter(configuration));
        Assert.Throws<ArgumentNullException>(() => services.AddInMemoryConcurrencyLimiter(null!));
    }
}
