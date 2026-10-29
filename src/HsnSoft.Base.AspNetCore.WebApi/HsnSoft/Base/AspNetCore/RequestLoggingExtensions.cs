using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.AspNetCore;

public static class RequestLoggingExtensions
{
    /// <summary>Reads the <c>RequestLogging</c> section (every setting has a default, the section may be missing).</summary>
    public static IServiceCollection AddBaseRequestLogging(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<RequestLoggingOptions>().Bind(configuration.GetSection(RequestLoggingOptions.SectionName));
        return services;
    }

    /// <summary>
    /// Logs every request and its response, and puts the request in a chain (<see cref="RequestLoggingOptions.CorrelationIdHeader"/>).
    /// Call it before the exception handler, so the response log has the status the client gets, and after routing (the
    /// <c>WebApplication</c> does that by itself) so the endpoint is known.
    /// </summary>
    public static IApplicationBuilder UseBaseRequestLogging(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
}
