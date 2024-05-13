using JetBrains.Annotations;

namespace HsnSoft.Base.Logging;

public interface IPersistentLogger<in T> : IBaseLogger where T : IPersistentLog
{
    public void PersistentInfoLog([NotNull] T t);
    public void PersistentErrorLog([NotNull] T t);
}