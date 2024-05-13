using HsnSoft.Base.EventBus.Logging;
using HsnSoft.Base.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HsnSoft.Base.EventBus;

public sealed class DefaultEventBusLogger : DefaultBaseLogger, IEventBusLogger
{
    public void EventBusInfoLog<T>(T t) where T : IEventBusLog => Write(LogLevel.Trace, t);
    public void EventBusErrorLog<T>(T t) where T : IEventBusLog => Write(LogLevel.Critical, t);

    private void Write<T>(LogLevel logLevel, T log) => BaseLogger.Log(logLevel, "{@Log}", JsonConvert.SerializeObject(log));
}