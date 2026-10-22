namespace HsnSoft.Base.Hosting;

/// <summary>Tags the infrastructure projects put on their health checks so a host can expose them as one readiness probe.</summary>
public static class HealthCheckTags
{
    /// <summary>The check says whether a dependency (database, cache, broker) answers.</summary>
    public const string Ready = "ready";
}
