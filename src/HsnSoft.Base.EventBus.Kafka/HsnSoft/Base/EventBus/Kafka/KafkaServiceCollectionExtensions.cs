using System;
using HsnSoft.Base.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HsnSoft.Base.EventBus.Kafka;

public static class KafkaServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Kafka <see cref="IEventBus"/>, the background subscriber for every handler added with
    /// <c>AddEventHandler</c>, and a readiness health check.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds the <c>Kafka</c> section (<see cref="KafkaOptions"/>) and the <c>Kafka:EventBus</c> section (<see cref="EventBusOptions"/>; the consumer group of an event is its <c>ConsumerClientInfo</c> and the event name).</param>
    /// <param name="configureEventBus">Optional code to set <see cref="EventBusOptions"/>; runs after the configuration is read.</param>
    public static IServiceCollection AddKafkaEventBus(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<EventBusOptions>? configureEventBus = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<KafkaOptions>(configuration.GetSection("Kafka"));
        services.AddOptions<EventBusOptions>().Bind(configuration.GetSection("Kafka:EventBus"));
        services.AddEventBusCore(configureEventBus);

        services.TryAddSingleton<IEventBus, KafkaEventBus>();
        services.AddHostedService<KafkaSubscriber>();
        services.AddHealthChecks().AddCheck<KafkaHealthCheck>("kafka", tags: [HealthCheckTags.Ready]);
        return services;
    }
}
