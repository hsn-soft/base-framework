using System.Reflection;
using HsnSoft.Base.AspNetCore.Tracing;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.EventBus.Logging;
using HsnSoft.Base.EventBus.RabbitMQ;
using HsnSoft.Base.EventBus.RabbitMQ.Configs;
using HsnSoft.Base.EventBus.RabbitMQ.Connection;
using HsnSoft.Base.EventBus.SubManagers;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.Logging.Masking;
using HsnSoft.Base.Serilog;
using HsnSoft.Base.Serilog.Loggers;
using HsnSoft.Base.Tracing;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Sample.Api.Hosting.Extensions;

public static class EventBusServiceCollectionExtensions
{
    /// <summary>
    /// Registers the RabbitMQ event bus and every <see cref="IIntegrationEventHandler"/> found in <paramref name="handlerAssembly"/>.
    /// Settings are read from <c>RabbitMq:Connection</c> and <c>RabbitMq:EventBus</c>.
    /// </summary>
    public static IServiceCollection AddSampleEventBus(this IServiceCollection services, IConfiguration configuration, Assembly handlerAssembly)
    {
        services.Configure<RabbitMqConnectionSettings>(configuration.GetSection("RabbitMq:Connection"));
        services.Configure<RabbitMqEventBusConfig>(configuration.GetSection("RabbitMq:EventBus"));

        services.AddSingleton<ILogMasker, LogMasker>();
        services.AddSingleton<IAppConsoleLogger, AppLogger>();
        services.AddSingleton<IFrameworkLogger, FrameworkLogger>();
        services.AddSingleton<IRefreshableLogger>(sp => (IRefreshableLogger)sp.GetRequiredService<IFrameworkLogger>());

        services.AddSingleton<ITraceAccesor, HttpContextTraceAccessor>();
        services.AddSingleton<IEventBusLogger, EventBusLogger>();
        services.AddSingleton<IRefreshableLogger>(sp => (IRefreshableLogger)sp.GetRequiredService<IEventBusLogger>());

        services.AddSingleton<IRabbitMqPersistentConnection, RabbitMqPersistentConnection>();
        services.AddSingleton<IEventBusSubscriptionManager, InMemoryEventBusSubscriptionManager>();
        services.AddSingleton<IEventDispatcher, EventDispatcher>();
        services.AddSingleton<IFailedEventNotifier, FailedEventNotifier>();
        services.AddSingleton<IEventBus, EventBusRabbitMq>(sp => new EventBusRabbitMq(sp));

        foreach (var handlerType in GetHandlerTypes(handlerAssembly))
        {
            services.AddTransient(handlerType);
        }

        return services;
    }

    /// <summary>Subscribes every integration event handler of <paramref name="handlerAssembly"/> to its event.</summary>
    public static WebApplication UseSampleEventBus(this WebApplication app, Assembly handlerAssembly)
    {
        var eventBus = app.Services.GetRequiredService<IEventBus>();

        foreach (var handlerType in GetHandlerTypes(handlerAssembly))
        {
            var eventType = handlerType.GetInterfaces().First(x => x.IsGenericType).GenericTypeArguments[0];
            eventBus.Subscribe(eventType, handlerType);
        }

        return app;
    }

    private static List<Type> GetHandlerTypes(Assembly assembly)
        => assembly.GetTypes()
            .Where(t => typeof(IIntegrationEventHandler).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false })
            .ToList();
}
