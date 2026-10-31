using DotNet.Testcontainers.Containers;
using HsnSoft.Base.Caching.StackExchangeRedis;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace HsnSoft.Base.IntegrationTests.Caching;

[Collection(Collections.Redis)]
public sealed class RedisTests(RedisFixture redis)
{
    [Fact]
    public async Task Cache_WritesUnderTheInstanceName_AndTheServiceIsReady()
    {
        IConfiguration configuration = Support.Configuration(("ConnectionStrings:Redis", redis.Container.GetConnectionString()), ("Redis:InstanceName", "acme:"));
        using IHost host = await Support.StartHostAsync(services => services.AddBaseRedis(configuration));

        await host.Services.GetRequiredService<IDistributedCache>().SetStringAsync("greeting", "hello");

        // the Redis cache keeps an entry as a hash with the payload in the "data" field
        RedisValue raw = await host.Services.GetRequiredService<IConnectionMultiplexer>().GetDatabase().HashGetAsync("acme:greeting", "data");
        Assert.Equal("hello", raw.ToString());
        Assert.Equal(HealthStatus.Healthy, (await Support.ReadyReportAsync(host.Services)).Status);
    }

    [Fact]
    public async Task TheCacheAndTheService_ShareOneConnection_NotTwo()
    {
        IConfiguration configuration = Support.Configuration(("ConnectionStrings:Redis", redis.Container.GetConnectionString()), ("Redis:InstanceName", "shared:"));
        using IHost host = await Support.StartHostAsync(services => services.AddBaseRedis(configuration));

        await host.Services.GetRequiredService<IDistributedCache>().SetStringAsync("greeting", "hello");
        await host.Services.GetRequiredService<IConnectionMultiplexer>().GetDatabase().PingAsync();

        // a multiplexer has two connections to the server (one for commands, one for subscriptions); a service that had two multiplexers had four, in every pod
        ExecResult clients = await redis.Container.ExecAsync(["redis-cli", "client", "list"]);
        int connectionsOfTheHost = clients.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries).Count(line => !line.Contains("cmd=client", StringComparison.Ordinal));
        Assert.Equal(2, connectionsOfTheHost);
    }

    [Fact]
    public async Task TheReadinessCheck_LooksAtTheConnectionTheCacheUses_AndTheHostStopsCleanly()
    {
        IConfiguration configuration = Support.Configuration(("ConnectionStrings:Redis", redis.Container.GetConnectionString()));
        IHost host = await Support.StartHostAsync(services => services.AddBaseRedis(configuration));
        await host.Services.GetRequiredService<IDistributedCache>().SetStringAsync("greeting", "hello");
        IConnectionMultiplexer connection = host.Services.GetRequiredService<IConnectionMultiplexer>();
        Assert.Equal(HealthStatus.Healthy, (await Support.ReadyReportAsync(host.Services)).Status);

        // the cache and the container both dispose the one connection at the end: once is done, the second changes nothing, and nothing throws
        await host.StopAsync();
        host.Dispose();

        Assert.False(connection.IsConnected);
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
