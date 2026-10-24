namespace Agt.Scheduler.Host;

/// <summary>Section <c>Scheduler</c>.</summary>
public sealed class SchedulerOptions
{
    /// <summary>The collection that is watched.</summary>
    public string Collection { get; set; } = "sample_records";

    /// <summary>How often the collection is checked.</summary>
    public int PollIntervalSeconds { get; set; } = 30;

    /// <summary>Records created within this many minutes before the check are reported.</summary>
    public int LookbackMinutes { get; set; } = 5;

    /// <summary>At most this many ids are written to one log line.</summary>
    public int MaxLoggedIds { get; set; } = 100;
}
