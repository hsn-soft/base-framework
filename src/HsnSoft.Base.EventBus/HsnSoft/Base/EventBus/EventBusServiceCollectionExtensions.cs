using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus;

public static class EventBusServiceCollectionExtensions
{
    /// <summary>
    /// Registers what every broker needs: <see cref="EventBusOptions"/> and the <see cref="IEventDispatcher"/>.
    /// Broker projects call it from their own registration method; call it directly only to change the defaults.
    /// </summary>
    public static IServiceCollection AddEventBusCore(this IServiceCollection services, Action<EventBusOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        OptionsBuilder<EventBusOptions> options = services.AddOptions<EventBusOptions>();
        if (configure is not null)
        {
            options.Configure(configure);
        }

        services.TryAddSingleton<IEventDispatcher, EventDispatcher>();
        return services;
    }

    /// <summary>
    /// Subscribes the service to <typeparamref name="TEvent"/>. <typeparamref name="THandler"/> runs in its own scope per message,
    /// so it can take scoped services (a DbContext) in its constructor.
    /// </summary>
    public static IServiceCollection AddEventHandler<TEvent, THandler>(this IServiceCollection services)
        where TEvent : IntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IIntegrationEventHandler<TEvent>, THandler>();
        services.AddSingleton(new EventSubscription(
            typeof(TEvent),
            (provider, integrationEvent, cancellationToken) =>
                provider.GetRequiredService<IIntegrationEventHandler<TEvent>>().HandleAsync((TEvent)integrationEvent, cancellationToken)));
        return services;
    }
}
