using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.EventBus.Tests;

public sealed record SampleEto(string Name, int Count) : IIntegrationEventMessage;

public sealed record OtherEto(Guid Id) : IIntegrationEventMessage;

/// <summary>Records what it handled and fails the first <see cref="FailTimes"/> calls.</summary>
public sealed class RecordingHandler : IIntegrationEventHandler<SampleEto>
{
    public List<MessageEnvelope<SampleEto>> Handled { get; } = [];

    public int Calls { get; private set; }

    public int FailTimes { get; set; }

    public Exception? Failure { get; set; }

    public ParentMessageEnvelope? ContextDuringHandling { get; private set; }

    public LogFlowStep? FlowDuringHandling { get; private set; }

    public HsnSoft.Base.Security.UserContext? ActingUserDuringHandling { get; private set; }

    public Task HandleAsync(MessageEnvelope<SampleEto> envelope, CancellationToken cancellationToken)
    {
        Calls++;
        ContextDuringHandling = EventContext.Current;
        FlowDuringHandling = LogFlow.Current;
        ActingUserDuringHandling = HsnSoft.Base.Security.ActingUser.Current;
        if (Calls <= FailTimes)
        {
            throw Failure ?? new InvalidOperationException($"failure {Calls}");
        }

        Handled.Add(envelope);
        return Task.CompletedTask;
    }
}

public sealed class SecondHandler : IIntegrationEventHandler<SampleEto>, IIntegrationEventHandler<OtherEto>
{
    public Task HandleAsync(MessageEnvelope<SampleEto> envelope, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task HandleAsync(MessageEnvelope<OtherEto> envelope, CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>Throws before its first await, the way a handler with a guard clause does.</summary>
public sealed class ThrowingSynchronouslyHandler : IIntegrationEventHandler<SampleEto>
{
    public Task HandleAsync(MessageEnvelope<SampleEto> envelope, CancellationToken cancellationToken)
        => throw new InvalidOperationException("thrown synchronously");
}

public sealed class CancellingHandler : IIntegrationEventHandler<SampleEto>
{
    public Task HandleAsync(MessageEnvelope<SampleEto> envelope, CancellationToken cancellationToken)
        => throw new OperationCanceledException();
}

/// <summary>Scoped dependency of <see cref="ScopedHandler"/>; one instance per scope.</summary>
public sealed class ScopedMarker
{
    public Guid Id { get; } = Guid.NewGuid();
}

public sealed class ScopedHandler(ScopedMarker marker, List<Guid> seen) : IIntegrationEventHandler<SampleEto>
{
    public Task HandleAsync(MessageEnvelope<SampleEto> envelope, CancellationToken cancellationToken)
    {
        seen.Add(marker.Id);
        throw new InvalidOperationException("fail so the dispatcher retries");
    }
}

public sealed class FixedHeaderProvider(string? correlationId, params (string Name, string Value)[] values) : IEventHeaderProvider
{
    public string? GetCorrelationId() => correlationId;

    public void AddHeaders(IDictionary<string, string> headers)
    {
        foreach ((string name, string value) in values)
        {
            headers[name] = value;
        }
    }
}

/// <summary>Keeps what was logged, so a test can look at the messages and the scopes.</summary>
public sealed class ListLogger<T> : ILogger<T>
{
    private readonly List<IReadOnlyDictionary<string, object?>> _active = [];

    public List<(LogLevel Level, string Message)> Entries { get; } = [];

    /// <summary>Every scope that was started.</summary>
    public List<IReadOnlyDictionary<string, object?>> Scopes { get; } = [];

    /// <summary>Every log with its event name and the properties of the scopes that were open when it was written.</summary>
    public List<LoggedEvent> Events { get; } = [];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        if (state is not IEnumerable<KeyValuePair<string, object?>> values)
        {
            return null;
        }

        IReadOnlyDictionary<string, object?> scope = values.ToDictionary(pair => pair.Key, pair => pair.Value);
        Scopes.Add(scope);
        _active.Add(scope);
        return new Pop(_active, scope);
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Entries.Add((logLevel, formatter(state, exception)));
        Dictionary<string, object?> properties = [];
        foreach (IReadOnlyDictionary<string, object?> scope in _active)
        {
            foreach (KeyValuePair<string, object?> pair in scope)
            {
                properties[pair.Key] = pair.Value;
            }
        }

        if (state is IEnumerable<KeyValuePair<string, object?>> own)
        {
            foreach (KeyValuePair<string, object?> pair in own)
            {
                properties[pair.Key] = pair.Value;
            }
        }

        Events.Add(new LoggedEvent(logLevel, eventId.Name, formatter(state, exception), exception, properties));
    }

    public sealed record LoggedEvent(LogLevel Level, string? EventName, string Message, Exception? Exception, IReadOnlyDictionary<string, object?> Properties);

    private sealed class Pop(List<IReadOnlyDictionary<string, object?>> active, IReadOnlyDictionary<string, object?> scope) : IDisposable
    {
        public void Dispose() => active.Remove(scope);
    }
}
