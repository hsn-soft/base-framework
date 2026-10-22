using System;
using HsnSoft.Base.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HsnSoft.Base.EventBus.RabbitMQ;

public static class RabbitMqServiceCollectionExtensions
{
    /// <summary>
    /// Registers the RabbitMQ <see cref="IEventBus"/>, the background subscriber for every handler added with
    /// <c>AddEventHandler</c>, and a readiness health check.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds the <c>RabbitMq</c> section (<see cref="RabbitMqOptions"/>).</param>
    /// <param name="serviceName">The logical service name; the queues are called <c>{serviceName}.{EventName}</c>.</param>
    /// <param name="configureEventBus">Optional broker-independent settings (retry attempts, delay).</param>
    public static IServiceCollection AddRabbitMqEventBus(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        Action<EventBusOptions>? configureEventBus = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));
        services.AddEventBusCore(options =>
        {
            options.ServiceName = serviceName;
            configureEventBus?.Invoke(options);
        });

        services.TryAddSingleton<RabbitMqConnection>();
        services.TryAddSingleton<IEventBus, RabbitMqEventBus>();
        services.AddHostedService<RabbitMqSubscriber>();
        services.AddHealthChecks().AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: [HealthCheckTags.Ready]);
        return services;
    }
}
