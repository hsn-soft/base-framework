using HsnSoft.Base.Logging.Abstracts;
using JetBrains.Annotations;

namespace HsnSoft.Base.AspNetCore.Logging;

public interface IRequestResponseLog : IPersistentLog;

public interface IRequestResponseLogger : IBaseLogger
{
    void RequestResponseInfoLog<T>([NotNull] T log) where T : IRequestResponseLog;
    void RequestResponseWarnLog<T>([NotNull] T log) where T : IRequestResponseLog;
    void RequestResponseErrorLog<T>([NotNull] T log) where T : IRequestResponseLog;
}