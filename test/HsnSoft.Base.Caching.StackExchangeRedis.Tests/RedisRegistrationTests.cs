using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.Caching.StackExchangeRedis.Tests;

public class RedisRegistrationTests
{
    private static IConfiguration Configuration(params (string Key, string Value)[] settings)
        => new ConfigurationBuilder().AddInMemoryCollection(settings.ToDictionary(x => x.Key, x => (string?)x.Value)).Build();

    [Fact]
    public void ConnectionStringAndInstanceName_ReachTheCacheOptions()
    {
        var services = new ServiceCollection().AddLogging();
        services.AddBaseRedis(Configuration(("ConnectionStrings:Redis", "localhost:6390"), ("Redis:InstanceName", "acme:")));
        using var provider = services.BuildServiceProvider();

        var cache = provider.GetRequiredService<IOptions<RedisCacheOptions>>().Value;

        Assert.Equal("localhost:6390", cache.Configuration);
        Assert.Equal("acme:", cache.InstanceName);
    }

    [Fact]
    public void InstanceName_DefaultsToNoPrefix()
    {
        var services = new ServiceCollection().AddLogging();
        services.AddBaseRedis(Configuration(("ConnectionStrings:Redis", "localhost:6390")));
        using var provider = services.BuildServiceProvider();

        Assert.Equal(string.Empty, provider.GetRequiredService<IOptions<RedisCacheOptions>>().Value.InstanceName);
        Assert.Equal(string.Empty, new RedisOptions().InstanceName);
    }

    [Fact]
    public void ReadinessCheck_IsRegistered()
    {
        var services = new ServiceCollection().AddLogging();
        services.AddBaseRedis(Configuration(("ConnectionStrings:Redis", "localhost:6390")));
        using var provider = services.BuildServiceProvider();

        var registration = Assert.Single(provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations);

        Assert.Equal("redis", registration.Name);
        Assert.Contains("ready", registration.Tags);
    }

    [Fact]
    public void ConnectionNameIsConfigurable()
    {
        var services = new ServiceCollection().AddLogging();
        services.AddBaseRedis(Configuration(("ConnectionStrings:Sessions", "sessions:6379")), "Sessions");
        using var provider = services.BuildServiceProvider();

        Assert.Equal("sessions:6379", provider.GetRequiredService<IOptions<RedisCacheOptions>>().Value.Configuration);
    }

    [Fact]
    public void MissingConnectionString_IsAnError()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddBaseRedis(Configuration()));

        Assert.Contains("ConnectionStrings:Redis", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NullArguments_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => RedisServiceCollectionExtensions.AddBaseRedis(null!, Configuration()));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddBaseRedis(null!));
    }
}
