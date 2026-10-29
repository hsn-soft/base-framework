using System;
using HsnSoft.Base.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.AspNetCore;

/// <summary>What every ASP.NET Core host of a system needs, api or not: the health endpoints a platform probes.</summary>
public static class WebApplicationExtensions
{
    /// <summary>Registers the health check services. The infrastructure projects add their own checks to it.</summary>
    public static IServiceCollection AddBaseHealthChecks(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHealthChecks();
        return services;
    }

    /// <summary><c>/health/live</c> (the process runs) and <c>/health/ready</c> (the checks tagged <see cref="HealthCheckTags.Ready"/> pass).</summary>
    public static WebApplication MapBaseHealth(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = registration => registration.Tags.Contains(HealthCheckTags.Ready) });
        return app;
    }
}
