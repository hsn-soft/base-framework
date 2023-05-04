using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Logging;

public interface IExceptionWithSelfLogging
{
    void Log(ILogger logger);
}