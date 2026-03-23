using HsnSoft.Base.DependencyInjection;
using JetBrains.Annotations;

namespace HsnSoft.Base.Logging;

public interface IBaseLogger : ISingletonDependency
{
    void LogDebug([CanBeNull] string messageTemplate, [ItemCanBeNull] params object[] args);
    void LogError([CanBeNull] string messageTemplate, [ItemCanBeNull] params object[] args);
    void LogWarning([CanBeNull] string messageTemplate, [ItemCanBeNull] params object[] args);
    void LogInformation([CanBeNull] string messageTemplate, [ItemCanBeNull] params object[] args);
}