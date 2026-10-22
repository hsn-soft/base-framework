using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HsnSoft.Base.AspNetCore;

public static class OpenApiExtensions
{
    public static IServiceCollection AddBaseOpenApi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddOpenApi();
    }

    /// <summary>Serves the OpenAPI document (<c>/openapi/v1.json</c>) in the development environment only.</summary>
    public static WebApplication MapBaseOpenApi(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        return app;
    }
}
