using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace HsnSoft.Base.Concurrency.StackExchangeRedis.Tests;

/// <summary>What needs no Redis; the limiter against a real Redis, from several instances at once, is in the integration tests.</summary>
public class RedisConcurrencyRegistrationTests
{
    [Fact]
    public async Task TheRegistration_ReadsTheSettings_AndGivesTheRedisLimiter()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Concurrency:KeyPrefix"] = "acme" }).Build();
        ServiceCollection services = new();
        services.AddSingleton<IConnectionMultiplexer>(_ => throw new InvalidOperationException("no connection is made for a registration"));
        services.AddRedisConcurrencyLimiter(configuration);
        await using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Equal("acme", provider.GetRequiredService<IOptions<ConcurrencyOptions>>().Value.KeyPrefix);
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddRedisConcurrencyLimiter(configuration));
        Assert.Throws<ArgumentNullException>(() => services.AddRedisConcurrencyLimiter(null!));
    }

    [Fact]
    public async Task TheArgumentsAreCheckedBeforeRedisIsAsked()
    {
        RedisConcurrencyLimiter limiter = new(null!, Options.Create(new ConcurrencyOptions()));
        using CancellationTokenSource cancelled = new();
        await cancelled.CancelAsync();

        await Assert.ThrowsAsync<ArgumentException>(async () => await limiter.TryAcquireAsync(" ", 1, TimeSpan.FromSeconds(1)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await limiter.TryAcquireAsync("x", 0, TimeSpan.FromSeconds(1)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await limiter.TryAcquireAsync("x", 1, TimeSpan.Zero));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await limiter.TryAcquireAsync("x", 1, TimeSpan.FromSeconds(1), cancelled.Token));
    }
}
