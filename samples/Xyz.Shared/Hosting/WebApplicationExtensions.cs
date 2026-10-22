using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Xyz.Shared.Hosting;

/// <summary>The middleware side of <see cref="ServiceRegister"/>: the same pipeline in every service.</summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Exception handling, OpenAPI document (development only), authentication and authorization.
    /// <paramref name="afterAuthentication"/> adds middleware that needs the user but must run before authorization.
    /// </summary>
    public static WebApplication UseSharedPipeline(this WebApplication app, Action<WebApplication>? afterAuthentication = null)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseAuthentication();
        afterAuthentication?.Invoke(app);
        app.UseAuthorization();
        return app;
    }

    /// <summary><c>/health/live</c> (the process runs) and <c>/health/ready</c> (its dependencies answer).</summary>
    public static WebApplication MapSharedHealth(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = registration => registration.Tags.Contains("ready") });
        return app;
    }

    /// <summary>
    /// Runs a startup step (create schema, create indexes, seed) and retries while the dependency it talks to is still starting,
    /// so the services can be started in any order.
    /// </summary>
    public static async Task RunStartupStepAsync(this WebApplication app, string name, Func<CancellationToken, Task> step)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
        const int maxAttempts = 15;

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await step(CancellationToken.None);
                return;
            }
            catch (Exception exception) when (attempt < maxAttempts)
            {
                logger.LogWarning("{Step} is waiting for its dependency (attempt {Attempt}/{Max}): {Reason}", name, attempt, maxAttempts, exception.Message);
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }
    }

    /// <summary>Creates the database schema, waiting for the database to accept connections.</summary>
    public static async Task EnsureDatabaseCreatedAsync<TContext>(this WebApplication app)
        where TContext : DbContext
        => await app.RunStartupStepAsync($"{typeof(TContext).Name} schema", async cancellationToken =>
        {
            await using var scope = app.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<TContext>().Database.EnsureCreatedAsync(cancellationToken);
        });
}
