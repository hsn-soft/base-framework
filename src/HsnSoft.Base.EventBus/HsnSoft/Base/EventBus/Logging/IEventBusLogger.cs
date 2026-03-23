using HsnSoft.Base.Logging.Abstracts;
using JetBrains.Annotations;

namespace HsnSoft.Base.EventBus.Logging;

public interface IEventBusLog : IPersistentLog;

public interface IEventBusLogger : IBaseLogger
{
    void EventBusInfoLog<T>([NotNull] T log) where T : IEventBusLog;
    void EventBusWarnLog<T>([NotNull] T log) where T : IEventBusLog;
    void EventBusErrorLog<T>([NotNull] T log) where T : IEventBusLog;
}