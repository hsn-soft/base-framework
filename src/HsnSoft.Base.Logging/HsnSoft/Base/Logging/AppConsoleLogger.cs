using System;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Logging;

/// <summary>
/// <see cref="IAppConsoleLogger"/> on <c>Microsoft.Extensions.Logging</c>. It writes under a category that starts with
/// <see cref="LogCategories.ConsoleOnlyPrefix"/>, which is how the logging providers keep its logs out of the persistent sink.
/// </summary>
#pragma warning disable CA1848, CA1873, CA2254 // a pass-through: the message template is the caller's, not a constant of this class
public sealed class AppConsoleLogger : IAppConsoleLogger
{
    private readonly ILogger _logger;

    /// <param name="loggerFactory">The factory of the product's logging provider.</param>
    /// <param name="loggerName">Shown as the source of the logs (default <c>AppLogger</c>).</param>
    public AppConsoleLogger(ILoggerFactory loggerFactory, string loggerName = "AppLogger")
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentException.ThrowIfNullOrWhiteSpace(loggerName);

        _logger = loggerFactory.CreateLogger(LogCategories.ConsoleOnlyPrefix + loggerName);
    }

    public void LogDebug(string messageTemplate, params object?[] args) => _logger.LogDebug(messageTemplate, args);

    public void LogInformation(string messageTemplate, params object?[] args) => _logger.LogInformation(messageTemplate, args);

    public void LogWarning(string messageTemplate, params object?[] args) => _logger.LogWarning(messageTemplate, args);

    public void LogError(string messageTemplate, params object?[] args) => _logger.LogError(messageTemplate, args);

    public void LogError(Exception exception, string messageTemplate, params object?[] args) => _logger.LogError(exception, messageTemplate, args);
}
#pragma warning restore CA1848, CA1873, CA2254
