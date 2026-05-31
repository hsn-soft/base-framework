using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.Logging.Masking;
using Microsoft.Extensions.Configuration;
using Serilog.Events;

namespace HsnSoft.Base.Serilog.Loggers;

public sealed class FrameworkLogger(IConfiguration configuration, ILogMasker masker) : PersistentLogger(configuration, "FrameworkLogger",true), IFrameworkLogger
{
    public void FrameworkInfoLog<T>(T log) where T : IFrameworkLog => Write(LogEventLevel.Information, "FrameworkLog", log);

    public void FrameworkWarnLog<T>(T log) where T : IFrameworkLog => Write(LogEventLevel.Warning, "FrameworkLog", log);

    public void FrameworkErrorLog<T>(T log) where T : IFrameworkLog => Write(LogEventLevel.Error, "FrameworkLog", log);

    private void Write<T>(LogEventLevel logLevel, string logType, T log)
    {
        object masked = masker.MaskObject(log);

        Logger
            .ForContext("LogType", logType)
            .ForContext("Payload", masked, destructureObjects: true)
            .Write(logLevel, "{LogType} created", logType);
    }
}