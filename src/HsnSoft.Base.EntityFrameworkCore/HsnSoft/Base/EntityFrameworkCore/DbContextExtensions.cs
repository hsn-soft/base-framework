using System;
using System.Threading.Tasks;
using HsnSoft.Base.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HsnSoft.Base.EntityFrameworkCore;

public static class DbContextExtensions
{
    /// <summary>
    /// Registers what a database provider adds for the concepts of the framework (<see cref="BaseDbContext"/>): <c>AddBaseEntityConcepts</c> and the <see cref="BaseEntityInterceptor"/> that
    /// the provider puts on the options of the context with <see cref="UseBaseEntityConcepts"/>.
    /// </summary>
    public static IServiceCollection AddBaseEntityFramework(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddBaseEntityConcepts();
        services.TryAddSingleton<BaseEntityInterceptor>();
        return services;
    }

    /// <summary>Puts <see cref="BaseEntityInterceptor"/> on the options of a context (the database provider calls it).</summary>
    public static DbContextOptionsBuilder UseBaseEntityConcepts(this DbContextOptionsBuilder options, IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(services);

        return options.AddInterceptors(services.GetRequiredService<BaseEntityInterceptor>());
    }

    /// <summary>Adds a readiness health check named <paramref name="name"/> that opens a connection through <typeparamref name="TContext"/>. Providers call it from their registration method.</summary>
    public static IServiceCollection AddDbContextHealthCheck<TContext>(this IServiceCollection services, string name)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHealthChecks().AddCheck<DbContextHealthCheck<TContext>>(name, tags: [HealthCheckTags.Ready]);
        return services;
    }

    /// <summary>
    /// Creates the database schema of <typeparamref name="TContext"/> if it does not exist, waiting for the database to accept
    /// connections. Use migrations instead for a schema that evolves.
    /// </summary>
    public static Task EnsureDatabaseCreatedAsync<TContext>(this IServiceProvider services)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.RunStartupStepAsync($"{typeof(TContext).Name} schema", async cancellationToken =>
        {
            AsyncServiceScope scope = services.CreateAsyncScope();
            await using (scope.ConfigureAwait(false))
            {
                await scope.ServiceProvider.GetRequiredService<TContext>().Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
            }
        });
    }
}
