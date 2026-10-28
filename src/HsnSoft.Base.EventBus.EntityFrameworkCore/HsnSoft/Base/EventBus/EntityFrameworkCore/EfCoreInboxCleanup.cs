using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.EventBus.Inbox;
using HsnSoft.Base.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus.EntityFrameworkCore;

/// <summary>
/// Removes the rows of the messages that were handled longer ago than <see cref="EventInboxCleanupOptions.RetentionPeriod"/>, a batch at a time. Every instance of a service runs it and needs no coordination: a batch is the ids of old
/// <see cref="InboxStatuses.Completed"/> rows and a delete of those rows that checks the condition again, so two instances that pick the same ids delete each row once and the other one deletes nothing.
/// </summary>
public sealed partial class EfCoreInboxCleanup<TContext>(IServiceScopeFactory scopes, IOptions<EventInboxCleanupOptions> options, ILogger<EfCoreInboxCleanup<TContext>> logger)
    : PeriodicBackgroundService(options.Value.Interval, logger)
    where TContext : DbContext
{
    private readonly EventInboxCleanupOptions _options = options.Value;
    private readonly ILogger _logger = logger;

    protected override async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        if (_options.RetentionPeriod <= TimeSpan.Zero)
        {
            return;
        }

        DateTime cutoff = DateTime.UtcNow - _options.RetentionPeriod;
        int batch = Math.Max(_options.BatchSize, 1);
        int removed = 0;
        int deleted;
        do
        {
            deleted = await RemoveBatchAsync(cutoff, batch, cancellationToken).ConfigureAwait(false);
            removed += deleted;
        }
        while (deleted == batch && !cancellationToken.IsCancellationRequested);

        if (removed > 0)
        {
            LogRemoved(_logger, removed, _options.RetentionPeriod.TotalDays);
        }
    }

    private async Task<int> RemoveBatchAsync(DateTime cutoff, int batch, CancellationToken cancellationToken)
    {
        AsyncServiceScope scope = scopes.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            DbSet<InboxMessage> messages = scope.ServiceProvider.GetRequiredService<TContext>().Set<InboxMessage>();
            Guid[] ids = await messages
                .Where(row => row.Status == InboxStatuses.Completed && row.ProcessedAt < cutoff)
                .OrderBy(row => row.ProcessedAt)
                .Select(row => row.Id)
                .Take(batch)
                .ToArrayAsync(cancellationToken).ConfigureAwait(false);
            if (ids.Length == 0)
            {
                return 0;
            }

            // checked again: a row another instance removed is not there, and nothing else can have changed a completed row
            await messages.Where(row => ids.Contains(row.Id) && row.Status == InboxStatuses.Completed).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            return ids.Length;
        }
    }

    [LoggerMessage(EventId = 4601, EventName = "EVENT_INBOX_CLEANED", Level = LogLevel.Information, Message = "The inbox forgot {Count} message(s) handled more than {Days} day(s) ago")]
    private static partial void LogRemoved(ILogger logger, int count, double days);
}
