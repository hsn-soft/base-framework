using JetBrains.Annotations;

namespace HsnSoft.Base.Logging;

public interface IFrameworkLogger : IBaseLogger
{
    void FrameworkInfoLog<T>([NotNull] T t) where T : IFrameworkLog;
    void FrameworkErrorLog<T>([NotNull] T t) where T : IFrameworkLog;
}