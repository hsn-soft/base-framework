using HsnSoft.Base.Logging;

namespace HsnSoft.Base.AspNetCore.Logging;

public interface IRequestResponseLogger : IBaseLogger
{
    public void RequestResponseInfoLog<T>(T t) where T : IRequestResponseLog;
    public void RequestResponseErrorLog<T>(T t) where T : IRequestResponseLog;
}