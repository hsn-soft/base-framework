using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HsnSoft.Base.RateLimiting;

public static class RateLimitingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the limits of the settings (<see cref="RateLimitOptions"/>, section <c>RateLimiting</c>) and the limiter that counts in the memory of one process (it uses the <see cref="TimeProvider"/> of the container when there is one). A cluster registers
    /// <c>AddRedisRateLimiter</c> of <c>HsnSoft.Base.RateLimiting.StackExchangeRedis</c> instead.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds the optional <c>RateLimiting</c> section.</param>
    public static IServiceCollection AddInMemoryRateLimiter(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<RateLimitOptions>(configuration.GetSection(RateLimitOptions.SectionName));
        services.TryAddSingleton<IRateLimiter>(provider => new InMemoryRateLimiter(provider.GetService<TimeProvider>()));
        return services;
    }
}
