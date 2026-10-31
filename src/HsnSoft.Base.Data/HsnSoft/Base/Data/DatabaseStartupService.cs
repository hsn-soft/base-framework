using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace HsnSoft.Base.Data;

/// <summary>A step that prepares a database when the host starts (create its schema, its indexes, seed it): what a service says in its persistence registration, <see cref="DatabaseStartupServiceCollectionExtensions.AddDatabaseStartupStep"/>.</summary>
/// <param name="ConnectionName">The name of the connection string of the database it prepares: it decides which host does it (<c>SeedStartupConnections</c>) and what is locked.</param>
/// <param name="Name">Names the step in the log.</param>
/// <param name="Run">The work, with the services of a scope of its own (the context, the current tenant, the options). Written so that running it again changes nothing.</param>
public sealed record DatabaseStartupStep(string ConnectionName, string Name, Func<IServiceProvider, CancellationToken, Task> Run);

public static class DatabaseStartupServiceCollectionExtensions
{
    /// <summary>
    /// Adds a step that prepares a database when the host starts, so the host has nothing to write in its <c>Program.cs</c>: the steps run in the order they were added, **before any hosted service starts** (the
    /// consumers of the event bus among them: a message must not meet a table that is not there yet), and before the host listens. Each is a database step (<see cref="DatabaseStartupSteps.RunDatabaseStepAsync"/>): only the host
    /// that owns the database runs it (<c>SeedStartupConnections</c>), of its pods one at a time, under the lock of the database. A step that fails, after it was tried again, stops the host from starting.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionName">The name of the connection string of the database the step prepares.</param>
    /// <param name="name">Names the step in the log.</param>
    /// <param name="run">The work: it gets the services of a scope that lives as long as the step.</param>
    public static IServiceCollection AddDatabaseStartupStep(this IServiceCollection services, string connectionName, string name, Func<IServiceProvider, CancellationToken, Task> run)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(run);

        services.AddSingleton(new DatabaseStartupStep(connectionName, name, run));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, DatabaseStartupService>());
        return services;
    }
}

/// <summary>Runs the <see cref="DatabaseStartupStep"/>s while the host is starting: in <see cref="IHostedLifecycleService.StartingAsync"/>, which the host calls on every hosted service before it starts any of them.</summary>
internal sealed class DatabaseStartupService(IServiceProvider services, IEnumerable<DatabaseStartupStep> steps) : IHostedLifecycleService
{
    public async Task StartingAsync(CancellationToken cancellationToken)
    {
        foreach (DatabaseStartupStep step in steps.ToList())
        {
            await services.RunDatabaseStepAsync(step.ConnectionName, step.Name, async stepToken =>
            {
                AsyncServiceScope scope = services.CreateAsyncScope();
                await using (scope.ConfigureAwait(false))
                {
                    await step.Run(scope.ServiceProvider, stepToken).ConfigureAwait(false);
                }
            }).ConfigureAwait(false);
        }
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
