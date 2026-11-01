using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.AspNetCore;

/// <summary>What every ASP.NET Core host of a system needs, api or not: the health endpoints a platform probes.</summary>
public static class WebApplicationExtensions
{
    private static readonly JsonSerializerOptions s_json = new(JsonSerializerDefaults.Web) { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    /// <summary>
    /// Registers the health check services. The infrastructure projects add their own checks to it, and every check that has no timeout of its own (a registration has none, no limit at all, unless its owner gives one) gets
    /// <see cref="BaseHealthOptions.CheckTimeout"/>, so a dependency that hangs cannot hold a probe.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Optional; holds the section <c>Health</c> (<see cref="BaseHealthOptions"/>).</param>
    public static IServiceCollection AddBaseHealthChecks(this IServiceCollection services, IConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHealthChecks();
        OptionsBuilder<BaseHealthOptions> options = services.AddOptions<BaseHealthOptions>();
        if (configuration is not null)
        {
            options.Bind(configuration.GetSection(BaseHealthOptions.SectionName));
        }

        services.AddOptions<HealthCheckServiceOptions>().PostConfigure<IOptions<BaseHealthOptions>>((health, own) =>
        {
            foreach (HealthCheckRegistration disabled in health.Registrations.Where(registration => own.Value.Disabled.Contains(registration.Name, StringComparer.OrdinalIgnoreCase)).ToList())
            {
                health.Registrations.Remove(disabled);
            }

            foreach (HealthCheckRegistration registration in health.Registrations.Where(registration => registration.Timeout == Timeout.InfiniteTimeSpan))
            {
                registration.Timeout = own.Value.CheckTimeout;
            }
        });
        return services;
    }

    /// <summary>
    /// A check of the service itself that decides, with the checks of the packages, when the service is ready (an API it cannot work without, a file share, ...). It is tagged
    /// <see cref="HealthCheckTags.Ready"/>, so the readiness and the startup probe run it; it has a name, so <see cref="BaseHealthOptions.Disabled"/> can switch it off.
    /// </summary>
    /// <typeparam name="TCheck">The check; it is created by the container, so it may take what it needs in its constructor.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The name of the check, in the answer of the endpoints.</param>
    public static IServiceCollection AddBaseReadyCheck<TCheck>(this IServiceCollection services, string name)
        where TCheck : class, IHealthCheck
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        services.AddHealthChecks().AddCheck<TCheck>(name, tags: [HealthCheckTags.Ready]);
        return services;
    }

    /// <summary>
    /// <c>/health/live</c> (the process runs), <c>/health/startup</c> and <c>/health/ready</c> (the checks tagged <see cref="HealthCheckTags.Ready"/> pass). The startup probe of a platform asks the same
    /// question as the readiness probe, but only until the service has started: it is what gives a slow start the time it needs without loosening the readiness probe. The answer is a JSON
    /// document with the status and, for every check, its name, status, description and duration.
    /// </summary>
    public static WebApplication MapBaseHealth(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false, ResponseWriter = WriteAsync });
        app.MapHealthChecks("/health/startup", new HealthCheckOptions { Predicate = registration => registration.Tags.Contains(HealthCheckTags.Ready), ResponseWriter = WriteAsync });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = registration => registration.Tags.Contains(HealthCheckTags.Ready), ResponseWriter = WriteAsync });
        return app;
    }

    // a check that throws gets the message of its exception as its description from the health check service: it has the address of the server, so it is only shown when the setting says so
    private static string? Describe(HealthReportEntry entry, bool includeErrors)
        => !includeErrors && entry.Exception is not null && entry.Description == entry.Exception.Message ? "the check failed" : entry.Description;

    private static Task WriteAsync(HttpContext context, HealthReport report)
    {
        bool includeErrors = context.RequestServices.GetService<IOptions<BaseHealthOptions>>()?.Value.IncludeErrors ?? false;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(
            new
            {
                status = report.Status.ToString(),
                durationMs = (long)report.TotalDuration.TotalMilliseconds,
                checks = report.Entries.Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    description = Describe(entry.Value, includeErrors),
                    durationMs = (long)entry.Value.Duration.TotalMilliseconds,
                    error = includeErrors ? entry.Value.Exception?.Message : null,
                }),
            },
            s_json));
    }
}
