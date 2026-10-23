using System;
using System.Collections.Generic;
using System.Globalization;

namespace HsnSoft.Base.Logging;

/// <summary>
/// The fixed fields of the logs that follow a chain (request/response, consume start/end), built in one place so every one of them
/// has the same names, and nothing in them is unbounded: Graylog stores every field name it sees.
/// </summary>
public static class LogFields
{
    public const string LogType = nameof(LogType);
    public const string CorrelationId = nameof(CorrelationId);
    public const string ScopeId = nameof(ScopeId);
    public const string From = nameof(From);
    public const string To = nameof(To);
    public const string ElapsedMs = nameof(ElapsedMs);

    /// <summary>The fields every log of a chain carries; add the ones of the log type to the result.</summary>
    /// <param name="logType">One of <see cref="LogTypes"/>.</param>
    /// <param name="correlationId">The id of the whole chain.</param>
    /// <param name="scopeId">The id the start log and the end log of one step share.</param>
    /// <param name="from">Where the step was started from.</param>
    /// <param name="to">The step itself.</param>
    public static Dictionary<string, object?> Chain(string logType, string? correlationId, string scopeId, string from, string to)
        => new()
        {
            [LogType] = logType,
            [CorrelationId] = correlationId,
            [ScopeId] = scopeId,
            [From] = from,
            [To] = to,
        };

    /// <summary>A new id for a chain or a step: a time-ordered GUID as 32 lower-case hex digits, without dashes.</summary>
    public static string NewId() => Guid.CreateVersion7().ToString("N", CultureInfo.InvariantCulture);

    /// <summary>The name of a request's step: <c>controller-action</c> in lower case, like <c>token-login</c>.</summary>
    public static string StepName(string first, string second) => string.Create(CultureInfo.InvariantCulture, $"{first}-{second}").ToLowerInvariant();

    /// <summary>The text cut to at most <paramref name="maxLength"/> characters (the cut is marked); <see langword="null"/> stays <see langword="null"/>.</summary>
    public static string? Text(string? value, int maxLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxLength);

        if (value is null || value.Length <= maxLength)
        {
            return value;
        }

        return string.Create(CultureInfo.InvariantCulture, $"{value.AsSpan(0, maxLength)}...(+{value.Length - maxLength})");
    }
}
