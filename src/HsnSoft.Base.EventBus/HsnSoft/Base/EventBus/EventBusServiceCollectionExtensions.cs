using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HsnSoft.Base.Domain.Entities.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus;

public static class EventBusServiceCollectionExtensions
{
    /// <summary>
    /// Registers what every broker needs: <see cref="EventBusOptions"/>, the subscription manager, the dispatcher and the envelope factory.
    /// Broker projects call it from their own registration method; call it directly only to change the options.
    /// </summary>
    public static IServiceCollection AddEventBusCore(this IServiceCollection services, Action<EventBusOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        OptionsBuilder<EventBusOptions> options = services.AddOptions<EventBusOptions>();
        if (configure is not null)
        {
            options.Configure(configure);
        }

        services.TryAddSingleton<IEventBusSubscriptionManager, InMemoryEventBusSubscriptionManager>();
        services.TryAddSingleton<IEventDispatcher, EventDispatcher>();
        services.TryAddSingleton<EventEnvelopeFactory>();
        return services;
    }

    /// <summary>
    /// Subscribes the service to <typeparamref name="TEvent"/>. <typeparamref name="THandler"/> runs in its own scope per message, so it can
    /// take scoped services (a DbContext) in its constructor. An event has one handler per service: when it is registered twice, the
    /// first registration wins.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="fetchCount">How many messages of this event are handled at the same time.</param>
    public static IServiceCollection AddEventHandler<TEvent, THandler>(this IServiceCollection services, ushort fetchCount = 1)
        where TEvent : IIntegrationEventMessage
        where THandler : class, IIntegrationEventHandler<TEvent>
        => services.AddEventHandler(typeof(TEvent), typeof(THandler), fetchCount);

    /// <inheritdoc cref="AddEventHandler{TEvent,THandler}"/>
    public static IServiceCollection AddEventHandler(this IServiceCollection services, Type eventType, Type handlerType, ushort fetchCount = 1)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(eventType);
        ArgumentNullException.ThrowIfNull(handlerType);

        if (!eventType.IsAssignableTo(typeof(IIntegrationEventMessage)))
        {
            throw new ArgumentException($"{eventType.Name} is not an {nameof(IIntegrationEventMessage)}.", nameof(eventType));
        }

        Type contract = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
        if (!handlerType.IsAssignableTo(contract))
        {
            throw new ArgumentException($"{handlerType.Name} is not an {contract.Name}.", nameof(handlerType));
        }

        services.TryAddTransient(handlerType);
        services.AddSingleton(new EventSubscription(eventType, handlerType, fetchCount));
        return services;
    }

    /// <summary>Registers every handler found in <paramref name="assembly"/> for the event(s) it handles.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">The assembly to scan.</param>
    /// <param name="fetchCounts">How many messages of an event are handled at the same time, by event type name (1 for the others).</param>
    public static IServiceCollection AddEventHandlers(
        this IServiceCollection services,
        Assembly assembly,
        IReadOnlyDictionary<string, ushort>? fetchCounts = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        IEnumerable<Type> handlerTypes = assembly.GetTypes()
            .Where(type => typeof(IIntegrationEventHandler).IsAssignableFrom(type) && type is { IsInterface: false, IsAbstract: false });

        foreach (Type handlerType in handlerTypes)
        {
            IEnumerable<Type> eventTypes = handlerType.GetInterfaces()
                .Where(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>))
                .Select(contract => contract.GenericTypeArguments[0]);

            foreach (Type eventType in eventTypes)
            {
                ushort fetchCount = 1;
                if (fetchCounts is not null && fetchCounts.TryGetValue(eventType.Name, out ushort configured) && configured > 0)
                {
                    fetchCount = configured;
                }

                services.AddEventHandler(eventType, handlerType, fetchCount);
            }
        }

        return services;
    }

    /// <summary>Registers a provider of correlation id and headers for the messages the service publishes (see <see cref="IEventHeaderProvider"/>).</summary>
    public static IServiceCollection AddEventHeaderProvider<TProvider>(this IServiceCollection services)
        where TProvider : class, IEventHeaderProvider
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IEventHeaderProvider, TProvider>();
        return services;
    }
}
