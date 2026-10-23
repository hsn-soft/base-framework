using System;
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
    /// provider (<c>HsnSoft.Base.Logging.Serilog</c>, NLog, ...).
    /// </summary>
    public static IServiceCollection AddBaseLogging(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IAppConsoleLogger>(provider => new AppConsoleLogger(provider.GetRequiredService<ILoggerFactory>()));
        services.TryAddSingleton<IFrameworkLogger>(provider => new FrameworkLogger(provider.GetRequiredService<ILoggerFactory>()));
        services.TryAddSingleton<ILogMasker, LogMasker>();
        return services;
    }
}
