using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xyz.Shared.Events;
using Xyz.Shared.Messaging;

namespace Xyz.Shared.Hosting;

public static partial class ServiceRegister
{
    /// <summary>The RabbitMQ event bus and the background subscriber. Settings: <c>RabbitMq</c> section.</summary>
    public static IServiceCollection AddSharedRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<IEventBus, RabbitMqEventBus>();
        services.AddHostedService<RabbitMqSubscriber>();
        services.AddHealthChecks().AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: ["ready"]);
        return services;
    }

    /// <summary>Subscribes the service to <typeparamref name="TEvent"/>; <typeparamref name="THandler"/> runs in its own scope per message.</summary>
    public static IServiceCollection AddEventHandler<TEvent, THandler>(this IServiceCollection services)
        where TEvent : IntegrationEvent
        where THandler : class, IEventHandler<TEvent>
    {
        services.AddScoped<IEventHandler<TEvent>, THandler>();
        services.AddSingleton(new EventSubscription(
            typeof(TEvent),
            (provider, @event, cancellationToken) => provider.GetRequiredService<IEventHandler<TEvent>>().HandleAsync((TEvent)@event, cancellationToken)));
        return services;
    }
}
