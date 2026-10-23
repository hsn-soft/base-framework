using HsnSoft.Base.Caching.StackExchangeRedis;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace HsnSoft.Base.IntegrationTests.Caching;

public sealed class RedisTests : IAsyncLifetime
{
    private readonly RedisContainer _container = new RedisBuilder("redis:7").Build();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    [Fact]
    public async Task Cache_WritesUnderTheInstanceName_AndTheServiceIsReady()
    {
        IConfiguration configuration = Support.Configuration(("ConnectionStrings:Redis", _container.GetConnectionString()), ("Redis:InstanceName", "acme:"));
        using IHost host = await Support.StartHostAsync(services => services.AddBaseRedis(configuration));

        await host.Services.GetRequiredService<IDistributedCache>().SetStringAsync("greeting", "hello");

        // the Redis cache keeps an entry as a hash with the payload in the "data" field
        RedisValue raw = await host.Services.GetRequiredService<IConnectionMultiplexer>().GetDatabase().HashGetAsync("acme:greeting", "data");
        Assert.Equal("hello", raw.ToString());
        Assert.Equal(HealthStatus.Healthy, (await Support.ReadyReportAsync(host.Services)).Status);
    }

    [Fact]
    public async Task Service_StartsBeforeRedis_AndReportsItNotReady()
    {
        IConfiguration configuration = Support.Configuration(("ConnectionStrings:Redis", "127.0.0.1:1,connectTimeout=500"));
        using IHost host = await Support.StartHostAsync(services => services.AddBaseRedis(configuration));

        HealthReport report = await Support.ReadyReportAsync(host.Services);

        Assert.Equal(HealthStatus.Unhealthy, report.Status);
    }
}
