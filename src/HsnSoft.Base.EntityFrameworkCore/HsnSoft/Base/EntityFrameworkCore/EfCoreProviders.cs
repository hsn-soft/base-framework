using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.EntityFrameworkCore;

/// <summary>A relational database the service can run on: what a provider package (<c>EntityFrameworkCore.PostgreSql</c>, <c>EntityFrameworkCore.SqlServer</c>, ...) says about itself and how it configures a context.</summary>
public interface IEfCoreProvider
{
    /// <summary>The name that selects it in <c>Database:Provider</c> (<c>postgres</c>, <c>sqlserver</c>); also the name of its readiness check.</summary>
    string Name { get; }

    /// <summary>The name of its connection string (<c>ConnectionStrings:Postgres</c>), when the call does not give another.</summary>
    string ConnectionName { get; }

    void Configure(DbContextOptionsBuilder options, string connectionString);
}

/// <summary>The providers a service offers; the setting <c>Database:Provider</c> says which one runs (<see cref="EfCoreProviderServiceCollectionExtensions.AddBaseEfCore"/>).</summary>
public sealed class EfCoreProviderOptions
{
    private readonly List<IEfCoreProvider> _providers = [];

    public IReadOnlyList<IEfCoreProvider> Providers => _providers;

    /// <summary>Adds a provider, with the name of its connection string when it is not the provider's own. A provider package calls it from its <c>Use...</c> method.</summary>
    public EfCoreProviderOptions Add(IEfCoreProvider provider, string? connectionName = null)
    {
        ArgumentNullException.ThrowIfNull(provider);

        _providers.RemoveAll(existing => string.Equals(existing.Name, provider.Name, StringComparison.OrdinalIgnoreCase));
        _providers.Add(connectionName is null ? provider : new Renamed(provider, connectionName));
        return this;
    }

    private sealed class Renamed(IEfCoreProvider inner, string connectionName) : IEfCoreProvider
    {
        public string Name => inner.Name;

        public string ConnectionName => connectionName;

        public void Configure(DbContextOptionsBuilder options, string connectionString) => inner.Configure(options, connectionString);
    }
}

public static class EfCoreProviderServiceCollectionExtensions
{
    /// <summary>The setting that says which provider runs.</summary>
    public const string ProviderSetting = "Database:Provider";

    /// <summary>
    /// Registers the <typeparamref name="TContext"/> of a service on the relational database that the setting <c>Database:Provider</c> names, among the providers the service offers (<c>providers.UsePostgres().UseSqlServer()</c>): it retries
    /// transient failures, has the concepts of the framework (<see cref="BaseDbContext"/>) and a readiness check. <b>Moving to another provider is a setting and its connection string</b>; the code of the domain and the application does not change.
    /// With one provider offered the setting may be left out.
    /// </summary>
    public static IServiceCollection AddBaseEfCore<TContext>(this IServiceCollection services, IConfiguration configuration, Action<EfCoreProviderOptions> providers)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(providers);

        EfCoreProviderOptions options = new();
        providers(options);
        IEfCoreProvider provider = Select(options.Providers, configuration[ProviderSetting]);
        string connectionString = configuration.GetConnectionString(provider.ConnectionName)
                               ?? throw new InvalidOperationException($"ConnectionStrings:{provider.ConnectionName} is required (the provider is {provider.Name}).");

        services.AddBaseEntityFramework();
        services.AddDbContext<TContext>((serviceProvider, builder) =>
        {
            provider.Configure(builder, connectionString);
            builder.UseBaseEntityConcepts(serviceProvider);
        });
        return services.AddDbContextHealthCheck<TContext>(provider.Name);
    }

    internal static IEfCoreProvider Select(IReadOnlyList<IEfCoreProvider> providers, string? setting)
    {
        string offered = string.Join(", ", providers.Select(provider => provider.Name));
        if (providers.Count == 0)
        {
            throw new InvalidOperationException("No database provider is offered: add one (providers.UsePostgres(), providers.UseSqlServer(), ...).");
        }

        if (string.IsNullOrWhiteSpace(setting))
        {
            return providers.Count == 1
                ? providers[0]
                : throw new InvalidOperationException($"{ProviderSetting} is required: the service offers {offered}.");
        }

        return providers.FirstOrDefault(provider => string.Equals(provider.Name, setting.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"{ProviderSetting} is '{setting}', but the service offers {offered}.");
    }
}
