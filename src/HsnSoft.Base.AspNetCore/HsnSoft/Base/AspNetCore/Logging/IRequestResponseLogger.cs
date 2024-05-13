using HsnSoft.Base.Logging;
using JetBrains.Annotations;

namespace HsnSoft.Base.AspNetCore.Logging;

public interface IRequestResponseLogger : IBaseLogger
{
    public void RequestResponseInfoLog<T>([NotNull] T t) where T : IRequestResponseLog;
    public void RequestResponseErrorLog<T>([NotNull] T t) where T : IRequestResponseLog;
}