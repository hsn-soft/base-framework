namespace Hhs.Shared.Helper.Configuration;

public abstract class RetrySettingsBase
{
    public int[] DelaySeconds { get; set; } = [60, 120, 300, 900];
    public int MaxRetryCount { get; set; } = 4;
    public int RetryWorkerIntervalSeconds { get; set; } = 10;
    public int ClaimFailRescheduleDelaySeconds { get; set; } = 10;
    public int BatchSize { get; set; } = 50;

    /// <summary>
    /// How long (in minutes) an EventInboxMessage may stay in 'Started' status before being
    /// considered stale and reset to 'Failed' so the next broker re-delivery can process it.
    /// This is the coarse last-resort sweep for a handler that hangs with no broker-level
    /// disconnect at all. Default: 15 minutes.
    /// </summary>
    public int StaleInboxMessageThresholdMinutes { get; set; } = 15;

    /// <summary>
    /// How long (in seconds) a single processing attempt gets before a redelivery of the same
    /// message is allowed to reclaim it. Much shorter than <see cref="StaleInboxMessageThresholdMinutes"/>
    /// because RabbitMQ redelivers an unacked message within seconds of a connection/heartbeat drop
    /// (crash, debugger pause, pod restart) — without this, that redelivery is silently swallowed
    /// (the inbox row is still 'Started') until the much slower stale-sweep fires, by which point the
    /// message has already been ACKed and is gone forever. Default: 120 seconds.
    /// </summary>
    public int InboxProcessingLeaseSeconds { get; set; } = 120;
}
