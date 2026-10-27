using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.EntityFrameworkCore.MySql;

public static class MySqlServiceCollectionExtensions
{
    /// <summary>
    /// Registers a MySQL <see cref="DbContext"/> that retries transient failures, with the concepts of the framework (audit fields, soft delete: see <see cref="BaseDbContext"/>), and a readiness health check. A service that can run on more than one database uses <c>AddBaseEfCore</c> instead.
    /// </summary>
    /// <typeparam name="TContext">The service's DbContext.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds <c>ConnectionStrings:{connectionName}</c>.</param>
    /// <param name="connectionName">The name of the connection string.</param>
    public static IServiceCollection AddBaseMySql<TContext>(this IServiceCollection services, IConfiguration configuration, string connectionName = "MySql")
        where TContext : DbContext
        => services.AddBaseEfCore<TContext>(configuration, providers => providers.UseMySql(), EfCoreProviderNames.MySql, connectionName);

    /// <summary>Offers MySQL to <c>AddBaseEfCore</c> (selected by <see cref="EfCoreProviderNames.MySql"/>).</summary>
    public static EfCoreProviderOptions UseMySql(this EfCoreProviderOptions providers)
    {
        ArgumentNullException.ThrowIfNull(providers);

        return providers.Add(MySqlProvider.Instance);
    }

    private sealed class MySqlProvider : IEfCoreProvider
    {
        public static readonly MySqlProvider Instance = new();

        public string Name => EfCoreProviderNames.MySql;

        public void Configure(DbContextOptionsBuilder options, string connectionString)
            => options.UseMySQL(connectionString, mysql => mysql.EnableRetryOnFailure());
    }
}
