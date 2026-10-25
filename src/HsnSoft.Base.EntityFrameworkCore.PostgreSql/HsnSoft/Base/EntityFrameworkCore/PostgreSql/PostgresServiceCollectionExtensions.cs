using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.EntityFrameworkCore.PostgreSql;

public static class PostgresServiceCollectionExtensions
{
    /// <summary>
    /// Registers a PostgreSQL <see cref="DbContext"/> that retries transient failures, with the concepts of the framework (audit fields, soft delete, tenants: see <see cref="BaseDbContext"/>), and a readiness health check.
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

        string connectionString = configuration.GetConnectionString(connectionName)
                               ?? throw new InvalidOperationException($"ConnectionStrings:{connectionName} is required.");

        services.AddBaseEntityFramework();
        services.AddDbContext<TContext>((provider, options) => options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure()).UseBaseEntityConcepts(provider));
        return services.AddDbContextHealthCheck<TContext>("postgres");
    }
}
