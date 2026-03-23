using System;
using HsnSoft.Base.DependencyInjection;
using JetBrains.Annotations;

namespace HsnSoft.Base.Logging.Abstracts;

public interface IAppConsoleLogger : IBaseLogger;

public interface IBaseLogger : ISingletonDependency
{
    void LogDebug([CanBeNull] string messageTemplate, [ItemCanBeNull] params object[] args);
    void LogInformation([CanBeNull] string messageTemplate, [ItemCanBeNull] params object[] args);
    void LogWarning([CanBeNull] string messageTemplate, [ItemCanBeNull] params object[] args);

    void LogError([CanBeNull] string messageTemplate, [ItemCanBeNull] params object[] args);
    void LogError(Exception exception, string messageTemplate, [ItemCanBeNull] params object[] args);
}