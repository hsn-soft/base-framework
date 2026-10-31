using Edx.Shared.Helper;
using HsnSoft.Base.Authorization;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.EventBus.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Edx.Shared.Hosting.Microservices;

/// <summary>The shell of every Edx microservice: the shared hosting with the bearer token required and a Swagger that can send it. The service adds its own parts (its stores, its application services).</summary>
public static class MicroserviceRegister
{
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds <c>RequestLogging</c>, <c>Swagger</c> and <c>Jwt</c>.</param>
    /// <param name="swaggerTitle">The title of the service's Swagger document.</param>
    /// <remarks>
    /// What a service may do without (Redis) it chooses itself: <c>AddBaseRedis</c>. A service a browser calls <b>directly</b> (the two integrations are not behind the gateway, which is what lets the back office call the others) says which origins may in
    /// <c>Cors:AllowedOrigins</c>; without it no origin is allowed.
    /// </remarks>
    public static IServiceCollection AddEdxMicroservice(this IServiceCollection services, IConfiguration configuration, string swaggerTitle)
    {
        string[] origins = AllowedOrigins(configuration);
        if (origins.Length > 0)
        {
            services.AddCors(cors => cors.AddPolicy(CorsPolicyName, policy => policy
                .WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .WithExposedHeaders("X-Correlation-Id", "Content-Disposition")));
        }

        return services.AddEdxHosting(configuration, swaggerTitle, swaggerBearerButton: true);
    }

    /// <summary>
    /// Connects a service to the Kafka event bus of the system (sections <c>Kafka</c> and <c>Kafka:EventBus</c>: the exchange <c>EDX</c>, which names the topics, and the client name of the service (<c>IntegrationFirmInvoiceApi</c>, <c>IntegrationGibInvoiceApi</c>, <c>InvoiceService</c>, <c>SignerService</c>: an integration's is named by what it integrates and the host that runs it)) and makes the
    /// events it publishes carry who started the chain (<see cref="EdxEventHeaderProvider"/>). The service adds its handlers with <c>AddEventHandler</c>. The auth server and the gateway do not use it: only the microservices reach Kafka.
    /// </summary>
    public static IServiceCollection AddEdxEventBus(this IServiceCollection services, IConfiguration configuration)
        => services
            .AddHttpContextAccessor()
            .AddSingleton<IEventHeaderProvider, EdxEventHeaderProvider>()
            .AddKafkaEventBus(configuration);

    /// <summary>
    /// Turns on <c>[PermissionAuthorize]</c> in the service: what the caller (the user, and its roles) was granted is read from the store the identity service writes (section <c>Permissions</c>; the host has Redis, <c>AddBaseRedis</c>), under the scope of the tenant of the
    /// request, because the roles of a tenant are its own (<see cref="EdxPermissionScopes"/>). The tenant is the one <c>AddBaseTenantClaims</c> gives the host.
    /// </summary>
    public static IServiceCollection AddEdxPermissions(this IServiceCollection services, IConfiguration configuration)
        => services
            .AddBasePermissions(configuration)
            .AddPermissionScope<TenantPermissionScope>();

    /// <summary>The shared middleware (see <see cref="HostingRegister.UseEdxHosting"/>); the service then maps its endpoints (<c>MapControllers()</c>, and what else it speaks).</summary>
    public static WebApplication UseEdxMicroservice(this WebApplication app)
    {
        if (AllowedOrigins(app.Configuration).Length > 0)
        {
            app.UseCors(CorsPolicyName);
        }

        return app.UseEdxHosting();
    }

    private const string CorsPolicyName = "edx-microservice";

    private static string[] AllowedOrigins(IConfiguration configuration) => configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
}
