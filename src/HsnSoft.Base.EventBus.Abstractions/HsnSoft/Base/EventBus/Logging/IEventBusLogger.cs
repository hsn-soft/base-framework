using HsnSoft.Base.Logging;

namespace HsnSoft.Base.EventBus.Logging;

public interface IEventBusLogger<T> : IBaseLogger<T>
{
    public void EventBusInfoLog<TLog>(TLog t) where TLog : IEventBusLog;
    public void EventBusErrorLog<TLog>(TLog t) where TLog : IEventBusLog;
}