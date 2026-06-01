using HsnSoft.Base.Logging.Masking;
using HsnSoft.Base.Serilog.Loggers;
using Microsoft.Extensions.Configuration;
using Serilog.Events;

namespace HsnSoft.Base.AspNetCore.Logging;

public sealed class RequestResponseLogger(IConfiguration configuration, ILogMasker masker) : PersistentLogger(configuration, nameof(RequestResponseLogger),true), IRequestResponseLogger
{
    public void RequestResponseInfoLog<T>(T log) where T : IRequestResponseLog => Write(LogEventLevel.Information, "RequestResponseLog", log);

    public void RequestResponseWarnLog<T>(T log) where T : IRequestResponseLog => Write(LogEventLevel.Warning, "RequestResponseLog", log);

    public void RequestResponseErrorLog<T>(T log) where T : IRequestResponseLog => Write(LogEventLevel.Error, "RequestResponseLog", log);

    private void Write<T>(LogEventLevel logLevel, string logType, T log)
    {
        // object masked = masker.MaskObject(log);
        //
        // // birinci yöntem
        // Logger
        //     .ForContext("LogType", logType)
        //     .ForContext("Log", masked, destructureObjects: true)
        //     .Write(logLevel, "{logLevel} | {LogType} created", logLevel.ToString(),logType);

        // ikinci yöntem, serilog konfigurasyonunda maskeleme ekleniyor zaten
        Logger
            .ForContext("LogType", logType)
            .Write(logLevel, "{@Log}", log);
    }
}