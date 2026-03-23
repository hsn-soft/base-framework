using Microsoft.Extensions.Configuration;
using Serilog.Events;

namespace HsnSoft.Base.Serilog.Persistent;

public sealed class RequestLogger : BaseLogger, IRequestResponseLogger
{
    public RequestLogger(IConfiguration configuration) : base(configuration)
    {
    }

    public void RequestResponseInfoLog<T>(T t) where T : IRequestResponseLog => Write(LogEventLevel.Verbose, t);
    public void RequestResponseErrorLog<T>(T t) where T : IRequestResponseLog => Write(LogEventLevel.Fatal, t);

    private void Write<T>(LogEventLevel logLevel, T log)
        => Logger.ForContext("LogType", "RequestResponseLog").Write(logLevel, MaskedSerializationHelper.SerializeWithMasking(log));
}