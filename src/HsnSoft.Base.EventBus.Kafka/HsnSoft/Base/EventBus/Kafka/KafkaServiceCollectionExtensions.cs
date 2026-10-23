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
    /// <param name="configuration">Holds the <c>Kafka</c> section (<see cref="KafkaOptions"/>).</param>
    /// <param name="serviceName">The logical service name; it is the consumer group.</param>
    /// <param name="configureEventBus">Optional broker-independent settings (retry attempts, delay).</param>
    public static IServiceCollection AddKafkaEventBus(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        Action<EventBusOptions>? configureEventBus = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        services.Configure<KafkaOptions>(configuration.GetSection("Kafka"));
        services.AddEventBusCore(options =>
        {
            options.ServiceName = serviceName;
            configureEventBus?.Invoke(options);
        });

        services.TryAddSingleton<IEventBus, KafkaEventBus>();
        services.AddHostedService<KafkaSubscriber>();
        services.AddHealthChecks().AddCheck<KafkaHealthCheck>("kafka", tags: [HealthCheckTags.Ready]);
        return services;
    }
}
