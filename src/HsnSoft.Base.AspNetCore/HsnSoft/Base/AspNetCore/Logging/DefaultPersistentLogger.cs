using HsnSoft.Base.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HsnSoft.Base.AspNetCore.Logging;

public sealed class DefaultPersistentLogger<T> : DefaultBaseLogger, IPersistentLogger<T> where T : IPersistentLog
{
    public void PersistentInfoLog(T t) => Logger.Log(LogLevel.Trace, JsonConvert.SerializeObject(t));

    public void PersistentErrorLog(T t) => Logger.Log(LogLevel.Critical, JsonConvert.SerializeObject(t));
}