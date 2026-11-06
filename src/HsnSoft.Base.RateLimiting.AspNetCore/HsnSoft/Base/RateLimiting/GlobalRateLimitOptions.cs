using System.Collections.Generic;

namespace HsnSoft.Base.RateLimiting;

/// <summary>The limit of every request of one address (section <c>RateLimiting:Global</c>), which <c>UseBaseRateLimiting</c> applies. It is off until <see cref="Permits"/> is set.</summary>
public sealed class GlobalRateLimitOptions
{
    public const string SectionName = "RateLimiting:Global";

    /// <summary>How many requests one address may make in the window; 0 (the default) is no global limit.</summary>
    public int Permits { get; set; }

    /// <summary>The window in seconds. Default: 60.</summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>The paths that are not counted (a path that starts with one of them): the probes of the platform must never be refused. Default: <c>/health</c>.</summary>
    public List<string> ExcludedPaths { get; set; } = ["/health"];
}
