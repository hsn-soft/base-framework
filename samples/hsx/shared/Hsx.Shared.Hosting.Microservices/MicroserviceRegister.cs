using System.Reflection;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.EventBus.RabbitMQ;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hsx.Shared.Hosting.Microservices;

/// <summary>The shell of every Hsx microservice: the shared hosting with the bearer token required and a Swagger that can send it. The service adds its own parts (its stores, its application services).</summary>
public static class MicroserviceRegister
{
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds <c>RequestLogging</c>, <c>Swagger</c> and <c>Jwt</c>.</param>
    /// <param name="swaggerTitle">The title of the service's Swagger document.</param>
    /// <remarks>What a service may do without (its texts in several languages, Redis) it chooses itself: <c>AddBaseLocalization</c> and <c>UseBaseLocalization</c>, <c>AddBaseRedis</c>.</remarks>
    public static IServiceCollection AddHsxMicroservice(this IServiceCollection services, IConfiguration configuration, string swaggerTitle)
        => services.AddHsxHosting(configuration, swaggerTitle, swaggerBearerButton: true);

    /// <summary>
    /// Connects a service to the RabbitMQ event bus of the system (sections <c>RabbitMq:Connection</c> and <c>RabbitMq:EventBus</c>: the exchange <c>HSX</c> and the client name of the service (<c>IdentityService</c>, <c>OrderService</c>, ...)) and makes the
    /// events it publishes carry who started the chain (<see cref="HsxEventHeaderProvider"/>). The handlers of the service are every handler of the assembly it names (<paramref name="handlers"/>, found by scanning; none for a service that only publishes) and <paramref name="fetchCounts"/> says how many messages of an event are handled at the same time (by the name of the event; one when it is not named). The auth server does not use it.
    /// </summary>
    public static IServiceCollection AddHsxEventBus(this IServiceCollection services, IConfiguration configuration, Assembly? handlers = null, IReadOnlyDictionary<string, ushort>? fetchCounts = null)
    {
        services
            .AddHttpContextAccessor()
            .AddSingleton<IEventHeaderProvider, HsxEventHeaderProvider>()
            .AddRabbitMqEventBus(configuration);
        return handlers is null ? services : services.AddEventHandlers(handlers, fetchCounts);
    }

    /// <summary>The shared middleware (see <see cref="HostingRegister.UseHsxHosting"/>); the service then maps its controllers with <c>MapControllers()</c>.</summary>
    public static WebApplication UseHsxMicroservice(this WebApplication app)
        => app.UseHsxHosting();
}
