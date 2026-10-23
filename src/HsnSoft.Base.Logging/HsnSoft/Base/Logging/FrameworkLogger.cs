using System;
using System.Collections.Generic;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Logging;

/// <summary>
/// <see cref="IFrameworkLogger"/> on <c>Microsoft.Extensions.Logging</c>: structured logs under the <see cref="LogCategories.FrameworkLog"/>
/// category. Whether one is kept is decided by its level, like every other log (Information and above are persistent).
/// </summary>
#pragma warning disable CA1848, CA1873, CA2254 // a pass-through: the message template is the caller's, not a constant of this class
public sealed class FrameworkLogger : IFrameworkLogger
{
    private readonly ILogger _logger;

    public FrameworkLogger(ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _logger = loggerFactory.CreateLogger(LogCategories.FrameworkLog);
    }

    public void FrameworkInfoLog<T>(T log)
        where T : IFrameworkLog
        => Structured(LogLevel.Information, log);

    public void FrameworkWarnLog<T>(T log)
        where T : IFrameworkLog
        => Structured(LogLevel.Warning, log);

    public void FrameworkErrorLog<T>(T log)
        where T : IFrameworkLog
        => Structured(LogLevel.Error, log);

    public void LogDebug(string messageTemplate, params object?[] args) => Arguments(LogLevel.Debug, null, messageTemplate, args);

    public void LogInformation(string messageTemplate, params object?[] args) => Arguments(LogLevel.Information, null, messageTemplate, args);

    public void LogWarning(string messageTemplate, params object?[] args) => Arguments(LogLevel.Warning, null, messageTemplate, args);

    public void LogError(string messageTemplate, params object?[] args) => Arguments(LogLevel.Error, null, messageTemplate, args);

    public void LogError(Exception exception, string messageTemplate, params object?[] args) => Arguments(LogLevel.Error, exception, messageTemplate, args);

    // "{@Log}": providers that know the @ operator write the log as an object with properties, not as text
    private void Structured<T>(LogLevel level, T log)
    {
        ArgumentNullException.ThrowIfNull(log);

        using IDisposable? scope = _logger.BeginScope(Scope("Structured"));
        _logger.Log(level, "{@Log}", log);
    }

    private void Arguments(LogLevel level, Exception? exception, string messageTemplate, object?[] args)
    {
        using IDisposable? scope = _logger.BeginScope(Scope("Args"));
        _logger.Log(level, exception, messageTemplate, args);
    }

    private static Dictionary<string, object> Scope(string messageType)
        => new() { ["LogType"] = LogCategories.FrameworkLog, ["MessageType"] = messageType };
}
#pragma warning restore CA1848, CA1873, CA2254
