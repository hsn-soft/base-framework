namespace HsnSoft.Base.Logging.Abstracts;

public interface IRefreshableLogger
{
    /// <summary>
    /// Discards the current sink pipeline (Graylog, file, etc.) and rebuilds it from scratch.
    /// Works around third-party sink bugs that can leave the underlying transport permanently
    /// wedged after a bad startup race (e.g. Serilog.Sinks.Graylog.Core's HttpTransportClient),
    /// self-healing on a schedule instead of requiring a process restart.
    /// </summary>
    void RefreshSink();
}
