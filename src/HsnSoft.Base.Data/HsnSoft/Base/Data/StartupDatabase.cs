using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Data;

/// <summary>
/// A database of the service, as startup sees it: its name (the name of its connection string), whether this host prepares it (<see cref="Owned"/>), and how to lock it. The persistence registrations
/// (<c>AddBaseEfCore</c>, <c>AddBaseMongo</c>) add one for each database.
/// </summary>
public sealed record StartupDatabase(string Name, bool Owned, Func<IServiceProvider, IStartupLock?> CreateLock);

public static class StartupDatabaseServiceCollectionExtensions
{
    /// <summary>The setting that says which databases a host prepares (create, schema, seed data): the names of their connection strings, separated by semicolons. Absent: all the ones it has. Empty: none.</summary>
    public const string SeedConnectionsSetting = "SeedStartupConnections";

    /// <summary>
    /// Adds a database the service has to startup. <paramref name="configuration"/> decides whether this host owns it (<see cref="SeedConnectionsSetting"/>): a database that more than one host uses is prepared by one of
    /// them, and the others only wait for it (their readiness checks tell when it is there).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds <c>SeedStartupConnections</c>.</param>
    /// <param name="connectionName">The name of the connection string of the database.</param>
    /// <param name="createLock">Makes the lock of the database; null when the database has none (the step is then only retried, not locked).</param>
    public static IServiceCollection AddStartupDatabase(this IServiceCollection services, IConfiguration configuration, string connectionName, Func<IServiceProvider, IStartupLock?> createLock)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);
        ArgumentNullException.ThrowIfNull(createLock);

        services.AddSingleton(new StartupDatabase(connectionName, Owns(configuration, connectionName), createLock));
        return services;
    }

    internal static bool Owns(IConfiguration configuration, string connectionName)
    {
        string? owns = configuration[SeedConnectionsSetting];
        return owns is null
            || owns.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Any(name => string.Equals(name, connectionName, StringComparison.OrdinalIgnoreCase));
    }
}
