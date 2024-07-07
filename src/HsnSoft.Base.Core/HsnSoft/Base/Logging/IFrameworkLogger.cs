using JetBrains.Annotations;

namespace HsnSoft.Base.Logging;

public interface IFrameworkLogger : IBaseLogger
{
    public void FrameworkInfoLog<T>([NotNull] T t) where T : IFrameworkLog;
    public void FrameworkErrorLog<T>([NotNull] T t) where T : IFrameworkLog;
}