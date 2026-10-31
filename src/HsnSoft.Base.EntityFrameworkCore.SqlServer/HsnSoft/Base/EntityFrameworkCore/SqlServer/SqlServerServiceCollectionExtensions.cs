using System;
using HsnSoft.Base.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.EntityFrameworkCore.SqlServer;

public static class SqlServerServiceCollectionExtensions
{
    /// <summary>
    /// Registers a SQL Server <see cref="DbContext"/> that retries transient failures, with the concepts of the framework (audit fields, soft delete, tenants: see <see cref="BaseDbContext"/>), and a readiness health check.
    /// </summary>
    /// <typeparam name="TContext">The service's DbContext.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds <c>ConnectionStrings:{connectionName}</c>.</param>
    /// <param name="connectionName">The name of the connection string.</param>
    public static IServiceCollection AddBaseSqlServer<TContext>(this IServiceCollection services, IConfiguration configuration, string connectionName = "SqlServer")
        where TContext : DbContext
        => services.AddBaseEfCore<TContext>(configuration, providers => providers.UseSqlServer(), EfCoreProviderNames.SqlServer, connectionName);

    /// <summary>Offers SQL Server to <c>AddBaseEfCore</c> (selected by <see cref="EfCoreProviderNames.SqlServer"/>).</summary>
    public static EfCoreProviderOptions UseSqlServer(this EfCoreProviderOptions providers)
    {
        ArgumentNullException.ThrowIfNull(providers);

        return providers.Add(SqlServerProvider.Instance);
    }

    private sealed class SqlServerProvider : IEfCoreProvider, IStartupLockProvider
    {
        public static readonly SqlServerProvider Instance = new();

        public string Name => EfCoreProviderNames.SqlServer;

        public void Configure(DbContextOptionsBuilder options, string connectionString)
            => options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());

        public IStartupLock CreateStartupLock(string connectionString) => new SqlServerStartupLock(connectionString);
    }
}
