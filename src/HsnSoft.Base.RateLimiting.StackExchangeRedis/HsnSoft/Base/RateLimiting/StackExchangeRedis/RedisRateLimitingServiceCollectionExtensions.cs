using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HsnSoft.Base.RateLimiting.StackExchangeRedis;

public static class RedisRateLimitingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the limits of the settings (<see cref="RateLimitOptions"/>, section <c>RateLimiting</c>) and the limiter that holds across every pod, on the Redis of the service: an <c>IConnectionMultiplexer</c> must be registered (<c>AddBaseRedis</c> of
    /// <c>HsnSoft.Base.Caching.StackExchangeRedis</c> does it, and the cache and the limiter share its one connection).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds the optional <c>RateLimiting</c> section.</param>
    public static IServiceCollection AddRedisRateLimiter(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<RateLimitOptions>(configuration.GetSection(RateLimitOptions.SectionName));
        services.TryAddSingleton<IRateLimiter, RedisRateLimiter>();
        return services;
    }
}
