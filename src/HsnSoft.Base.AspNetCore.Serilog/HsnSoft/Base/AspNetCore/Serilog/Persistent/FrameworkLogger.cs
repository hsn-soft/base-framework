using HsnSoft.Base.Logging;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog.Events;

namespace HsnSoft.Base.AspNetCore.Serilog.Persistent;

public sealed class FrameworkLogger : BaseLogger, IPersistentLogger
{
    public FrameworkLogger(IConfiguration configuration) : base(configuration)
    {
    }

    public void PersistentInfoLog<T>(T t) where T : IPersistentLog => Write(LogEventLevel.Verbose, t);
    public void PersistentErrorLog<T>(T t) where T : IPersistentLog => Write(LogEventLevel.Fatal, t);

    private void Write<T>(LogEventLevel logLevel, T log) => Logger.Write(logLevel, JsonConvert.SerializeObject(log));
}