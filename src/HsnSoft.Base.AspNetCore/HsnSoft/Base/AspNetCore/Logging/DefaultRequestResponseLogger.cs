using HsnSoft.Base.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HsnSoft.Base.AspNetCore.Logging;

public sealed class DefaultRequestResponseLogger : DefaultBaseLogger, IRequestResponseLogger
{
    public void RequestResponseInfoLog<T>(T t) where T : IRequestResponseLog => Write(LogLevel.Trace, t);
    public void RequestResponseErrorLog<T>(T t) where T : IRequestResponseLog => Write(LogLevel.Critical, t);

    private void Write<T>(LogLevel logLevel, T log) => BaseLogger.Log(logLevel, "{@Log}", JsonConvert.SerializeObject(log));
}