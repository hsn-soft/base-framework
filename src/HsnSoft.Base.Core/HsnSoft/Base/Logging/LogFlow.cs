using System;
using System.Threading;

namespace HsnSoft.Base.Logging;

/// <summary>
/// Where the code that is running now belongs in a chain: the request or the message it is handling. A request that publishes an
/// event, whose handler publishes another event, is one chain with one correlation id; <see cref="LogFlowStep.Name"/> says which step of it
/// is running (<c>token-login</c>, <c>ordercreated</c>), and follows async calls.
/// </summary>
public static class LogFlow
{
    /// <summary>What a message published outside any request or message (from an agent, a scheduler) says it comes from.</summary>
    public const string AgentName = "agent";

    /// <summary>What a request says it comes from.</summary>
    public const string HttpClientName = "httpclient";

    private static readonly AsyncLocal<LogFlowStep?> s_current = new();

    /// <summary>The step that is running, or <see langword="null"/> outside a request and a message.</summary>
    public static LogFlowStep? Current => s_current.Value;

    /// <summary>Makes the code that follows a step of the chain until the returned scope is disposed.</summary>
    /// <param name="correlationId">The id that stays the same along the whole chain.</param>
    /// <param name="scopeId">The id of this step's start and end logs.</param>
    /// <param name="name">What this step is called, lower case: <c>controller-action</c> for a request, the event name for a message.</param>
    public static IDisposable Begin(string? correlationId, string scopeId, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scopeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        LogFlowStep? previous = s_current.Value;
        s_current.Value = new LogFlowStep(correlationId, scopeId, name);
        return new Restore(previous);
    }

    private sealed class Restore(LogFlowStep? previous) : IDisposable
    {
        public void Dispose() => s_current.Value = previous;
    }
}

/// <summary>One step of a chain.</summary>
/// <param name="CorrelationId">The id of the whole chain.</param>
/// <param name="ScopeId">The id of this step.</param>
/// <param name="Name">What the step is called.</param>
public sealed record LogFlowStep(string? CorrelationId, string ScopeId, string Name);
