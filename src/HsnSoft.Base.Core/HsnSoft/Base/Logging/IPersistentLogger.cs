using JetBrains.Annotations;

namespace HsnSoft.Base.Logging;

public interface IPersistentLogger : IBaseLogger
{
    public void PersistentInfoLog<T>([NotNull] T t) where T : IPersistentLog;
    public void PersistentErrorLog<T>([NotNull] T t) where T : IPersistentLog;
}