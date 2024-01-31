using HsnSoft.Base.Logging;

namespace HsnSoft.Base.EventBus.Logging;

public interface IEventBusLogger : IBaseLogger
{
    public void EventBusInfoLog<T>(T t) where T : IEventBusLog;
    public void EventBusErrorLog<T>(T t) where T : IEventBusLog;
}