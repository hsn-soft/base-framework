using Quartz;

namespace Abc.Scheduler.Host;

/// <summary>The settings of the scheduler (section <c>Scheduler</c>): the key it calls with, where the services are and which jobs it calls when.</summary>
internal sealed class SchedulerSettings
{
    public const string SectionName = "Scheduler";

    private static readonly string[] s_methods = ["GET", "POST", "PUT", "DELETE"];

    /// <summary>The key of the scheduler: the services check it (<c>Scheduler:ApiKey</c> of theirs is the same). A secret, from the environment, a .env file or the secret of the platform.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>The address of each service by its name (<c>shipment-service</c>): <c>http://localhost:5440</c> in development, <c>http://shipment-service:8080</c> in the cluster and the compose network.</summary>
    public Dictionary<string, string> Services { get; set; } = [];

    /// <summary>The jobs: what is called, where, and when.</summary>
    public List<ScheduledJob> Jobs { get; set; } = [];

    /// <summary>Stops the host at its start, with every reason, when a job cannot work: a service that is not listed, a path or a cron that is wrong, a name used twice.</summary>
    public void Validate()
    {
        List<string> problems = [];
        foreach (KeyValuePair<string, string> service in Services.Where(service => !Uri.TryCreate(service.Value, UriKind.Absolute, out Uri? uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
        {
            problems.Add($"Scheduler:Services:{service.Key} is not an http address");
        }

        foreach (IGrouping<string, ScheduledJob> twice in Jobs.GroupBy(job => job.Name, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1))
        {
            problems.Add($"the job '{twice.Key}' is listed {twice.Count()} times");
        }

        foreach (ScheduledJob job in Jobs)
        {
            if (string.IsNullOrWhiteSpace(job.Name))
            {
                problems.Add("a job has no name");
            }

            if (!Services.ContainsKey(job.Service))
            {
                problems.Add($"the job '{job.Name}' is for the service '{job.Service}', which is not in Scheduler:Services");
            }

            if (!job.Path.StartsWith('/'))
            {
                problems.Add($"the path of the job '{job.Name}' must start with '/'");
            }

            if (!s_methods.Contains(job.Method, StringComparer.OrdinalIgnoreCase))
            {
                problems.Add($"the method of the job '{job.Name}' is '{job.Method}' (GET, POST, PUT or DELETE)");
            }

            if (!CronExpression.IsValidExpression(job.Cron))
            {
                problems.Add($"the cron of the job '{job.Name}' is not a Quartz cron expression ('{job.Cron}', six or seven fields)");
            }
        }

        if (problems.Count > 0)
        {
            throw new InvalidOperationException("The Scheduler section is not usable: " + string.Join("; ", problems) + ".");
        }
    }
}

/// <summary>A job: one call of an endpoint of a service on a cron schedule.</summary>
internal sealed class ScheduledJob
{
    /// <summary>The name of the job, in the logs and in the header <c>SchedulerJobName</c>.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The service that is called (a name in <c>Scheduler:Services</c>).</summary>
    public string Service { get; set; } = string.Empty;

    /// <summary>The path of the endpoint, from the root of the service, starting with <c>/</c> (the one the service has: <c>/api/shipment-service/v1/commercial/jobs/payment-summary</c>).</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>The HTTP method. Default POST.</summary>
    public string Method { get; set; } = "POST";

    /// <summary>When: a Quartz cron expression with seconds (<c>0/30 * * * * ?</c> is every thirty seconds).</summary>
    public string Cron { get; set; } = string.Empty;

    /// <summary>Also calls the job once when the scheduler starts. Default false.</summary>
    public bool TriggerOnStartup { get; set; }

    /// <summary>How long the call may take before it is given up (the service may still be working on it). Default 30 seconds.</summary>
    public int TimeoutSeconds { get; set; } = 30;
}
