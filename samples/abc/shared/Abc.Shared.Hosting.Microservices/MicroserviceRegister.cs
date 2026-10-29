using HsnSoft.Base.EventBus;
using HsnSoft.Base.EventBus.RabbitMQ;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Abc.Shared.Hosting.Microservices;

/// <summary>The shell of every Abc microservice: the shared hosting with the bearer token required and a Swagger that can send it. The service adds its own parts (its stores, its application services).</summary>
public static class MicroserviceRegister
{
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds <c>RequestLogging</c>, <c>Swagger</c> and <c>Jwt</c>.</param>
    /// <param name="swaggerTitle">The title of the service's Swagger document.</param>
    /// <remarks>What a service may do without (its texts in several languages, Redis) it chooses itself: <c>AddBaseLocalization</c> and <c>UseBaseLocalization</c>, <c>AddBaseRedis</c>.</remarks>
    public static IServiceCollection AddAbcMicroservice(this IServiceCollection services, IConfiguration configuration, string swaggerTitle)
        => services.AddAbcHosting(configuration, swaggerTitle, authenticateUsers: true, swaggerBearerButton: true);

    /// <summary>
    /// Connects a service to the RabbitMQ event bus of the system (sections <c>RabbitMq:Connection</c> and <c>RabbitMq:EventBus</c>: the exchange <c>ABC</c> and the client name of the service (<c>IdentityService</c>, <c>OrderService</c>, ...)) and makes the
    /// events it publishes carry who started the chain (<see cref="AbcEventHeaderProvider"/>). The service adds its handlers with <c>AddEventHandler</c>. The auth server does not use it.
    /// </summary>
    public static IServiceCollection AddAbcEventBus(this IServiceCollection services, IConfiguration configuration)
        => services
            .AddHttpContextAccessor()
            .AddSingleton<IEventHeaderProvider, AbcEventHeaderProvider>()
            .AddRabbitMqEventBus(configuration);

    /// <summary>The shared middleware (see <see cref="HostingRegister.UseAbcHosting"/>); the service then maps its controllers with <c>MapControllers()</c>.</summary>
    public static WebApplication UseAbcMicroservice(this WebApplication app)
        => app.UseAbcHosting(authenticateUsers: true);
}
