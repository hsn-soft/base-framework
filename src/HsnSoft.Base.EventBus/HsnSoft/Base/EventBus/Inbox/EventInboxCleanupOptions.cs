using System;

namespace HsnSoft.Base.EventBus.Inbox;

/// <summary>
/// How long the inbox keeps the rows of the messages it has handled (section <c>EventInbox:Cleanup</c>). Only <see cref="InboxStatuses.Completed"/> rows are ever removed: a row that is started or failed holds a lease or a retry count.
/// </summary>
public sealed class EventInboxCleanupOptions
{
    public const string SectionName = "EventInbox:Cleanup";

    /// <summary>
    /// How long a handled message is remembered. A copy of it that comes after this is handled again, so it must not be shorter than the broker keeps messages (Kafka keeps them 7 days by default). <see cref="TimeSpan.Zero"/> turns the cleanup off.
    /// </summary>
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(7);

    /// <summary>How often a relational database is cleaned (every instance of the service does it; a run that finds nothing costs one query). MongoDB removes by itself and ignores it.</summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>How many rows one statement removes, so a large backlog is not one long lock.</summary>
    public int BatchSize { get; set; } = 1000;
}
