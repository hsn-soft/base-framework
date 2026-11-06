using System.Reflection;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.EventBus.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hsx.Shared.Hosting.EventBus;

public static class EventBusRegister
{
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
}
