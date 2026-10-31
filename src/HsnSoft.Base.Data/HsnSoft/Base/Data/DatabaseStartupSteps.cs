using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Data;

public static partial class DatabaseStartupSteps
{
    /// <summary>The longest a step waits for the lock of its database (another pod is preparing it); then it is tried again like any step that waits for its dependency.</summary>
    public static readonly TimeSpan DefaultLockTimeout = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Runs a step that prepares a database (create it, its schema, its seed data) the way a host with more than one pod, and a database with more than one host, need it:
    /// <list type="bullet">
    /// <item>only the host that <b>owns</b> the database runs it (<c>SeedStartupConnections</c>, <see cref="StartupDatabaseServiceCollectionExtensions.SeedConnectionsSetting"/>); the others skip it and wait, through their readiness checks, for the owner;</item>
    /// <item>of the pods of the owner only <b>one at a time</b> runs it, under the lock of the database; the next one gets the lock when the first is done and finds the work done (a step is written so that running
    /// it again changes nothing), also when the first pod died (its lock goes with its connection or its lease);</item>
    /// <item>like <see cref="StartupSteps.RunStartupStepAsync"/>, it is tried again while the database is not there yet.</item>
    /// </list>
    /// </summary>
    /// <param name="services">The application services.</param>
    /// <param name="connectionName">The name of the connection string of the database (what <c>AddStartupDatabase</c> got).</param>
    /// <param name="name">Names the step in the log.</param>
    /// <param name="step">The work.</param>
    /// <param name="maxAttempts">After this many failed attempts the last exception is thrown.</param>
    /// <param name="retryDelay">Pause between attempts; defaults to two seconds.</param>
    /// <param name="lockTimeout">How long to wait for the lock in an attempt; defaults to <see cref="DefaultLockTimeout"/>.</param>
    public static async Task RunDatabaseStepAsync(
        this IServiceProvider services,
        string connectionName,
        string name,
        Func<CancellationToken, Task> step,
        int maxAttempts = 15,
        TimeSpan? retryDelay = null,
        TimeSpan? lockTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(step);

        StartupDatabase? database = services.GetServices<StartupDatabase>().FirstOrDefault(known => string.Equals(known.Name, connectionName, StringComparison.OrdinalIgnoreCase));
        if (database is { Owned: false })
        {
            ILogger? logger = services.GetService<ILoggerFactory>()?.CreateLogger(typeof(DatabaseStartupSteps).FullName!);
            if (logger is not null)
            {
                LogNotOwner(logger, name, connectionName);
            }

            return;
        }

        IStartupLock? startupLock = database?.CreateLock(services);
        if (startupLock is null)
        {
            await services.RunStartupStepAsync(name, step, maxAttempts, retryDelay).ConfigureAwait(false);
            return;
        }

        TimeSpan timeout = lockTimeout ?? DefaultLockTimeout;
        await services.RunStartupStepAsync(
            name,
            async cancellationToken =>
            {
                IAsyncDisposable held = await startupLock.AcquireAsync(connectionName, timeout, cancellationToken).ConfigureAwait(false);
                await using (held.ConfigureAwait(false))
                {
                    await step(cancellationToken).ConfigureAwait(false);
                }
            },
            maxAttempts,
            retryDelay).ConfigureAwait(false);
    }

    [LoggerMessage(EventId = 5111, EventName = "STARTUP_NOT_OWNER", Level = LogLevel.Information, Message = "{Step} is left to the host that owns {Database} (SeedStartupConnections)")]
    private static partial void LogNotOwner(ILogger logger, string step, string database);
}
