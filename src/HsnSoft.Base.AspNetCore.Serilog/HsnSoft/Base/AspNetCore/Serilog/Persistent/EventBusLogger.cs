using HsnSoft.Base.EventBus.Logging;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog.Events;

namespace HsnSoft.Base.AspNetCore.Serilog.Persistent;

public sealed class EventBusLogger : BaseLogger, IEventBusLogger
{
    public EventBusLogger(IConfiguration configuration) : base(configuration)
    {
    }

    public void EventBusInfoLog<T>(T t) where T : IEventBusLog => Write(LogEventLevel.Verbose, t);

    public void EventBusErrorLog<T>(T t) where T : IEventBusLog => Write(LogEventLevel.Fatal, t);

    private void Write<T>(LogEventLevel logLevel, T log) => Logger.Write(logLevel, JsonConvert.SerializeObject(log));
}