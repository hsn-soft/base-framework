using HsnSoft.Base.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HsnSoft.Base.AspNetCore.Logging;

public sealed class DefaultPersistentLogger : DefaultBaseLogger, IPersistentLogger
{
    public DefaultPersistentLogger(IConfiguration configuration) : base(configuration)
    {
    }

    public void PersistentInfoLog<T>(T t) where T : IPersistentLog => Write(LogLevel.Trace, t);
    public void PersistentErrorLog<T>(T t) where T : IPersistentLog => Write(LogLevel.Critical, t);

    private void Write<T>(LogLevel logLevel, T log) => Logger.Log(logLevel, "{@Log}", JsonConvert.SerializeObject(log));
}