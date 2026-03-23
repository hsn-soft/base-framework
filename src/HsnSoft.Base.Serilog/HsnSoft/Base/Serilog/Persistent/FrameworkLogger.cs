using HsnSoft.Base.Logging;
using Microsoft.Extensions.Configuration;
using Serilog.Events;

namespace HsnSoft.Base.Serilog.Persistent;

public sealed class FrameworkLogger : BaseLogger, IFrameworkLogger
{
    public FrameworkLogger(IConfiguration configuration) : base(configuration)
    {
    }

    public void FrameworkInfoLog<T>(T t) where T : IFrameworkLog => Write(LogEventLevel.Verbose, t);
    public void FrameworkErrorLog<T>(T t) where T : IFrameworkLog => Write(LogEventLevel.Fatal, t);

    private void Write<T>(LogEventLevel logLevel, T log)
        => Logger.ForContext("LogType", "FrameworkLog").Write(logLevel, MaskedSerializationHelper.SerializeWithMasking(log));
}