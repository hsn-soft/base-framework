using System;
using System.Threading.Tasks;
using HsnSoft.Base.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.EntityFrameworkCore.PostgreSql;

public static class PostgresServiceCollectionExtensions
{
    /// <summary>
    /// Registers a PostgreSQL <see cref="DbContext"/> that retries transient failures, and a readiness health check.
    /// </summary>
    /// <typeparam name="TContext">The service's DbContext.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds <c>ConnectionStrings:{connectionName}</c>.</param>
    /// <param name="connectionName">The name of the connection string.</param>
    public static IServiceCollection AddBasePostgres<TContext>(this IServiceCollection services, IConfiguration configuration, string connectionName = "Postgres")
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(connectionName)
                               ?? throw new InvalidOperationException($"ConnectionStrings:{connectionName} is required.");

        services.AddDbContext<TContext>(options => options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure()));
        services.AddHealthChecks().AddCheck<DbContextHealthCheck<TContext>>("postgres", tags: [HealthCheckTags.Ready]);
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
