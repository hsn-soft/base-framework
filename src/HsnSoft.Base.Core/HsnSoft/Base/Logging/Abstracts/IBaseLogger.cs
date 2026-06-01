using System;
using HsnSoft.Base.DependencyInjection;
using JetBrains.Annotations;

namespace HsnSoft.Base.Logging.Abstracts;

public interface IAppConsoleLogger : IBaseLogger;

public interface IBaseLogger : ISingletonDependency
{
    void LogDebug([NotNull] string messageTemplate, [ItemCanBeNull] params object[] args);
    void LogInformation([NotNull] string messageTemplate, [ItemCanBeNull] params object[] args);
    void LogWarning([NotNull] string messageTemplate, [ItemCanBeNull] params object[] args);

    void LogError([NotNull] string messageTemplate, [ItemCanBeNull] params object[] args);
    void LogError(Exception exception, string messageTemplate, [ItemCanBeNull] params object[] args);
}