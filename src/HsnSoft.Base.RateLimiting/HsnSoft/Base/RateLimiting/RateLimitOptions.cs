using System;
using System.Collections.Generic;

namespace HsnSoft.Base.RateLimiting;

/// <summary>The settings of the limits (section <c>RateLimiting</c>): a limit that the code names can be changed here, with no deployment.</summary>
public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>The start of the keys in the store (default <c>ratelimit</c>), so a service does not meet the keys of another on a shared Redis.</summary>
    public string KeyPrefix { get; set; } = "ratelimit";

    /// <summary>
    /// What a host does when the store of the counts cannot be reached: <see langword="false"/> (the default) the request fails, so a limit that protects a login is never silently off; <see langword="true"/> the request is let through and the failure is logged, for an endpoint that matters
    /// more than its limit. Read by the host's gate (<c>HsnSoft.Base.RateLimiting.AspNetCore</c>); the limiters themselves throw.
    /// </summary>
    public bool FailOpen { get; set; }

    /// <summary>The limits that the settings change, by name (<c>sign-in</c>); what is not here is what the code says.</summary>
    public Dictionary<string, RateLimitSetting> Limits { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The permits and the window of <paramref name="name"/>: the settings win over what the code said.</summary>
    public (int Permits, TimeSpan Window) Resolve(string name, int codePermits, int codeWindowSeconds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Limits.TryGetValue(name, out RateLimitSetting? setting);
        int permits = setting?.Permits ?? codePermits;
        int seconds = setting?.WindowSeconds ?? codeWindowSeconds;
        if (permits < 1)
        {
            throw new InvalidOperationException($"The limit '{name}' has to allow at least 1 request.");
        }

        if (seconds < 1)
        {
            throw new InvalidOperationException($"The window of '{name}' has to be one second at least.");
        }

        return (permits, TimeSpan.FromSeconds(seconds));
    }
}

/// <summary>What the settings say of one limit; a value that is not set is what the code says.</summary>
public sealed class RateLimitSetting
{
    public int? Permits { get; set; }

    public int? WindowSeconds { get; set; }
}
