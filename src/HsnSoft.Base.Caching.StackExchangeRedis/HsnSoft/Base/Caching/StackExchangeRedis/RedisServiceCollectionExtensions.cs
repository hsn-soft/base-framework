using System;
using HsnSoft.Base.Hosting;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace HsnSoft.Base.Caching.StackExchangeRedis;

public static class RedisServiceCollectionExtensions
{
    /// <summary>
    /// Registers <c>IDistributedCache</c>, the raw <see cref="IConnectionMultiplexer"/> and a readiness health check.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds <c>ConnectionStrings:{connectionName}</c> and the optional <c>Redis</c> section (<see cref="RedisOptions"/>).</param>
    /// <param name="connectionName">The name of the connection string.</param>
    public static IServiceCollection AddBaseRedis(this IServiceCollection services, IConfiguration configuration, string connectionName = "Redis")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        string connectionString = configuration.GetConnectionString(connectionName)
                               ?? throw new InvalidOperationException($"ConnectionStrings:{connectionName} is required.");

        services.Configure<RedisOptions>(configuration.GetSection("Redis"));

        services.AddStackExchangeRedisCache(_ => { });
        services.AddOptions<RedisCacheOptions>().Configure<IOptions<RedisOptions>>((cache, redis) =>
        {
            cache.Configuration = connectionString;
            cache.InstanceName = redis.Value.InstanceName;
        });

        services.TryAddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(connectionString);
            options.AbortOnConnectFail = false; // the service may start before Redis does
            return ConnectionMultiplexer.Connect(options);
        });

        services.AddHealthChecks().AddCheck<RedisHealthCheck>("redis", tags: [HealthCheckTags.Ready]);
        return services;
    }
}
