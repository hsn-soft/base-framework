using System;
using System.Reflection;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.Logging.Masking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Logging;

public static class LoggingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the logger interfaces products use (<see cref="IAppConsoleLogger"/>, <see cref="IFrameworkLogger"/>) and the
    /// <see cref="ILogMasker"/>. They write through <c>Microsoft.Extensions.Logging</c>: choose where the logs go with a logging
    /// provider (<c>HsnSoft.Base.Logging.Serilog</c>, NLog, ...). What is secret is said by the classes, with
    /// <see cref="SensitiveDataAttribute"/> on a property; a place that sees only JSON and not the classes (a gateway) hides the properties of the
    /// contract classes in <paramref name="sensitiveDataAssemblies"/> too.
    /// </summary>
    public static IServiceCollection AddBaseLogging(this IServiceCollection services, params Assembly[] sensitiveDataAssemblies)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IAppConsoleLogger>(provider => new AppConsoleLogger(provider.GetRequiredService<ILoggerFactory>()));
        services.TryAddSingleton<IFrameworkLogger>(provider => new FrameworkLogger(provider.GetRequiredService<ILoggerFactory>()));
        services.TryAddSingleton(_ =>
        {
            SensitiveDataRegistry registry = new();
            foreach (Assembly assembly in sensitiveDataAssemblies)
            {
                registry.Add(assembly);
            }

            return registry;
        });
        services.TryAddSingleton<ILogMasker>(provider => new LogMasker(provider.GetRequiredService<SensitiveDataRegistry>()));
        return services;
    }
}
