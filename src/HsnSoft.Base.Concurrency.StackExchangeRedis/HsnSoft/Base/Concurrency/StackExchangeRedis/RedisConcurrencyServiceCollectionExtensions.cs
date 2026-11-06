using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HsnSoft.Base.Concurrency.StackExchangeRedis;

public static class RedisConcurrencyServiceCollectionExtensions
{
    /// <summary>
    /// Registers the limits of the settings (<see cref="ConcurrencyOptions"/>) and the limiter that holds across every pod, on the Redis of the service: an <c>IConnectionMultiplexer</c> must be registered (<c>AddBaseRedis</c> of
    /// <c>HsnSoft.Base.Caching.StackExchangeRedis</c> does it, and the cache and the limiter share its one connection).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds the optional <c>Concurrency</c> section.</param>
    public static IServiceCollection AddRedisConcurrencyLimiter(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<ConcurrencyOptions>(configuration.GetSection(ConcurrencyOptions.SectionName));
        services.TryAddSingleton<IConcurrencyLimiter, RedisConcurrencyLimiter>();
        return services;
    }
}
