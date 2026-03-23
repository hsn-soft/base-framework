using JetBrains.Annotations;

namespace HsnSoft.Base.Logging.Abstracts;

public interface IFrameworkLog : IPersistentLog;

public interface IFrameworkLogger : IBaseLogger
{
    void FrameworkInfoLog<T>([NotNull] T log) where T : IFrameworkLog;
    void FrameworkErrorLog<T>([NotNull] T log) where T : IFrameworkLog;
}