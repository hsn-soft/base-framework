using HsnSoft.Base.DependencyInjection;
using JetBrains.Annotations;

namespace HsnSoft.Base.Logging;

public interface IBaseLogger : ISingletonDependency
{
    public void LogDebug([CanBeNull] string messageTemplate, [ItemCanBeNull] params object[] args);
    public void LogError([CanBeNull] string messageTemplate, [ItemCanBeNull] params object[] args);
    public void LogWarning([CanBeNull] string messageTemplate, [ItemCanBeNull] params object[] args);
    public void LogInformation([CanBeNull] string messageTemplate, [ItemCanBeNull] params object[] args);
}