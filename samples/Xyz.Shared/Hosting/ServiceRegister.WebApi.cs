using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Xyz.Shared.Hosting;

/// <summary>
/// The service registrations every Xyz service repeats, written once. Each concern (web api, redis, rabbitmq, database,
/// jwt) is one extension method so a service picks exactly what it needs in its Program.cs.
/// </summary>
public static partial class ServiceRegister
{
    /// <summary>Problem-details error responses, global exception handler, enum-as-string JSON, OpenAPI and health checks.</summary>
    public static IServiceCollection AddSharedWebApi(this IServiceCollection services, string serviceName)
    {
        services.Configure<ServiceOptions>(options => options.Name = serviceName);
        services.AddOpenApi();
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.Configure<JsonOptions>(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.AddHealthChecks();
        return services;
    }
}
