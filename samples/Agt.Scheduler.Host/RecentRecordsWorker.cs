using HsnSoft.Base.Hosting;
using Microsoft.Extensions.Options;

namespace Agt.Scheduler.Host;

/// <summary>Every poll interval, writes the ids of the records created in the last few minutes to the log.</summary>
public sealed class RecentRecordsWorker(
    RecordStore store,
    IOptions<SchedulerOptions> options,
    TimeProvider timeProvider,
    ILogger<RecentRecordsWorker> logger)
    : PeriodicBackgroundService(TimeSpan.FromSeconds(options.Value.PollIntervalSeconds), logger)
{
    private readonly SchedulerOptions _options = options.Value;

    protected override async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        var since = timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-_options.LookbackMinutes);

        // one more than we log, so we can tell "exactly the limit" from "more than the limit"
        var ids = await store.FindIdsCreatedSinceAsync(since, _options.MaxLoggedIds + 1, cancellationToken);

        if (ids.Count == 0)
        {
            logger.LogInformation("No record was created in the last {Minutes} minute(s)", _options.LookbackMinutes);
            return;
        }

        var shown = ids.Take(_options.MaxLoggedIds).Select(id => id.ToString());
        logger.LogInformation(
            "{Count}{More} record(s) created in the last {Minutes} minute(s): {Ids}",
            Math.Min(ids.Count, _options.MaxLoggedIds),
            ids.Count > _options.MaxLoggedIds ? "+" : string.Empty,
            _options.LookbackMinutes,
            string.Join(", ", shown));
    }
}
