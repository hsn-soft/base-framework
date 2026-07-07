namespace Hhs.Scheduler.ApiTrigger.Host.Models;

public class ServiceScheduledEndpoints
{
    public string ServiceName { get; set; }
    public string ServiceUrl { get; set; }
    public List<EndpointModel> Endpoints { get; set; }
}