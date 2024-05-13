using HsnSoft.Base.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HsnSoft.Base.AspNetCore.Logging;

public sealed class DefaultPersistentLogger : DefaultBaseLogger, IPersistentLogger
{
    public void PersistentInfoLog<T>(T t) where T : IPersistentLog => Write(LogLevel.Trace, t);
    public void PersistentErrorLog<T>(T t) where T : IPersistentLog => Write(LogLevel.Critical, t);

    private void Write<T>(LogLevel logLevel, T log) => BaseLogger.Log(logLevel, "{@Log}", JsonConvert.SerializeObject(log));
}