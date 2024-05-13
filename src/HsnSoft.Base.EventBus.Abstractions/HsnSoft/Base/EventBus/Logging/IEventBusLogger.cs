using HsnSoft.Base.Logging;

namespace HsnSoft.Base.EventBus.Logging;

public interface IEventBusLogger<in T> : IPersistentLogger<T> where T : IEventBusLog
{
}