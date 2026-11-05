using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HsnSoft.Base.Concurrency;

public static class ConcurrencyServiceCollectionExtensions
{
    /// <summary>
    /// Registers the limits of the settings (<see cref="ConcurrencyOptions"/>) and the limiter that counts in the memory of one process. A cluster registers <c>AddRedisConcurrencyLimiter</c> of <c>HsnSoft.Base.Concurrency.StackExchangeRedis</c> instead.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds the optional <c>Concurrency</c> section.</param>
    public static IServiceCollection AddInMemoryConcurrencyLimiter(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<ConcurrencyOptions>(configuration.GetSection(ConcurrencyOptions.SectionName));
        services.TryAddSingleton<IConcurrencyLimiter>(_ => new InMemoryConcurrencyLimiter());
        return services;
    }
}
