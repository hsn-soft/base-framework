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
    public string Body { get; set; }
    public string Key { get; set; }
}