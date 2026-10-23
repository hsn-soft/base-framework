using System;
using System.Threading.Tasks;
using HsnSoft.Base.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.EntityFrameworkCore;

public static class DbContextExtensions
{
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
            var scope = services.CreateAsyncScope();
            await using (scope.ConfigureAwait(false))
            {
                await scope.ServiceProvider.GetRequiredService<TContext>().Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
            }
        });
    }
}
