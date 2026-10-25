using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.EntityFrameworkCore.PostgreSql;

public static class PostgresServiceCollectionExtensions
{
    /// <summary>
    /// Registers a PostgreSQL <see cref="DbContext"/> that retries transient failures, with the concepts of the framework (audit fields, soft delete, tenants: see <see cref="BaseDbContext"/>), and a readiness health check. A service that can run on more than one database uses <c>AddBaseEfCore</c> instead.
    /// </summary>
    /// <typeparam name="TContext">The service's DbContext.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds <c>ConnectionStrings:{connectionName}</c>.</param>
    /// <param name="connectionName">The name of the connection string.</param>
    public static IServiceCollection AddBasePostgres<TContext>(this IServiceCollection services, IConfiguration configuration, string connectionName = "Postgres")
        where TContext : DbContext
        => services.AddBaseEfCore<TContext>(configuration, providers => providers.UsePostgres(), EfCoreProviderNames.Postgres, connectionName);

    /// <summary>Offers PostgreSQL to <c>AddBaseEfCore</c> (selected by <see cref="EfCoreProviderNames.Postgres"/>).</summary>
    public static EfCoreProviderOptions UsePostgres(this EfCoreProviderOptions providers)
    {
        ArgumentNullException.ThrowIfNull(providers);

        return providers.Add(PostgresProvider.Instance);
    }

    private sealed class PostgresProvider : IEfCoreProvider
    {
        public static readonly PostgresProvider Instance = new();

        public string Name => EfCoreProviderNames.Postgres;

        public void Configure(DbContextOptionsBuilder options, string connectionString)
            => options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure());
    }
}
