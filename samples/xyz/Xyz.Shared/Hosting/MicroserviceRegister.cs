using System.Reflection;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.EventBus.Kafka;
using HsnSoft.Base.Localization;
using HsnSoft.Base.MultiTenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Xyz.Shared.Hosting;

/// <summary>The shell of every Xyz microservice: the shared hosting with the bearer token required and a Swagger that can send it. The service adds its own parts (its stores, its application services).</summary>
public static class MicroserviceRegister
{
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds <c>RequestLogging</c>, <c>Swagger</c>, <c>Jwt</c> and <c>ConnectionStrings:Redis</c>.</param>
    /// <param name="swaggerTitle">The title of the service's Swagger document.</param>
    /// <param name="localizedAssemblies">The assemblies of the service that have texts (<c>Localization/en.json</c>, ...): the innermost layer, which wins over the shared one and the framework's.</param>
    public static IServiceCollection AddXyzMicroservice(this IServiceCollection services, IConfiguration configuration, string swaggerTitle, params Assembly[] localizedAssemblies)
        => services
            .AddXyzHosting(configuration, swaggerTitle, authenticateUsers: true, swaggerBearerButton: true)
            .AddBaseLocalization(configure: options => Array.ForEach(localizedAssemblies, assembly => options.AddEmbedded(assembly)))
            .AddXyzRedis(configuration)
            .AddBaseTenantClaims();

    /// <summary>
    /// Connects a service to the Kafka event bus of the system (sections <c>Kafka</c> and <c>Kafka:EventBus</c>: the topic prefix <c>xyz.</c> and the name of the service as consumer (<c>OrderService</c>, ...), which is its consumer group) and makes the
    /// events it publishes carry who started the chain (<see cref="XyzEventHeaderProvider"/>) and in which tenant. The service adds its handlers with <c>AddEventHandler</c>. The auth server uses it as the others do.
    /// </summary>
    public static IServiceCollection AddXyzEventBus(this IServiceCollection services, IConfiguration configuration)
        => services
            .AddHttpContextAccessor()
            .AddSingleton<IEventHeaderProvider, XyzEventHeaderProvider>()
            .AddBaseTenantEventBus()
            .AddKafkaEventBus(configuration);

    /// <summary>The shared middleware (see <see cref="HostingRegister.UseXyzHosting"/>); the service then maps its controllers with <c>MapControllers()</c>.</summary>
    public static WebApplication UseXyzMicroservice(this WebApplication app)
        => app.UseXyzHosting(authenticateUsers: true);
}
