namespace HsnSoft.Base.Logging.Abstracts;

public interface IFrameworkLog : IPersistentLog;

/// <summary>For structured logs that are meant to be kept: they go to the persistent sink like any other log of level Information or higher.</summary>
public interface IFrameworkLogger : IBaseLogger
{
    void FrameworkInfoLog<T>(T log)
        where T : IFrameworkLog;

    void FrameworkWarnLog<T>(T log)
        where T : IFrameworkLog;

    void FrameworkErrorLog<T>(T log)
        where T : IFrameworkLog;
}
