using System;
using System.Collections.Generic;
using System.Linq;
using HsnSoft.Base.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.EntityFrameworkCore;

/// <summary>The names of the providers of the framework: what the <c>Provider</c> of a context's properties class says.</summary>
public static class EfCoreProviderNames
{
    public const string MySql = "mysql";

    public const string Postgres = "postgres";

    public const string SqlServer = "sqlserver";
}

/// <summary>A relational database a service can run a context on: what a provider package (<c>EntityFrameworkCore.PostgreSql</c>, <c>EntityFrameworkCore.SqlServer</c>, ...) says about itself and how it configures a context.</summary>
public interface IEfCoreProvider
{
    /// <summary>The name that selects it (<see cref="EfCoreProviderNames"/>); also the name of its readiness check.</summary>
    string Name { get; }

    void Configure(DbContextOptionsBuilder options, string connectionString);

    /// <summary>Configures the context and says how long it waits and how often it retries (<see cref="EfCoreOptions"/>). A provider that does not know them configures the context as before.</summary>
    void Configure(DbContextOptionsBuilder options, string connectionString, EfCoreOptions tuning) => Configure(options, connectionString);
}

/// <summary>What a provider offers when it can lock its server for the startup steps of a database (<see cref="IStartupLock"/>): the three providers of the framework do.</summary>
public interface IStartupLockProvider
{
    IStartupLock CreateStartupLock(string connectionString);
}

/// <summary>The name of the connection string of the database of a context: what the startup steps of the context (<c>EnsureDatabaseCreatedAsync</c>) lock and ask the ownership of.</summary>
public sealed record StartupContext<TContext>(string ConnectionName)
    where TContext : DbContext;

/// <summary>The providers a service offers; the provider of a context is named by the code of the service (<c>AddBaseEfCore</c>).</summary>
public sealed class EfCoreProviderOptions
{
    private readonly List<IEfCoreProvider> _providers = [];

    public IReadOnlyList<IEfCoreProvider> Providers => _providers;

    /// <summary>Adds a provider; a provider package calls it from its <c>Use...</c> method. One of the same name is replaced.</summary>
    public EfCoreProviderOptions Add(IEfCoreProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        _providers.RemoveAll(existing => string.Equals(existing.Name, provider.Name, StringComparison.OrdinalIgnoreCase));
        _providers.Add(provider);
        return this;
    }
}

public static class EfCoreProviderServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <typeparamref name="TContext"/> of a service on one relational database: the provider (<paramref name="provider"/>, one of <see cref="EfCoreProviderNames"/>, among the ones the service offers with
    /// <paramref name="providers"/>, <c>providers.UsePostgres().UseSqlServer()</c>) and the name of its connection string (<paramref name="connectionName"/>) are said by the code of the service, as properties of the context
    /// (<c>EfCoreIdentityDbProperties.Provider</c>, <c>EfCoreIdentityDbProperties.ConnectionStringName</c>), not by a setting: a service with more than one context says for each which provider and where. The context retries transient
    /// failures, has the concepts of the framework (<see cref="BaseDbContext"/>) and a readiness check named after the provider. The code of the domain and the application does not depend on the provider.
    /// </summary>
    public static IServiceCollection AddBaseEfCore<TContext>(this IServiceCollection services, IConfiguration configuration, Action<EfCoreProviderOptions> providers, string provider, string connectionName)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);

        EfCoreProviderOptions options = new();
        providers(options);
        IEfCoreProvider selected = Select(options.Providers, provider);
        string connectionString = configuration.GetConnectionString(connectionName)
                               ?? throw new InvalidOperationException($"ConnectionStrings:{connectionName} is required (the provider is {selected.Name}).");

        EfCoreOptions tuning = configuration.GetSection(EfCoreOptions.SectionName).Get<EfCoreOptions>() ?? new EfCoreOptions();

        services.AddBaseEntityFramework();
        // who prepares this database (SeedStartupConnections) and how it is locked while the pods of the owner do it
        services.AddStartupDatabase(configuration, connectionName, _ => (selected as IStartupLockProvider)?.CreateStartupLock(connectionString));
        services.AddSingleton(new StartupContext<TContext>(connectionName));
        services.AddDbContext<TContext>((serviceProvider, builder) =>
        {
            selected.Configure(builder, connectionString, tuning);
            builder.UseBaseEntityConcepts(serviceProvider);
        });

        // a second context on the same provider cannot take the name of the first one: its check is named after the context too
        bool nameTaken = services.Any(descriptor => descriptor.ServiceType == typeof(HealthCheckName) && descriptor.ImplementationInstance is HealthCheckName taken && taken.Value == selected.Name);
        services.AddSingleton(new HealthCheckName(selected.Name));
        return services.AddDbContextHealthCheck<TContext>(nameTaken ? $"{selected.Name}-{typeof(TContext).Name}" : selected.Name);
    }

    private sealed record HealthCheckName(string Value);

    private static IEfCoreProvider Select(IReadOnlyList<IEfCoreProvider> providers, string name)
    {
        if (providers.Count == 0)
        {
            throw new InvalidOperationException("No database provider is offered: add one (providers.UsePostgres(), providers.UseSqlServer(), ...).");
        }

        return providers.FirstOrDefault(provider => string.Equals(provider.Name, name.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"The provider is '{name}', but the service offers {string.Join(", ", providers.Select(provider => provider.Name))}.");
    }
}
