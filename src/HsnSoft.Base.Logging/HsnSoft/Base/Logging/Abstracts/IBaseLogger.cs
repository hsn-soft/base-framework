using System;

namespace HsnSoft.Base.Logging.Abstracts;

/// <summary>A logger with the four calls products use. It writes through <c>Microsoft.Extensions.Logging</c>, so it ends up wherever the product's logging provider sends logs.</summary>
public interface IBaseLogger
{
    void LogDebug(string messageTemplate, params object?[] args);

    void LogInformation(string messageTemplate, params object?[] args);

    void LogWarning(string messageTemplate, params object?[] args);

    void LogError(string messageTemplate, params object?[] args);

    void LogError(Exception exception, string messageTemplate, params object?[] args);
}

/// <summary>For what is only worth seeing in the console: the logging providers keep it out of the persistent sink (Graylog, file).</summary>
public interface IAppConsoleLogger : IBaseLogger;
