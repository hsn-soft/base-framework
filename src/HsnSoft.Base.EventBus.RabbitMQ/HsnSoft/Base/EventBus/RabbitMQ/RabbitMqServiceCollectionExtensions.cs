using System;
using HsnSoft.Base.EventBus.RabbitMQ.Configs;
using HsnSoft.Base.EventBus.RabbitMQ.Connection;
using HsnSoft.Base.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HsnSoft.Base.EventBus.RabbitMQ;

public static class RabbitMqServiceCollectionExtensions
{
    /// <summary>
    /// Registers the RabbitMQ event bus: the connection, the publisher (<see cref="IEventBus"/>), the failed event notifier, the hosted
    /// service that consumes every handler added with <c>AddEventHandler</c>/<c>AddEventHandlers</c>, and a readiness health check.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds the two sections below.</param>
    /// <param name="connectionSection">How to reach the broker (<see cref="RabbitMqConnectionSettings"/>).</param>
    /// <param name="eventBusSection">Exchange, client names, event name prefix and suffix, retries (<see cref="EventBusOptions"/>) and the RabbitMQ tuning (<see cref="RabbitMqEventBusConfig"/>).</param>
    /// <param name="configureEventBus">Optional code to set <see cref="EventBusOptions"/>; runs after the configuration is read.</param>
    public static IServiceCollection AddRabbitMqEventBus(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionSection = "RabbitMq:Connection",
        string eventBusSection = "RabbitMq:EventBus",
        Action<EventBusOptions>? configureEventBus = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<RabbitMqConnectionSettings>(configuration.GetSection(connectionSection));
        services.AddOptions<EventBusOptions>().Bind(configuration.GetSection(eventBusSection));
        services.AddOptions<RabbitMqEventBusConfig>().Bind(configuration.GetSection(eventBusSection));
        services.AddEventBusCore(configureEventBus);

        services.TryAddSingleton<IRabbitMqPersistentConnection, RabbitMqPersistentConnection>();
        services.TryAddSingleton<IFailedEventNotifier, FailedEventNotifier>();
        services.TryAddSingleton<IEventBus, EventBusRabbitMq>();
        services.AddHostedService<RabbitMqClientLogService>();
        services.AddHostedService<RabbitMqSubscriber>();
        services.AddHealthChecks().AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: [HealthCheckTags.Ready]);
        return services;
    }
}
