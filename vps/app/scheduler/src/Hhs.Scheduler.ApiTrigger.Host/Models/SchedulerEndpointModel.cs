namespace Hhs.Scheduler.ApiTrigger.Host.Models;

public class SchedulerEndpointModel : EndpointModel
{
    public string ServiceName { get; set; }
}

public class EndpointModel
{
    public string JobName { get; set; }
    public string Url { get; set; }
    public string Method { get; set; }
    public string Pattern { get; set; }
    public PayloadModel Payload { get; set; }
    public string Key { get; set; }

    /// <summary>
    /// Whether this job should also fire once immediately when the scheduler starts up, in
    /// addition to its cron schedule. Defaults to true to preserve existing behavior for entries
    /// that don't specify it.
    /// </summary>
    public bool TriggerOnStartup { get; set; } = true;
}

public class PayloadModel
{
    /// <summary>
    /// How often this job's trigger fires, in seconds. Used to compute the human-readable
    /// JobPeriodDesc and the estimated NextTriggerTimeUtc sent in the request body.
    /// </summary>
    public int PeriodSeconds { get; set; }
}