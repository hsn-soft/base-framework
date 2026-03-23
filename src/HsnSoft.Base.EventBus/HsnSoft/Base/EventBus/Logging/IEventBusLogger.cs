using HsnSoft.Base.Logging;
using JetBrains.Annotations;

namespace HsnSoft.Base.EventBus.Logging;

public interface IEventBusLogger : IBaseLogger
{
    void EventBusInfoLog<T>([NotNull] T t) where T : IEventBusLog;
    void EventBusErrorLog<T>([NotNull] T t) where T : IEventBusLog;
}