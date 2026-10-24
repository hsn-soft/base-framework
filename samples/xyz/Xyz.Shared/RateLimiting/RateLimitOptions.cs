namespace Xyz.Shared.RateLimiting;

/// <summary>Section <c>RateLimit</c>.</summary>
public sealed class RateLimitOptions
{
    /// <summary>How many requests one caller may make per minute.</summary>
    public int RequestsPerMinute { get; set; } = 120;
}
