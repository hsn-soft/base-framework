using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Data;

/// <summary>
/// A piece of the data a database starts with (the tenants of a system, the catalogue of permissions, the first administrator, the data of a sample). It runs when the host starts, as a step of the database (see
/// <see cref="DatabaseStartupSteps.RunDatabaseStepAsync"/>): only on the host that owns the database, one pod at a time under the lock of the database, in the order the seeders were added, after the schema. A seeder is made by the
/// container in a scope of its own, so it takes what it needs in its constructor (a context, a repository, the current tenant).
/// </summary>
public interface IDataSeeder
{
    /// <summary>
    /// Writes the data. It is run again by every pod that gets the lock after the first one, and by every start, so it <b>checks what is there</b> and adds only what is not: running it twice changes nothing.
    /// </summary>
    /// <param name="cancellationToken">Cancelled when the host is stopped.</param>
    Task SeedAsync(CancellationToken cancellationToken);
}

public static partial class DataSeederServiceCollectionExtensions
{
    /// <summary>The seeders that are not run, by the name of their class (<c>SampleIdentitySeeder</c>): <c>Seeding:Disabled:0 = SampleIdentitySeeder</c>. An environment that must not have the data of a sample (production) lists them, with no change of code.</summary>
    public const string DisabledSetting = "Seeding:Disabled";

    /// <summary>
    /// Adds a seeder to the start of the database named <paramref name="connectionName"/> (what <c>AddStartupDatabase</c> got). The seeders of a database run in the order they are added, after its schema step
    /// (<c>AddSchemaStartupStep</c>); a seeder named in <c>Seeding:Disabled</c> is skipped.
    /// </summary>
    /// <typeparam name="TSeeder">The seeder; it is registered as a scoped service, so it may take what it needs in its constructor.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionName">The name of the connection string of the database.</param>
    public static IServiceCollection AddDataSeeder<TSeeder>(this IServiceCollection services, string connectionName)
        where TSeeder : class, IDataSeeder
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);

        string name = typeof(TSeeder).Name;
        services.AddScoped<TSeeder>();
        return services.AddDatabaseStartupStep(connectionName, name, async (scope, cancellationToken) =>
        {
            IEnumerable<string?> disabledNames = scope.GetService<IConfiguration>()?.GetSection(DisabledSetting).GetChildren().Select(child => child.Value) ?? [];
            if (disabledNames.Any(disabled => string.Equals(disabled?.Trim(), name, StringComparison.OrdinalIgnoreCase)))
            {
                ILogger? logger = scope.GetService<ILoggerFactory>()?.CreateLogger(typeof(DataSeederServiceCollectionExtensions).FullName!);
                if (logger is not null)
                {
                    LogDisabled(logger, name);
                }

                return;
            }

            await scope.GetRequiredService<TSeeder>().SeedAsync(cancellationToken).ConfigureAwait(false);
        });
    }

    [LoggerMessage(EventId = 5112, EventName = "SEEDER_DISABLED", Level = LogLevel.Information, Message = "{Seeder} is not run (Seeding:Disabled)")]
    private static partial void LogDisabled(ILogger logger, string seeder);
}
