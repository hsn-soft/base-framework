using System;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Data;
using HsnSoft.Base.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HsnSoft.Base.EntityFrameworkCore;

public static class DbContextExtensions
{
    /// <summary>
    /// Registers what a database provider adds for the concepts of the framework (<see cref="BaseDbContext"/>): <c>AddBaseEntityConcepts</c> and the <see cref="BaseEntityInterceptor"/> that
    /// the provider puts on the options of the context with <see cref="UseBaseEntityConcepts"/>, with the interceptors that packages register as <see cref="IInterceptor"/> (the tenant of a new entity, <c>AddBaseMultiTenancyEntityFramework</c>).
    /// </summary>
    public static IServiceCollection AddBaseEntityFramework(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddBaseEntityConcepts();
        services.TryAddSingleton<BaseEntityInterceptor>();
        return services;
    }

    /// <summary>Puts <see cref="BaseEntityInterceptor"/> and the registered <see cref="IInterceptor"/> services on the options of a context (the database provider calls it).</summary>
    public static DbContextOptionsBuilder UseBaseEntityConcepts(this DbContextOptionsBuilder options, IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(services);

        return options.AddInterceptors(services.GetRequiredService<BaseEntityInterceptor>()).AddInterceptors(services.GetServices<IInterceptor>());
    }

    /// <summary>Adds a readiness health check named <paramref name="name"/> that opens a connection through <typeparamref name="TContext"/>. Providers call it from their registration method.</summary>
    public static IServiceCollection AddDbContextHealthCheck<TContext>(this IServiceCollection services, string name)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHealthChecks().AddCheck<DbContextHealthCheck<TContext>>(name, tags: [HealthCheckTags.Ready]);
        return services;
    }

    /// <summary>
    /// A startup step that creates the database schema of <typeparamref name="TContext"/> if it does not exist (<see cref="DatabaseStartupServiceCollectionExtensions.AddDatabaseStartupStep"/>): add it to the persistence of a
    /// service, before the steps that seed it. Use migrations instead for a schema that evolves.
    /// </summary>
    public static IServiceCollection AddSchemaStartupStep<TContext>(this IServiceCollection services, string connectionName)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddDatabaseStartupStep(connectionName, $"{typeof(TContext).Name} schema", async (scope, cancellationToken) =>
            await scope.GetRequiredService<TContext>().Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Creates the database schema of <typeparamref name="TContext"/> if it does not exist, waiting for the database to accept connections. Use migrations instead for a schema that evolves. For a context that was
    /// registered with <c>AddBaseEfCore</c> it is a database step (<see cref="DatabaseStartupSteps.RunDatabaseStepAsync"/>): only the host that owns the database does it (<c>SeedStartupConnections</c>), and the pods of that host
    /// one at a time, under the lock of the database.
    /// </summary>
    public static Task EnsureDatabaseCreatedAsync<TContext>(this IServiceProvider services)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.RunContextStepAsync<TContext>($"{typeof(TContext).Name} schema", async (context, cancellationToken) =>
            await context.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Runs a step that prepares the database of <typeparamref name="TContext"/> (its seed data, ...) with the context in a scope of its own, as a database step: only the host that owns the database does it, the pods of that host
    /// one at a time, and it is tried again while the database is not there. <see cref="EnsureDatabaseCreatedAsync{TContext}"/> is the first of them; the steps of a service that follow it use this one.
    /// </summary>
    public static Task RunContextStepAsync<TContext>(this IServiceProvider services, string name, Func<TContext, CancellationToken, Task> step)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(step);

        return services.RunScopedContextStepAsync<TContext>(name, (scopeServices, cancellationToken) => step(scopeServices.GetRequiredService<TContext>(), cancellationToken));
    }

    /// <summary>
    /// The same as <see cref="RunContextStepAsync{TContext}"/> for a step that needs more of the scope than the context (the current tenant, the options): it gets the services of the scope it runs in, which is
    /// the one of <typeparamref name="TContext"/> and of whatever else it asks for.
    /// </summary>
    public static Task RunScopedContextStepAsync<TContext>(this IServiceProvider services, string name, Func<IServiceProvider, CancellationToken, Task> step)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(step);

        async Task Run(CancellationToken cancellationToken)
        {
            AsyncServiceScope scope = services.CreateAsyncScope();
            await using (scope.ConfigureAwait(false))
            {
                await step(scope.ServiceProvider, cancellationToken).ConfigureAwait(false);
            }
        }

        StartupContext<TContext>? context = services.GetService<StartupContext<TContext>>();
        return context is null
            ? services.RunStartupStepAsync(name, Run)
            : services.RunDatabaseStepAsync(context.ConnectionName, name, Run);
    }
}
