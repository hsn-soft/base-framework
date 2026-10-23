using System;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.AspNetCore;

public static class WebApiServiceCollectionExtensions
{
    /// <summary>
    /// What every web api of a system repeats: controllers, problem-details error responses, the <see cref="GlobalExceptionHandler"/>
    /// and enums as strings in JSON (for controllers and for minimal APIs). Map the controllers with <c>app.MapControllers()</c>.
    /// </summary>
    public static IServiceCollection AddBaseWebApi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.Configure<JsonOptions>(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        return services;
    }

    /// <summary>Turns unhandled exceptions into problem responses and gives bodyless status codes (404, 405, ...) a problem body.</summary>
    public static IApplicationBuilder UseBaseExceptionHandling(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseExceptionHandler();
        app.UseStatusCodePages();
        return app;
    }
}
