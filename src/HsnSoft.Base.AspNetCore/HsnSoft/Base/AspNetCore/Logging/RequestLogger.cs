using HsnSoft.Base.Logging.Masking;
using HsnSoft.Base.Serilog.Loggers;
using Microsoft.Extensions.Configuration;
using Serilog.Events;

namespace HsnSoft.Base.AspNetCore.Logging;

public sealed class RequestLogger(IConfiguration configuration, ILogMasker masker) : PersistentLogger(configuration, "RequestLogger"), IRequestResponseLogger
{
    public void RequestResponseInfoLog<T>(T log) where T : IRequestResponseLog => Write(LogEventLevel.Information, "RequestResponseLog", log);

    public void RequestResponseWarnLog<T>(T log) where T : IRequestResponseLog => Write(LogEventLevel.Warning, "RequestResponseLog", log);

    public void RequestResponseErrorLog<T>(T log) where T : IRequestResponseLog => Write(LogEventLevel.Error, "RequestResponseLog", log);

    private void Write<T>(LogEventLevel logLevel, string logType, T log)
    {
        object masked = masker.MaskObject(log);

        Logger
            .ForContext("LogType", logType)
            .ForContext("Payload", masked, destructureObjects: true)
            .Write(logLevel, "{LogType} created", logType);
    }
}