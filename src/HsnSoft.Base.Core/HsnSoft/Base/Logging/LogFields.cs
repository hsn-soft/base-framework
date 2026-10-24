using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

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
    public const string Facility = nameof(Facility);

    /// <summary>
    /// A placeholder that only builds the text of a message (what is worth a word after the status): it is not a field of the persistent sink, the
    /// message has it.
    /// </summary>
    public const string MessageDetails = nameof(MessageDetails);

    /// <summary>The fields about who asked; they belong to the logs of a request or a message and of the application, not to the logs of the framework and the platform.</summary>
    public static readonly string[] WhoFields = ["UserId", "UserRoles", "ClientChannel", "ClientVersion", "ClientLat", "ClientLong"];

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

    /// <summary>The trace id of the request being handled (the W3C trace ASP.NET Core started or continued), or a new id when there is no trace.</summary>
    public static string NewCorrelationId() => System.Diagnostics.Activity.Current?.TraceId.ToHexString() is { } traceId && traceId != new string('0', 32) ? traceId : NewId();

    /// <summary>Whether a text is a W3C trace id: 32 lower-case hex digits that are not all zero. A correlation id that is one is also the <c>TraceId</c> of the logs.</summary>
    public static bool IsTraceId(string? value)
        => value is { Length: 32 } && value.All(character => character is (>= '0' and <= '9') or (>= 'a' and <= 'f')) && value.Any(character => character != '0');

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
