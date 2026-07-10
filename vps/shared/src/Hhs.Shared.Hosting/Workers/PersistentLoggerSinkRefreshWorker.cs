using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Hhs.Shared.Hosting.Workers;

// Periodically rebuilds every registered persistent logger's sink pipeline (Graylog/file). Guards
// against a third-party sink bug (Serilog.Sinks.Graylog.Core's HttpTransportClient has an unlocked
// lazy-init race that can leave its HttpClient permanently unconfigured for the rest of the
// process's life if lost once at startup) — bounds the resulting Graylog outage to one refresh
// interval instead of requiring a manual restart. Only runs when Graylog is actually configured;
// a no-op process (rebuilding a plain file sink) would just be wasted churn otherwise.
public sealed class PersistentLoggerSinkRefreshWorker(
    IEnumerable<IRefreshableLogger> refreshableLoggers,
    IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!GetGraylogIsActive()) return;

        var loggers = refreshableLoggers.ToList();
        if (loggers.Count == 0) return;

        var interval = TimeSpan.FromMinutes(GetIntervalMinutes());

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            foreach (var logger in loggers)
            {
                try
                {
                    logger.RefreshSink();
                }
                catch
                {
                    // best-effort: a failed refresh just means we try again next tick
                }
            }
        }
    }

    private int GetIntervalMinutes()
    {
        try
        {
            int minutes = int.Parse(configuration["FrameworkLogger:GrayLog:SinkRefreshIntervalMinutes"] ?? "5");
            return minutes <= 0 ? 5 : minutes;
        }
        catch
        {
            return 5;
        }
    }

    private bool GetGraylogIsActive()
    {
        try
        {
            return bool.Parse(configuration["FrameworkLogger:IsGrayLogActive"] ?? "false");
        }
        catch
        {
            return false;
        }
    }
}
