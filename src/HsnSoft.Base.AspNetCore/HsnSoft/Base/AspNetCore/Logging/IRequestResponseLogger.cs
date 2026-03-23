using HsnSoft.Base.Logging;
using JetBrains.Annotations;

namespace HsnSoft.Base.AspNetCore.Logging;

public interface IRequestResponseLogger : IBaseLogger
{
    void RequestResponseInfoLog<T>([NotNull] T t) where T : IRequestResponseLog;
    void RequestResponseErrorLog<T>([NotNull] T t) where T : IRequestResponseLog;
}