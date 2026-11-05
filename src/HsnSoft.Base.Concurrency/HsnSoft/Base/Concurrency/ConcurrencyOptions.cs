using System;
using System.Collections.Generic;

namespace HsnSoft.Base.Concurrency;

/// <summary>The settings of the limits (section <c>Concurrency</c>): a limit that the code names can be changed here, with no deployment.</summary>
public sealed class ConcurrencyOptions
{
    public const string SectionName = "Concurrency";

    /// <summary>The start of the keys in the store (default <c>concurrency</c>), so a service does not meet the keys of another.</summary>
    public string KeyPrefix { get; set; } = "concurrency";

    /// <summary>The lease of a holder that does not say its own, in seconds. Default: 60.</summary>
    public int DefaultLeaseSeconds { get; set; } = 60;

    /// <summary>What an answer of "too many" tells the caller to wait before it tries again, in seconds (<c>Retry-After</c>). Default: 1.</summary>
    public int RetryAfterSeconds { get; set; } = 1;

    /// <summary>The limits that the settings change, by name (<c>send-invoice</c>); what is not here is what the code says.</summary>
    public Dictionary<string, ConcurrencyLimitSetting> Limits { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The maximum and the lease of <paramref name="name"/>: the settings win over what the code said.</summary>
    public (int Max, TimeSpan Lease) Resolve(string name, int codeMax, TimeSpan? codeLease = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Limits.TryGetValue(name, out ConcurrencyLimitSetting? setting);
        int max = setting?.Max ?? codeMax;
        TimeSpan lease = setting?.LeaseSeconds is { } seconds ? TimeSpan.FromSeconds(seconds) : codeLease ?? TimeSpan.FromSeconds(DefaultLeaseSeconds);
        if (max < 1)
        {
            throw new InvalidOperationException($"The limit '{name}' has to allow at least 1.");
        }

        if (lease <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"The lease of '{name}' has to be longer than zero.");
        }

        return (max, lease);
    }
}

/// <summary>What the settings say of one limit; a value that is not set is what the code says.</summary>
public sealed class ConcurrencyLimitSetting
{
    public int? Max { get; set; }

    public int? LeaseSeconds { get; set; }
}
