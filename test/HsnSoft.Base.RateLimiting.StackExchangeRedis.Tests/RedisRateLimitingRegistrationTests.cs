using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace HsnSoft.Base.RateLimiting.StackExchangeRedis.Tests;

/// <summary>What needs no Redis; the limiter against a real Redis, from several instances at once, is in the integration tests.</summary>
public class RedisRateLimitingRegistrationTests
{
    [Fact]
    public async Task TheRegistration_ReadsTheSettings_AndGivesTheRedisLimiter()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["RateLimiting:KeyPrefix"] = "acme" }).Build();
        ServiceCollection services = new();
        services.AddSingleton<IConnectionMultiplexer>(_ => throw new InvalidOperationException("no connection is made for a registration"));
        services.AddRedisRateLimiter(configuration);
        await using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Equal("acme", provider.GetRequiredService<IOptions<RateLimitOptions>>().Value.KeyPrefix);
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddRedisRateLimiter(configuration));
        Assert.Throws<ArgumentNullException>(() => services.AddRedisRateLimiter(null!));
    }

    [Fact]
    public async Task TheArgumentsAreCheckedBeforeRedisIsAsked()
    {
        RedisRateLimiter limiter = new(null!, Options.Create(new RateLimitOptions()));
        using CancellationTokenSource cancelled = new();
        await cancelled.CancelAsync();

        await Assert.ThrowsAsync<ArgumentException>(async () => await limiter.TryAcquireAsync(" ", "k", 1, TimeSpan.FromSeconds(1)));
        await Assert.ThrowsAsync<ArgumentException>(async () => await limiter.TryAcquireAsync("a", " ", 1, TimeSpan.FromSeconds(1)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await limiter.TryAcquireAsync("a", "k", 0, TimeSpan.FromSeconds(1)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await limiter.TryAcquireAsync("a", "k", 1, TimeSpan.Zero));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await limiter.TryAcquireAsync("a", "k", 1, TimeSpan.FromSeconds(1), cancelled.Token));
    }
}
